USE [master];
GO

SET NOCOUNT ON;
GO

DECLARE @DatabaseName sysname;
DECLARE @BackupFile nvarchar(520);
DECLARE @DataDirectory nvarchar(260);
DECLARE @LogDirectory nvarchar(260);
DECLARE @Sql nvarchar(max);
DECLARE @MoveClauses nvarchar(max);
DECLARE @FileExists int;
DECLARE @FileIsDirectory int;
DECLARE @ParentDirectoryExists int;
DECLARE @CredentialName sysname;

/*
    Configuration
*/
SET @CredentialName = N'$(IdentityName)';
SET @DatabaseName = N'$(DatabaseName)';
SET @BackupFile = N'$(OutputDirectory)$(DatabaseName).bak';
SET @DataDirectory = N'C:\[databaserYourfolder]\[MSSQL11.DYNGBASE]\MSSQL\DATA\';
SET @LogDirectory = N'C:\[databaserYourfolder]\[MSSQL11.DYNGBASE]\MSSQL\Log\';

/*
    Ensure that the directories end with a backslash.
*/
IF RIGHT(@DataDirectory, 1) <> N'\'
    SET @DataDirectory = @DataDirectory + N'\';

IF RIGHT(@LogDirectory, 1) <> N'\'
    SET @LogDirectory = @LogDirectory + N'\';


/*
    Check that the backup file exists and is visible
    to the SQL Server service account.
*/
CREATE TABLE #FileCheck
(
    FileExists int,
    FileIsDirectory int,
    ParentDirectoryExists int
);

INSERT INTO #FileCheck
EXEC master.dbo.xp_fileexist @BackupFile;

SELECT
    @FileExists = FileExists,
    @FileIsDirectory = FileIsDirectory,
    @ParentDirectoryExists = ParentDirectoryExists
FROM #FileCheck;

DROP TABLE #FileCheck;

IF ISNULL(@FileExists, 0) = 0
BEGIN
    RAISERROR(
        'The backup file "%s" does not exist or cannot be accessed by SQL Server.',
        16,
        1,
        @BackupFile
    );

    RETURN;
END;


/*
    RESTORE FILELISTONLY output structure for SQL Server 2012.
*/
CREATE TABLE #BackupFiles
(
    LogicalName nvarchar(128),
    PhysicalName nvarchar(260),
    Type char(1),
    FileGroupName nvarchar(128) NULL,
    Size numeric(20,0),
    MaxSize numeric(20,0),
    FileId bigint,
    CreateLSN numeric(25,0),
    DropLSN numeric(25,0) NULL,
    UniqueId uniqueidentifier,
    ReadOnlyLSN numeric(25,0) NULL,
    ReadWriteLSN numeric(25,0) NULL,
    BackupSizeInBytes bigint,
    SourceBlockSize int,
    FileGroupId int,
    LogGroupGUID uniqueidentifier NULL,
    DifferentialBaseLSN numeric(25,0) NULL,
    DifferentialBaseGUID uniqueidentifier NULL,
    IsReadOnly bit,
    IsPresent bit,
    TDEThumbprint varbinary(32) NULL
);


