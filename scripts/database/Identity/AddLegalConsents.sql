/*
  DogPlatform.Identity - versioned legal documents and user consent evidence.
  Idempotent DDL/seed only. This script is not executed automatically.
  IMPORTANT: Replace both placeholder contents with approved V1 legal text
  before production deployment.
*/
USE [DogPlatform_IdentityDb];
GO

SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'auth.LegalDocuments', N'U') IS NULL
    BEGIN
        CREATE TABLE auth.LegalDocuments
        (
            LegalDocumentId uniqueidentifier NOT NULL CONSTRAINT PK_LegalDocuments PRIMARY KEY,
            Type nvarchar(40) NOT NULL,
            Version nvarchar(30) NOT NULL,
            Title nvarchar(200) NOT NULL,
            Content nvarchar(max) NOT NULL,
            PublishedAtUtc datetime2 NOT NULL,
            EffectiveAtUtc datetime2 NOT NULL,
            IsActive bit NOT NULL,
            RequiresAcceptance bit NOT NULL,
            CreatedAtUtc datetime2 NOT NULL,
            UpdatedAtUtc datetime2 NULL,
            CONSTRAINT CK_LegalDocuments_Type
                CHECK (Type IN (N'TermsAndConditions', N'PrivacyPolicy'))
        );
    END;

    IF OBJECT_ID(N'auth.UserLegalConsents', N'U') IS NULL
    BEGIN
        CREATE TABLE auth.UserLegalConsents
        (
            UserLegalConsentId uniqueidentifier NOT NULL CONSTRAINT PK_UserLegalConsents PRIMARY KEY,
            UserId uniqueidentifier NOT NULL,
            LegalDocumentId uniqueidentifier NOT NULL,
            AcceptedAtUtc datetime2 NOT NULL,
            RevokedAtUtc datetime2 NULL,
            CONSTRAINT FK_UserLegalConsents_Users FOREIGN KEY (UserId)
                REFERENCES auth.Users(UserId),
            CONSTRAINT FK_UserLegalConsents_LegalDocuments FOREIGN KEY (LegalDocumentId)
                REFERENCES auth.LegalDocuments(LegalDocumentId)
        );
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'auth.LegalDocuments') AND name=N'UX_LegalDocuments_Type_Version')
        CREATE UNIQUE INDEX UX_LegalDocuments_Type_Version ON auth.LegalDocuments(Type, Version);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'auth.LegalDocuments') AND name=N'IX_LegalDocuments_IsActive')
        CREATE INDEX IX_LegalDocuments_IsActive ON auth.LegalDocuments(IsActive);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'auth.LegalDocuments') AND name=N'IX_LegalDocuments_Type')
        CREATE INDEX IX_LegalDocuments_Type ON auth.LegalDocuments(Type);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'auth.UserLegalConsents') AND name=N'UX_UserLegalConsents_UserId_LegalDocumentId')
        CREATE UNIQUE INDEX UX_UserLegalConsents_UserId_LegalDocumentId ON auth.UserLegalConsents(UserId, LegalDocumentId);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'auth.UserLegalConsents') AND name=N'IX_UserLegalConsents_UserId')
        CREATE INDEX IX_UserLegalConsents_UserId ON auth.UserLegalConsents(UserId);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'auth.UserLegalConsents') AND name=N'IX_UserLegalConsents_AcceptedAtUtc')
        CREATE INDEX IX_UserLegalConsents_AcceptedAtUtc ON auth.UserLegalConsents(AcceptedAtUtc);

    IF NOT EXISTS (SELECT 1 FROM auth.LegalDocuments WHERE Type=N'TermsAndConditions' AND Version=N'1.0')
    BEGIN
        INSERT auth.LegalDocuments
            (LegalDocumentId, Type, Version, Title, Content, PublishedAtUtc,
             EffectiveAtUtc, IsActive, RequiresAcceptance, CreatedAtUtc)
        VALUES
            ('11111111-1111-4111-8111-111111111111', N'TermsAndConditions', N'1.0',
             N'Términos y Condiciones',
             N'[PLACEHOLDER: insertar aquí el texto legal V1 aprobado de Términos y Condiciones antes de producción]',
             '2026-08-25T00:00:00', '2026-08-25T00:00:00', 1, 1, SYSUTCDATETIME());
    END;

    IF NOT EXISTS (SELECT 1 FROM auth.LegalDocuments WHERE Type=N'PrivacyPolicy' AND Version=N'1.0')
    BEGIN
        INSERT auth.LegalDocuments
            (LegalDocumentId, Type, Version, Title, Content, PublishedAtUtc,
             EffectiveAtUtc, IsActive, RequiresAcceptance, CreatedAtUtc)
        VALUES
            ('22222222-2222-4222-8222-222222222222', N'PrivacyPolicy', N'1.0',
             N'Política de Privacidad',
             N'[PLACEHOLDER: insertar aquí el texto legal V1 aprobado de Política de Privacidad antes de producción]',
             '2026-08-25T00:00:00', '2026-08-25T00:00:00', 1, 1, SYSUTCDATETIME());
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
