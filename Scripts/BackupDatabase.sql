DECLARE @databaseName sysname = N'$(DatabaseName)';
DECLARE @outputDirectory nvarchar(4000) = N'$(OutputDirectory)';
DECLARE @backupFile nvarchar(4000) = @outputDirectory + N'\' + @databaseName + N'_' +
    REPLACE(CONVERT(nvarchar(19), GETDATE(), 120), N':', N'') + N'.bak';

BACKUP DATABASE @databaseName
TO DISK = @backupFile
WITH INIT, COMPRESSION, STATS = 10;
