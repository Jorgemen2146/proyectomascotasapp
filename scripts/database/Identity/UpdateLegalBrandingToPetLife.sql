/*
  Updates only the two existing V1 public legal documents from the former
  commercial name to PetLife. Technical DogPlatform identifiers are untouched.
  Idempotent: no rows or legal-document versions are created.
*/
USE [DogPlatform_IdentityDb];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

DECLARE @Targets TABLE
(
    LegalDocumentId uniqueidentifier NOT NULL PRIMARY KEY,
    Type nvarchar(50) NOT NULL,
    Version nvarchar(20) NOT NULL
);

INSERT @Targets (LegalDocumentId, Type, Version)
VALUES
    ('11111111-1111-4111-8111-111111111111', N'TermsAndConditions', N'1.0'),
    ('22222222-2222-4222-8222-222222222222', N'PrivacyPolicy', N'1.0');

IF OBJECT_ID(N'auth.LegalDocuments', N'U') IS NULL
    THROW 50001, 'auth.LegalDocuments does not exist.', 1;

IF EXISTS
(
    SELECT 1
    FROM @Targets AS target
    LEFT JOIN auth.LegalDocuments AS document
        ON document.LegalDocumentId = target.LegalDocumentId
       AND document.Type = target.Type
       AND document.Version = target.Version
    WHERE document.LegalDocumentId IS NULL
)
    THROW 50002, 'One or more expected V1 legal documents were not found.', 1;

-- Preview the exact rows before the update.
SELECT document.LegalDocumentId, document.Type, document.Version, document.Title,
       (LEN(document.Title) - LEN(REPLACE(document.Title COLLATE Latin1_General_100_BIN2,
           N'DogPlatform', N''))) / LEN(N'DogPlatform') AS TitleBrandMentions,
       (LEN(document.Content) - LEN(REPLACE(document.Content COLLATE Latin1_General_100_BIN2,
           N'DogPlatform', N''))) / LEN(N'DogPlatform') AS ContentBrandMentions
FROM auth.LegalDocuments AS document
INNER JOIN @Targets AS target
    ON target.LegalDocumentId = document.LegalDocumentId
   AND target.Type = document.Type
   AND target.Version = document.Version;

BEGIN TRY
    BEGIN TRANSACTION;

    UPDATE document
    SET Title = REPLACE(REPLACE(REPLACE(REPLACE(
                    document.Title COLLATE Latin1_General_100_BIN2,
                    N'DOGPLATFORM', N'PETLIFE'),
                    N'DogPlatform', N'PetLife'),
                    N'dogplatform', N'petlife'),
                    N'Dog Platform', N'PetLife'),
        Content = REPLACE(REPLACE(REPLACE(REPLACE(
                    document.Content COLLATE Latin1_General_100_BIN2,
                    N'DOGPLATFORM', N'PETLIFE'),
                    N'DogPlatform', N'PetLife'),
                    N'dogplatform', N'petlife'),
                    N'Dog Platform', N'PetLife')
    FROM auth.LegalDocuments AS document
    INNER JOIN @Targets AS target
        ON target.LegalDocumentId = document.LegalDocumentId
       AND target.Type = document.Type
       AND target.Version = document.Version
    WHERE document.Title COLLATE Latin1_General_100_BIN2 LIKE N'%DogPlatform%'
       OR document.Title COLLATE Latin1_General_100_BIN2 LIKE N'%DOGPLATFORM%'
       OR document.Title COLLATE Latin1_General_100_BIN2 LIKE N'%dogplatform%'
       OR document.Title COLLATE Latin1_General_100_BIN2 LIKE N'%Dog Platform%'
       OR document.Content COLLATE Latin1_General_100_BIN2 LIKE N'%DogPlatform%'
       OR document.Content COLLATE Latin1_General_100_BIN2 LIKE N'%DOGPLATFORM%'
       OR document.Content COLLATE Latin1_General_100_BIN2 LIKE N'%dogplatform%'
       OR document.Content COLLATE Latin1_General_100_BIN2 LIKE N'%Dog Platform%';

    IF EXISTS
    (
        SELECT 1
        FROM auth.LegalDocuments AS document
        INNER JOIN @Targets AS target
            ON target.LegalDocumentId = document.LegalDocumentId
           AND target.Type = document.Type
           AND target.Version = document.Version
        WHERE document.Title COLLATE Latin1_General_100_BIN2 LIKE N'%DogPlatform%'
           OR document.Title COLLATE Latin1_General_100_BIN2 LIKE N'%DOGPLATFORM%'
           OR document.Title COLLATE Latin1_General_100_BIN2 LIKE N'%dogplatform%'
           OR document.Title COLLATE Latin1_General_100_BIN2 LIKE N'%Dog Platform%'
           OR document.Content COLLATE Latin1_General_100_BIN2 LIKE N'%DogPlatform%'
           OR document.Content COLLATE Latin1_General_100_BIN2 LIKE N'%DOGPLATFORM%'
           OR document.Content COLLATE Latin1_General_100_BIN2 LIKE N'%dogplatform%'
           OR document.Content COLLATE Latin1_General_100_BIN2 LIKE N'%Dog Platform%'
    )
        THROW 50003, 'Visible legacy branding remains in a target legal document.', 1;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

-- Verify the same exact rows after the update.
SELECT document.LegalDocumentId, document.Type, document.Version, document.Title,
       LEFT(document.Content, 120) AS ContentPreview
FROM auth.LegalDocuments AS document
INNER JOIN @Targets AS target
    ON target.LegalDocumentId = document.LegalDocumentId
   AND target.Type = document.Type
   AND target.Version = document.Version;
GO