BEGIN TRY

    /*
        Read the logical files contained in the backup.
    */
    SET @Sql =
        N'RESTORE FILELISTONLY ' +
        N'FROM DISK = N''' +
        REPLACE(@BackupFile, N'''', N'''''') +
        N''';';

    INSERT INTO #BackupFiles
    EXEC sys.sp_executesql @Sql;


    IF NOT EXISTS
    (
        SELECT 1
        FROM #BackupFiles
        WHERE Type IN ('D', 'L')
    )
    BEGIN
        RAISERROR(
            'No database data or log files were found in the backup.',
            16,
            1
        );
    END;


    /*
        Build RESTORE MOVE clauses.

        Data files:
            DatabaseName.mdf
            DatabaseName_2.ndf
            DatabaseName_3.ndf

        Log files:
            DatabaseName_log.ldf
            DatabaseName_log_2.ldf
    */
    ;WITH NumberedFiles AS
    (
        SELECT
            LogicalName,
            Type,
            ROW_NUMBER() OVER
            (
                PARTITION BY Type
                ORDER BY FileId
            ) AS FileNumber
        FROM #BackupFiles
        WHERE Type IN ('D', 'L')
    )
    SELECT
        @MoveClauses =
            STUFF
            (
                (
                    SELECT
                        N', MOVE N''' +
                        REPLACE(NF.LogicalName, N'''', N'''''') +
                        N''' TO N''' +

                        CASE
                            WHEN NF.Type = 'D' THEN
                                @DataDirectory +
                                @DatabaseName +
                                CASE
                                    WHEN NF.FileNumber = 1
                                        THEN N'.mdf'
                                    ELSE
                                        N'_' +
                                        CONVERT(nvarchar(10), NF.FileNumber) +
                                        N'.ndf'
                                END

                            WHEN NF.Type = 'L' THEN
                                @LogDirectory +
                                @DatabaseName +
                                N'_log' +
                                CASE
                                    WHEN NF.FileNumber = 1
                                        THEN N''
                                    ELSE
                                        N'_' +
                                        CONVERT(nvarchar(10), NF.FileNumber)
                                END +
                                N'.ldf'
                        END +

                        N''''
                    FROM NumberedFiles AS NF
                    ORDER BY
                        CASE WHEN NF.Type = 'D' THEN 1 ELSE 2 END,
                        NF.FileNumber
                    FOR XML PATH(''), TYPE
                ).value('.', 'nvarchar(max)'),
                1,
                2,
                N''
            );


    /*
        Disconnect users and delete the destination database
        if it already exists.
    */
    IF DB_ID(@DatabaseName) IS NOT NULL
    BEGIN
        PRINT N'Dropping existing database: ' + @DatabaseName;

        SET @Sql =
            N'ALTER DATABASE ' +
            QUOTENAME(@DatabaseName) +
            N' SET SINGLE_USER WITH ROLLBACK IMMEDIATE;' +
            N'DROP DATABASE ' +
            QUOTENAME(@DatabaseName) +
            N';';

        EXEC sys.sp_executesql @Sql;
    END;


    /*
        Restore the database.
    */
    SET @Sql =
        N'RESTORE DATABASE ' +
        QUOTENAME(@DatabaseName) +
        N' FROM DISK = N''' +
        REPLACE(@BackupFile, N'''', N'''''') +
        N''' WITH ' +
        @MoveClauses +
        N', RECOVERY, REPLACE, STATS = 10;';

    PRINT N'Restoring database: ' + @DatabaseName;
    PRINT N'Backup file: ' + @BackupFile;

    EXEC sys.sp_executesql @Sql;


    /*
        Ensure that the restored database uses SIMPLE recovery.
    */
    SET @Sql =
        N'ALTER DATABASE ' +
        QUOTENAME(@DatabaseName) +
        N' SET RECOVERY SIMPLE;';

    EXEC sys.sp_executesql @Sql;


    PRINT N'Database restored successfully.';
    PRINT N'Database: ' + @DatabaseName;

END TRY
BEGIN CATCH

    DECLARE @ErrorMessage nvarchar(4000);

    SET @ErrorMessage = ERROR_MESSAGE();

    PRINT N'Database restore failed.';
    PRINT N'Database: ' + ISNULL(@DatabaseName, N'(unknown)');
    PRINT N'Backup file: ' + ISNULL(@BackupFile, N'(unknown)');
    PRINT N'Error: ' + @ErrorMessage;

    RAISERROR(
        '%s',
        16,
        1,
        @ErrorMessage
    );

END CATCH;

DROP TABLE #BackupFiles;


/*
    Create the Windows login if it does not already exist.
    Otherwise, update its default database.
*/

IF NULLIF(LTRIM(RTRIM(@CredentialName)), N'') IS NOT NULL
BEGIN
print '------------- is null'; 

IF NULLIF(LTRIM(RTRIM(@CredentialName)), N'') IS NOT NULL
BEGIN
    SET @Sql =
        N'CREATE LOGIN ' +
        QUOTENAME(@CredentialName) +
        N' FROM WINDOWS ' +
        N'WITH DEFAULT_DATABASE = ' +
        QUOTENAME(@DatabaseName) +
        N';';

    EXEC sys.sp_executesql @Sql;
END
ELSE
BEGIN
    SET @Sql =
        N'ALTER LOGIN ' +
        QUOTENAME(@CredentialName) +
        N' WITH DEFAULT_DATABASE = ' +
        QUOTENAME(@DatabaseName) +
        N';';

    EXEC sys.sp_executesql @Sql;
END;


/*
    Change context to the restored database.

    USE cannot take a variable directly, so the database-specific
    operations are executed as dynamic SQL.
*/
SET @Sql =
    N'USE ' + QUOTENAME(@DatabaseName) + N';

    IF DATABASE_PRINCIPAL_ID(@CredentialName) IS NULL
    BEGIN
        CREATE USER ' + QUOTENAME(@CredentialName) +
        N' FOR LOGIN ' + QUOTENAME(@CredentialName) + N';
    END
    ELSE
    BEGIN
        ALTER USER ' + QUOTENAME(@CredentialName) +
        N' WITH LOGIN = ' + QUOTENAME(@CredentialName) + N';
    END;

    IF IS_ROLEMEMBER(N''db_owner'', @CredentialName) <> 1
    BEGIN
        EXEC sys.sp_addrolemember
            @rolename = N''db_owner'',
            @membername = @CredentialName;
    END;';

EXEC sys.sp_executesql
    @Sql,
    N'@CredentialName sysname',
    @CredentialName = @CredentialName;

END
GO
