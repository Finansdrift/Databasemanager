DECLARE @databaseName sysname = N'$(DatabaseName)';
DECLARE @identityName nvarchar(256) = N'$(IdentityName)';

PRINT N'Set identity requested for database ' + QUOTENAME(@databaseName) + N' and identity ' + @identityName + N'.';
PRINT N'Replace this template with the T-SQL required by your environment.';
