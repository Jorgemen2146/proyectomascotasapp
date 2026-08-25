/* Run after CreateNotificationsDatabase.sql as a SQL Server administrator. */
USE [master];
GO

SET NOCOUNT ON;

IF SUSER_ID(N'IIS APPPOOL\Notifications.API') IS NULL
    CREATE LOGIN [IIS APPPOOL\Notifications.API] FROM WINDOWS;
GO

USE [DogPlatform_NotificationsDb];
GO

IF USER_ID(N'IIS APPPOOL\Notifications.API') IS NULL
    CREATE USER [IIS APPPOOL\Notifications.API]
        FOR LOGIN [IIS APPPOOL\Notifications.API];
GO

GRANT SELECT, INSERT, UPDATE, DELETE
    ON SCHEMA::[notifications]
    TO [IIS APPPOOL\Notifications.API];
GO

USE [DogPlatform_IdentityDb];
GO

IF USER_ID(N'IIS APPPOOL\Notifications.API') IS NULL
    CREATE USER [IIS APPPOOL\Notifications.API]
        FOR LOGIN [IIS APPPOOL\Notifications.API];
GO

GRANT INSERT ON OBJECT::[auth].[ErrorLogs]
    TO [IIS APPPOOL\Notifications.API];
GO
