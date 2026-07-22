DECLARE @databaseName sysname = N'$(DatabaseName)';
DECLARE @outputDirectory nvarchar(4000) = N'$(OutputDirectory)';
DECLARE @backupFile nvarchar(4000) = @outputDirectory + N'\' + @databaseName + N'.bak';

EXEC(N'ALTER DATABASE ' + QUOTENAME(@databaseName) + N' SET SINGLE_USER WITH ROLLBACK IMMEDIATE;');
RESTORE DATABASE @databaseName
FROM DISK = @backupFile
WITH REPLACE, STATS = 10;
EXEC(N'ALTER DATABASE ' + QUOTENAME(@databaseName) + N' SET MULTI_USER;');
