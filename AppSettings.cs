namespace Databasemanager;

public sealed class AppSettings
{
    public string ServerInstanceName { get; set; } = @"instance\name";
    public string DefaultDatabaseName { get; set; } = "YourTable";
    public string SqlScriptsPath { get; set; } = @"C:\Projects\Databasemanager\Scripts";
    public string OutputDirectory { get; set; } = @"C:\Projects\Databasemanager\Output";
    public string DefaultIdentityName { get; set; } = "root";
}
