using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace Databasemanager;

public sealed class SqlScriptRunner
{
    private static readonly Regex GoSeparator = new(
        @"^\s*GO(?:\s+(?<count>\d+))?\s*(?:--.*)?$",
        RegexOptions.IgnoreCase |
        RegexOptions.Multiline |
        RegexOptions.Compiled);

    public async Task ExecuteFileAsync(
        string connectionString,
        string scriptFile,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException(
                "A connection string is required.",
                nameof(connectionString));

        if (!File.Exists(scriptFile))
            throw new FileNotFoundException(
                "The SQL script file was not found.",
                scriptFile);

        string script = await File.ReadAllTextAsync(
            scriptFile,
            Encoding.UTF8,
            cancellationToken);

        await ExecuteScriptAsync(
            connectionString,
            script,
            progress,
            cancellationToken);
    }

    public async Task ExecuteScriptAsync(string connectionString,string script,IProgress<string>? progress = null,CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);

        connection.InfoMessage += (_, e) =>
        {
            foreach (SqlError message in e.Errors)
                progress?.Report(message.Message);
        };

        await connection.OpenAsync(cancellationToken);
        var batches = SplitIntoBatches(script);
        int batchNumber = 0;

        foreach (SqlBatch batch in batches)
        {
            batchNumber++;

            for (int execution = 1; execution <= batch.RepeatCount; execution++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress?.Report($"Executing batch {batchNumber}, " +$"iteration {execution} of {batch.RepeatCount}.");
                await using var command = connection.CreateCommand();
                command.CommandText = batch.Text;

                /*
                    BACKUP and RESTORE can take a long time.
                    Zero means no command timeout.
                */
                command.CommandTimeout = 0;

                try
                {
                    await command.ExecuteNonQueryAsync(cancellationToken);
                }
                catch (SqlException ex)
                {
                    throw new SqlScriptException(batchNumber,batch.Text,ex);
                }
            }
        }
    }

    private static IReadOnlyList<SqlBatch> SplitIntoBatches(string script)
    {
        var batches = new List<SqlBatch>();

        int startIndex = 0;

        foreach (Match match in GoSeparator.Matches(script))
        {
            string batchText = script[startIndex..match.Index].Trim();

            if (batchText.Length > 0)
            {
                int repeatCount = 1;

                if (match.Groups["count"].Success)
                {
                    repeatCount = int.Parse(
                        match.Groups["count"].Value,
                        System.Globalization.CultureInfo.InvariantCulture);
                }

                batches.Add(new SqlBatch(batchText, repeatCount));
            }

            startIndex = match.Index + match.Length;
        }

        string finalBatch = script[startIndex..].Trim();

        if (finalBatch.Length > 0)
            batches.Add(new SqlBatch(finalBatch, 1));

        return batches;
    }

    private sealed record SqlBatch(
        string Text,
        int RepeatCount);
}

public sealed class SqlScriptException : Exception
{
    public int BatchNumber { get; }
    public string BatchText { get; }

    public SqlScriptException(
        int batchNumber,
        string batchText,
        Exception innerException)
        : base(
            $"SQL script batch {batchNumber} failed: " +
            innerException.Message,
            innerException)
    {
        BatchNumber = batchNumber;
        BatchText = batchText;
    }
}
