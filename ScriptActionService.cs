using System.Text;

namespace Databasemanager;

public enum DatabaseAction
{
    BackupDatabase,
    RestoreDatabase,
    SetIdentity,
    DeleteDatabase
}

public sealed class ScriptActionService
{
    private readonly SqlScriptRunner runner = new();

    public async Task ExecuteAsync(
        DatabaseAction action,
        AppSettings settings,
        string serverInstance,
        string defaultDatabase,
        string identityName,
        string databaseName,
        string scriptsPath,
        string outputDirectory,
        IProgress<string>? progress,
        CancellationToken cancellationToken = default)
    {
        string scriptFile = Path.Combine(scriptsPath, GetScriptFileName(action));
        if (!File.Exists(scriptFile))
            throw new FileNotFoundException(
                $"Expected script file '{Path.GetFileName(scriptFile)}' in the SQL scripts directory.",
                scriptFile);

        string script = await File.ReadAllTextAsync(scriptFile, Encoding.UTF8, cancellationToken);
        script = ReplaceTokens(script, settings, serverInstance, defaultDatabase, identityName, databaseName, scriptsPath, outputDirectory);
        string connectionDatabase = action is DatabaseAction.RestoreDatabase or DatabaseAction.DeleteDatabase
            ? "master"
            : databaseName;
        string connectionString = ConnectionStringFactory.Create(serverInstance, connectionDatabase);
        progress?.Report($"Running {GetDisplayName(action)} for {databaseName} using {scriptFile}");
        await runner.ExecuteScriptAsync(connectionString, script, progress, cancellationToken);
        progress?.Report($"{GetDisplayName(action)} completed for {databaseName}.");
    }

    public async Task ExecuteRestoreSqlAsync(
        AppSettings settings,
        string serverInstance,
        string databaseName,
        string identityName,
        string scriptsPath,
        string outputDirectory,
        IProgress<string>? progress,
        CancellationToken cancellationToken = default)
    {
        string scriptFile = Path.Combine(scriptsPath, "Restorefromfile.sql");
        if (!File.Exists(scriptFile))
            throw new FileNotFoundException("Expected script file 'Restore.sql' in the SQL scripts directory.",scriptFile);

        string script = await File.ReadAllTextAsync(scriptFile, Encoding.UTF8, cancellationToken);
        script = ReplaceTokens(script, settings, serverInstance, databaseName, identityName, databaseName, scriptsPath, outputDirectory);
        string connectionString = ConnectionStringFactory.Create(serverInstance, "master");
        progress?.Report($"Running Restore for {databaseName} using {scriptFile}");
        await runner.ExecuteScriptAsync(connectionString, script, progress, cancellationToken);
        progress?.Report($"Restore completed for {databaseName}.");
    }

    public static string GetDisplayName(DatabaseAction action) =>
        action switch
        {
            DatabaseAction.BackupDatabase => "Backup database",
            DatabaseAction.RestoreDatabase => "Restore database",
            DatabaseAction.SetIdentity => "Set identity",
            DatabaseAction.DeleteDatabase => "Delete database",
            _ => action.ToString()
        };

    private static string GetScriptFileName(DatabaseAction action) =>
        action switch
        {
            DatabaseAction.BackupDatabase => "BackupDatabase.sql",
            DatabaseAction.RestoreDatabase => "Restorefromfile.sql",
            DatabaseAction.SetIdentity => "SetIdentity.sql",
            DatabaseAction.DeleteDatabase => "deleteDatabase.sql",
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
        };

    private static string ReplaceTokens(
        string script,
        AppSettings settings,
        string serverInstance,
        string defaultDatabase,
        string identityName,
        string databaseName,
        string scriptsPath,
        string outputDirectory)
    {
        var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ServerInstance"] = serverInstance,
            ["DefaultDatabase"] = defaultDatabase,
            ["DatabaseName"] = databaseName,
            ["IdentityName"] = identityName,
            ["ScriptsDirectory"] = scriptsPath,
            ["OutputDirectory"] = outputDirectory,
            ["ConfiguredServerInstance"] = settings.ServerInstanceName,
            ["ConfiguredDefaultDatabase"] = settings.DefaultDatabaseName
        };

        foreach (var token in tokens)
        {
            script = script.Replace("$(" + token.Key + ")", EscapeSqlLiteral(token.Value), StringComparison.OrdinalIgnoreCase);
            script = script.Replace("{{" + token.Key + "}}", EscapeSqlLiteral(token.Value), StringComparison.OrdinalIgnoreCase);
        }

        return script;
    }

    private static string EscapeSqlLiteral(string value) =>
        value.Replace("'", "''", StringComparison.Ordinal);
}
