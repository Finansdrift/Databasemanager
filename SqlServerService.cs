using Microsoft.Data.SqlClient;
using Microsoft.Win32;

namespace Databasemanager;

public sealed class SqlServerService
{
    public IReadOnlyList<string> GetLikelyServerInstances(string configuredServer)
    {
        var servers = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(configuredServer))
            servers.Add(configuredServer);

        AddLocalSqlInstances(servers);
        servers.Add(".");
        servers.Add(@".\SQLEXPRESS");
        servers.Add(@"(localdb)\MSSQLLocalDB");

        return servers.ToList();
    }

    public async Task<IReadOnlyList<string>> GetDatabasesAsync(
        string serverInstance,
        CancellationToken cancellationToken = default)
    {
        var databases = new List<string>();
        string connectionString = ConnectionStringFactory.Create(serverInstance, "master");

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT name
            FROM sys.databases
            WHERE state_desc = 'ONLINE'
            ORDER BY name;
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            databases.Add(reader.GetString(0));

        return databases;
    }

    private static void AddLocalSqlInstances(ISet<string> servers)
    {
        using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL");
        if (key is null)
            return;

        foreach (string instanceName in key.GetValueNames())
        {
            if (instanceName.Equals("MSSQLSERVER", StringComparison.OrdinalIgnoreCase))
                servers.Add(Environment.MachineName);
            else
                servers.Add($@"{Environment.MachineName}\{instanceName}");
        }
    }
}
