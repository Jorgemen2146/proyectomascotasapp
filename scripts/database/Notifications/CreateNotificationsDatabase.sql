/*
  DogPlatform.Notifications - database and schema.
  Run from SSMS with a SQL Server administrator account.
  This script is idempotent and is not executed automatically.
*/
USE [master];
GO

SET NOCOUNT ON;

IF DB_ID(N'DogPlatform_NotificationsDb') IS NULL
BEGIN
    CREATE DATABASE [DogPlatform_NotificationsDb];
END;
GO

USE [DogPlatform_NotificationsDb];
GO

SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF SCHEMA_ID(N'notifications') IS NULL
        EXEC(N'CREATE SCHEMA [notifications]');

    IF OBJECT_ID(N'[notifications].[Notifications]', N'U') IS NULL
    BEGIN
        CREATE TABLE [notifications].[Notifications]
        (
            [NotificationId] uniqueidentifier NOT NULL
                CONSTRAINT [PK_Notifications] PRIMARY KEY,
            [UserId] uniqueidentifier NOT NULL,
            [PetId] uniqueidentifier NULL,
            [VaccineId] int NULL,
            [Type] nvarchar(100) NOT NULL,
            [Title] nvarchar(200) NOT NULL,
            [Message] nvarchar(1000) NOT NULL,
            [ReferenceType] nvarchar(100) NULL,
            [ReferenceId] nvarchar(200) NULL,
            [Status] nvarchar(50) NOT NULL
                CONSTRAINT [DF_Notifications_Status] DEFAULT (N'Created'),
            [IsRead] bit NOT NULL
                CONSTRAINT [DF_Notifications_IsRead] DEFAULT (0),
            [ReadAtUtc] datetime2 NULL,
            [CreatedAtUtc] datetime2 NOT NULL
                CONSTRAINT [DF_Notifications_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
            [NotificationDateUtc] date NOT NULL,
            [DeduplicationKey] nvarchar(300) NOT NULL,
            [MetadataJson] nvarchar(max) NULL,
            CONSTRAINT [CK_Notifications_Status]
                CHECK ([Status] IN (N'Created', N'Sent', N'Failed')),
            CONSTRAINT [CK_Notifications_Type]
                CHECK ([Type] IN
                (
                    N'VaccinationDueSoon',
                    N'VaccinationDueToday',
                    N'VaccinationOverdue',
                    N'VaccinationNotStarted'
                )),
            CONSTRAINT [CK_Notifications_ReadAtUtc]
                CHECK (([IsRead] = 0 AND [ReadAtUtc] IS NULL) OR [IsRead] = 1)
        );
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [object_id] = OBJECT_ID(N'[notifications].[Notifications]') AND [name] = N'IX_Notifications_UserId_CreatedAtUtc')
        CREATE INDEX [IX_Notifications_UserId_CreatedAtUtc]
            ON [notifications].[Notifications] ([UserId], [CreatedAtUtc] DESC);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [object_id] = OBJECT_ID(N'[notifications].[Notifications]') AND [name] = N'IX_Notifications_UserId_IsRead')
        CREATE INDEX [IX_Notifications_UserId_IsRead]
            ON [notifications].[Notifications] ([UserId], [IsRead]);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [object_id] = OBJECT_ID(N'[notifications].[Notifications]') AND [name] = N'IX_Notifications_NotificationDateUtc')
        CREATE INDEX [IX_Notifications_NotificationDateUtc]
            ON [notifications].[Notifications] ([NotificationDateUtc]);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [object_id] = OBJECT_ID(N'[notifications].[Notifications]') AND [name] = N'IX_Notifications_Type')
        CREATE INDEX [IX_Notifications_Type]
            ON [notifications].[Notifications] ([Type]);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [object_id] = OBJECT_ID(N'[notifications].[Notifications]') AND [name] = N'IX_Notifications_PetId')
        CREATE INDEX [IX_Notifications_PetId]
            ON [notifications].[Notifications] ([PetId]);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [object_id] = OBJECT_ID(N'[notifications].[Notifications]') AND [name] = N'UX_Notifications_DeduplicationKey')
        CREATE UNIQUE INDEX [UX_Notifications_DeduplicationKey]
            ON [notifications].[Notifications] ([DeduplicationKey]);

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

SELECT DB_NAME() AS [DatabaseName];
SELECT [TABLE_SCHEMA], [TABLE_NAME]
FROM INFORMATION_SCHEMA.TABLES
WHERE [TABLE_SCHEMA] = N'notifications';
GO
