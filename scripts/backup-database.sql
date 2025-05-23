-- SQL Server Backup Script
-- This script creates a full backup of the Nostra_Dataload and Nostra_Dataload_Tasks databases

USE master;
GO

-- Set backup file path - adjust as needed
DECLARE @BackupPath NVARCHAR(255) = N'C:\Backups\';
DECLARE @BackupFileName NVARCHAR(255);
DECLARE @BackupFileNameTasks NVARCHAR(255);
DECLARE @DateTime NVARCHAR(20);

-- Format date for filename
SET @DateTime = REPLACE(CONVERT(NVARCHAR, GETDATE(), 112) + '_' + 
                REPLACE(CONVERT(NVARCHAR, GETDATE(), 108), ':', ''), ' ', '_');

-- Set backup filenames
SET @BackupFileName = @BackupPath + 'Nostra_Dataload_' + @DateTime + '.bak';
SET @BackupFileNameTasks = @BackupPath + 'Nostra_Dataload_Tasks_' + @DateTime + '.bak';

-- Backup main database
BACKUP DATABASE [Nostra_Dataload] 
TO DISK = @BackupFileName
WITH COMPRESSION, STATS = 10, CHECKSUM;

-- Backup tasks database
BACKUP DATABASE [Nostra_Dataload_Tasks] 
TO DISK = @BackupFileNameTasks
WITH COMPRESSION, STATS = 10, CHECKSUM;

PRINT 'Backup completed successfully.';
PRINT 'Main database backup: ' + @BackupFileName;
PRINT 'Tasks database backup: ' + @BackupFileNameTasks;
GO