USE [DogPlatform_IdentityDb];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'[auth].[Users]', N'U') IS NULL
BEGIN
    THROW 52001, 'Required table [auth].[Users] does not exist.', 1;
END;

CREATE TABLE #ExistingUsers
(
    [UserId] uniqueidentifier NOT NULL PRIMARY KEY
);

DECLARE @DeploymentUtc datetime2(7) = SYSUTCDATETIME();

BEGIN TRY
    BEGIN TRANSACTION;

    IF COL_LENGTH(N'auth.Users', N'EmailConfirmedAt') IS NULL
    BEGIN
        INSERT INTO #ExistingUsers ([UserId])
        SELECT [UserId]
        FROM [auth].[Users];

        ALTER TABLE [auth].[Users]
            ADD [EmailConfirmedAt] datetime2(7) NULL;
    END;

    IF COL_LENGTH(N'auth.Users', N'EmailVerificationCodeHash') IS NULL
    BEGIN
        ALTER TABLE [auth].[Users]
            ADD [EmailVerificationCodeHash] nvarchar(128) NULL;
    END;

    IF COL_LENGTH(N'auth.Users', N'EmailVerificationCodeExpiresAt') IS NULL
    BEGIN
        ALTER TABLE [auth].[Users]
            ADD [EmailVerificationCodeExpiresAt] datetime2(7) NULL;
    END;

    IF COL_LENGTH(N'auth.Users', N'EmailVerificationAttempts') IS NULL
    BEGIN
        ALTER TABLE [auth].[Users]
            ADD [EmailVerificationAttempts] int NOT NULL
                CONSTRAINT [DF_Users_EmailVerificationAttempts] DEFAULT ((0)) WITH VALUES;
    END;

    IF COL_LENGTH(N'auth.Users', N'EmailVerificationLastSentAt') IS NULL
    BEGIN
        ALTER TABLE [auth].[Users]
            ADD [EmailVerificationLastSentAt] datetime2(7) NULL;
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.default_constraints AS dc
        INNER JOIN sys.columns AS c
            ON c.object_id = dc.parent_object_id
           AND c.column_id = dc.parent_column_id
        WHERE dc.parent_object_id = OBJECT_ID(N'[auth].[Users]')
          AND c.name = N'EmailVerificationAttempts'
    )
    BEGIN
        ALTER TABLE [auth].[Users]
            ADD CONSTRAINT [DF_Users_EmailVerificationAttempts]
            DEFAULT ((0)) FOR [EmailVerificationAttempts];
    END;

    EXEC sys.sp_executesql
        N'UPDATE u
          SET
              u.[IsEmailConfirmed] = 1,
              u.[EmailConfirmedAt] = COALESCE(u.[EmailConfirmedAt], @DeploymentUtc)
          FROM [auth].[Users] AS u
          INNER JOIN #ExistingUsers AS existing ON existing.[UserId] = u.[UserId]
          WHERE u.[IsEmailConfirmed] = 0;',
        N'@DeploymentUtc datetime2(7)',
        @DeploymentUtc;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
    BEGIN
        ROLLBACK TRANSACTION;
    END;

    THROW;
END CATCH;

SELECT
    c.[ORDINAL_POSITION],
    c.[COLUMN_NAME],
    c.[DATA_TYPE],
    c.[CHARACTER_MAXIMUM_LENGTH],
    c.[DATETIME_PRECISION],
    c.[IS_NULLABLE],
    c.[COLUMN_DEFAULT]
FROM [INFORMATION_SCHEMA].[COLUMNS] AS c
WHERE c.[TABLE_SCHEMA] = N'auth'
  AND c.[TABLE_NAME] = N'Users'
  AND c.[COLUMN_NAME] IN
  (
      N'IsEmailConfirmed',
      N'EmailConfirmedAt',
      N'EmailVerificationCodeHash',
      N'EmailVerificationCodeExpiresAt',
      N'EmailVerificationAttempts',
      N'EmailVerificationLastSentAt'
  )
ORDER BY c.[ORDINAL_POSITION];

SELECT
    COUNT_BIG(*) AS [UsersCount],
    SUM(CASE WHEN [IsEmailConfirmed] = 1 THEN 1 ELSE 0 END) AS [ConfirmedUsersCount],
    SUM(CASE WHEN [IsEmailConfirmed] = 0 THEN 1 ELSE 0 END) AS [UnconfirmedUsersCount]
FROM [auth].[Users];
