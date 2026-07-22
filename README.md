# Databasemanager
Absolute minimum and rapid DBMS for backup, restore, delete and identity. 
.net core 9 Winforms application for MS SQL server 2012 and upwards

Preparations before use
Change @DataDirectory for the restorefromfile.sql script in the Scriptsfolder to reflect your own environment:
SET @DataDirectory = N'C:\[databaserYourfolder]\[MSSQL11.DYNGBASE]\MSSQL\DATA\';
SET @LogDirectory = N'C:\[databaserYourfolder]\[MSSQL11.DYNGBASE]\MSSQL\Log\';

Select the database instanace from a dropdown and right-click table names to manage. 

__1 Restoring an eksisting database from a backup file__
- The backupfile must end with the extension .bak
- The backup file must reside in the folder configured as output folder
- Right-click the database name and select Restore

__2 Restoring a database not in the list of databases for the chosen nistance__
- The backupfile must end with the extension .bak
- The backup file must reside in the folder configured as output folder
- Write the name of the database to restore in the text field Default database
- Click the Restore button

__3 Backing up a database__
- The backupfile must end with the extension .bak
- The backup file must reside in the folder configured as output folder
- Righ-click the name of the database to make a backup of

__4 Delete  a database__
- Righ-click the name of the database to delete 

__5 Attach an identity to database__
- Write the exact identity name in the Identity text box. Example: iis apppool\tremendous  or just myidentity. The access right must exist in the database main identities list
- Righ-click the name of the database and select Set identity to give an access right

__6 Suggested improvements__
- A file selector for the Default database text field
- A script editor
- Some way of selecting an existing identity from the selected database



