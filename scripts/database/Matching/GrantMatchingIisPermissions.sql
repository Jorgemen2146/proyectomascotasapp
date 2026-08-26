/* Run after the Matching schema scripts as a SQL Server administrator. */
USE [master];
GO

SET NOCOUNT ON;

IF SUSER_ID(N'IIS APPPOOL\Matching.API') IS NULL
    CREATE LOGIN [IIS APPPOOL\Matching.API] FROM WINDOWS;
GO

USE [DogPlatform_MatchingDb];
GO

IF USER_ID(N'IIS APPPOOL\Matching.API') IS NULL
    CREATE USER [IIS APPPOOL\Matching.API]
        FOR LOGIN [IIS APPPOOL\Matching.API];
GO

GRANT SELECT, INSERT, UPDATE, DELETE
    ON SCHEMA::[matching]
    TO [IIS APPPOOL\Matching.API];
GO

USE [DogPlatform_IdentityDb];
GO

IF USER_ID(N'IIS APPPOOL\Matching.API') IS NULL
    CREATE USER [IIS APPPOOL\Matching.API]
        FOR LOGIN [IIS APPPOOL\Matching.API];
GO

GRANT INSERT ON OBJECT::[auth].[ErrorLogs]
    TO [IIS APPPOOL\Matching.API];
GO
