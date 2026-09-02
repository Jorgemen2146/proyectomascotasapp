SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;

    IF COL_LENGTH(N'auth.Users', N'PasswordHash') IS NOT NULL
        ALTER TABLE auth.Users ALTER COLUMN PasswordHash nvarchar(max) NULL;

    IF COL_LENGTH(N'auth.Users', N'PasswordSalt') IS NOT NULL
        ALTER TABLE auth.Users ALTER COLUMN PasswordSalt nvarchar(500) NULL;

    IF OBJECT_ID(N'auth.ExternalLogins', N'U') IS NULL
    BEGIN
        CREATE TABLE auth.ExternalLogins
        (
            ExternalLoginId uniqueidentifier NOT NULL CONSTRAINT PK_ExternalLogins PRIMARY KEY,
            UserId uniqueidentifier NOT NULL,
            Provider nvarchar(20) NOT NULL,
            ProviderUserId nvarchar(255) NOT NULL,
            EmailAtLinkTime nvarchar(200) NULL,
            CreatedAtUtc datetime2 NOT NULL,
            UpdatedAtUtc datetime2 NULL,
            CONSTRAINT FK_ExternalLogins_Users_UserId FOREIGN KEY (UserId)
                REFERENCES auth.Users(UserId) ON DELETE CASCADE,
            CONSTRAINT CK_ExternalLogins_Provider
                CHECK (Provider IN (N'Google', N'Facebook', N'Apple'))
        );

        CREATE UNIQUE INDEX UX_ExternalLogins_Provider_ProviderUserId
            ON auth.ExternalLogins(Provider, ProviderUserId);
        CREATE INDEX IX_ExternalLogins_UserId ON auth.ExternalLogins(UserId);
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
