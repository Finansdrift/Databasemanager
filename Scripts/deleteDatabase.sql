/*
    Disconnect users and delete the destination database if it already exists.
*/

DECLARE @DatabaseName sysname;
DECLARE @Sql nvarchar(max);
SET @DatabaseName = N'$(DatabaseName)';

IF DB_ID(@DatabaseName) IS NOT NULL
BEGIN
    PRINT N'Dropping existing database: ' + @DatabaseName;

    SET @Sql =
        N'ALTER DATABASE ' + QUOTENAME(@DatabaseName) +
        N' SET SINGLE_USER WITH ROLLBACK IMMEDIATE;' +
        N'DROP DATABASE ' + QUOTENAME(@DatabaseName) + N';'; 

    EXEC sys.sp_executesql @Sql;
END;
