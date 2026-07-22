using Microsoft.Data.SqlClient;

namespace Databasemanager;

public static class ConnectionStringFactory
{
    public static string Create(string serverInstance, string databaseName)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = serverInstance,
            InitialCatalog = string.IsNullOrWhiteSpace(databaseName) ? "master" : databaseName,
            IntegratedSecurity = true,
            TrustServerCertificate = true,
            Encrypt = false,
            MultipleActiveResultSets = true
        };

        return builder.ConnectionString;
    }
}
