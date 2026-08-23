/*
  DogPlatform.Health - least-privilege SQL Server permissions for its IIS pool.
  Run after CreateHealthDatabase.sql with a SQL Server administrator account.
*/
USE [master];
GO

SET NOCOUNT ON;

IF SUSER_ID(N'IIS APPPOOL\Health.API') IS NULL
BEGIN
    CREATE LOGIN [IIS APPPOOL\Health.API] FROM WINDOWS;
END;
GO

USE [DogPlatform_HealthDb];
GO

IF USER_ID(N'IIS APPPOOL\Health.API') IS NULL
BEGIN
    CREATE USER [IIS APPPOOL\Health.API] FOR LOGIN [IIS APPPOOL\Health.API];
END;
GO

GRANT SELECT, INSERT, UPDATE, DELETE
    ON SCHEMA::[health]
    TO [IIS APPPOOL\Health.API];
GO

USE [DogPlatform_IdentityDb];
GO

IF USER_ID(N'IIS APPPOOL\Health.API') IS NULL
BEGIN
    CREATE USER [IIS APPPOOL\Health.API] FOR LOGIN [IIS APPPOOL\Health.API];
END;
GO

GRANT INSERT
    ON OBJECT::[auth].[ErrorLogs]
    TO [IIS APPPOOL\Health.API];
GO

/*
-- Optional verification (does not display secrets):
SELECT [name], [type_desc]
FROM [master].[sys].[server_principals]
WHERE [name] = N'IIS APPPOOL\Health.API';

USE [DogPlatform_HealthDb];
SELECT [name], [type_desc]
FROM [sys].[database_principals]
WHERE [name] = N'IIS APPPOOL\Health.API';

SELECT USER_NAME([grantee_principal_id]) AS [Principal],
       [permission_name], [state_desc], [class_desc]
FROM [sys].[database_permissions]
WHERE [grantee_principal_id] = USER_ID(N'IIS APPPOOL\Health.API');

USE [DogPlatform_IdentityDb];
SELECT USER_NAME([grantee_principal_id]) AS [Principal],
       [permission_name], [state_desc],
       OBJECT_SCHEMA_NAME([major_id]) AS [SchemaName],
       OBJECT_NAME([major_id]) AS [ObjectName]
FROM [sys].[database_permissions]
WHERE [grantee_principal_id] = USER_ID(N'IIS APPPOOL\Health.API');
*/
