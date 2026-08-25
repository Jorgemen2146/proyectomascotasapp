/* Run after CreateGenealogyDatabase.sql as a SQL Server administrator. */
USE [master];
GO

IF SUSER_ID(N'IIS APPPOOL\Genealogy.API') IS NULL
    CREATE LOGIN [IIS APPPOOL\Genealogy.API] FROM WINDOWS;
GO

USE [DogPlatform_GenealogyDb];
GO

IF USER_ID(N'IIS APPPOOL\Genealogy.API') IS NULL
    CREATE USER [IIS APPPOOL\Genealogy.API] FOR LOGIN [IIS APPPOOL\Genealogy.API];
GO

GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::[genealogy]
    TO [IIS APPPOOL\Genealogy.API];
GO

USE [DogPlatform_IdentityDb];
GO

IF USER_ID(N'IIS APPPOOL\Genealogy.API') IS NULL
    CREATE USER [IIS APPPOOL\Genealogy.API] FOR LOGIN [IIS APPPOOL\Genealogy.API];
GO

GRANT INSERT ON OBJECT::[auth].[ErrorLogs]
    TO [IIS APPPOOL\Genealogy.API];
GO
