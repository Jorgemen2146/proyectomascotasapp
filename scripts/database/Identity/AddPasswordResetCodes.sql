/*
  DogPlatform.Identity - password reset codes.
  Idempotent DDL only. This script is not executed automatically.
  Reset codes are stored only as keyed hashes; plaintext codes are never persisted.
*/
USE [DogPlatform_IdentityDb];
GO

SET XACT_ABORT ON;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'auth.PasswordResetCodes', N'U') IS NULL
    BEGIN
        CREATE TABLE auth.PasswordResetCodes
        (
            PasswordResetCodeId uniqueidentifier NOT NULL
                CONSTRAINT PK_PasswordResetCodes PRIMARY KEY,
            UserId uniqueidentifier NOT NULL,
            CodeHash nvarchar(128) NOT NULL,
            CreatedAtUtc datetime2 NOT NULL,
            ExpiresAtUtc datetime2 NOT NULL,
            UsedAtUtc datetime2 NULL,
            FailedAttempts int NOT NULL
                CONSTRAINT DF_PasswordResetCodes_FailedAttempts DEFAULT (0),
            IsRevoked bit NOT NULL
                CONSTRAINT DF_PasswordResetCodes_IsRevoked DEFAULT (0),
            CreatedFromIp nvarchar(45) NULL,
            CONSTRAINT FK_PasswordResetCodes_Users
                FOREIGN KEY (UserId) REFERENCES auth.Users(UserId),
            CONSTRAINT CK_PasswordResetCodes_FailedAttempts
                CHECK (FailedAttempts >= 0),
            CONSTRAINT CK_PasswordResetCodes_Expiration
                CHECK (ExpiresAtUtc > CreatedAtUtc)
        );
    END;

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'auth.PasswordResetCodes')
          AND name = N'IX_PasswordResetCodes_UserId')
        CREATE INDEX IX_PasswordResetCodes_UserId
            ON auth.PasswordResetCodes(UserId);

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'auth.PasswordResetCodes')
          AND name = N'IX_PasswordResetCodes_ExpiresAtUtc')
        CREATE INDEX IX_PasswordResetCodes_ExpiresAtUtc
            ON auth.PasswordResetCodes(ExpiresAtUtc);

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'auth.PasswordResetCodes')
          AND name = N'IX_PasswordResetCodes_UsedAtUtc')
        CREATE INDEX IX_PasswordResetCodes_UsedAtUtc
            ON auth.PasswordResetCodes(UsedAtUtc);

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
