/* DogPlatform.Matching MVP extensions. Idempotent; do not execute automatically. */
USE [DogPlatform_MatchingDb];
GO
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    IF COL_LENGTH(N'matching.MatchingProfiles', N'LookingForSex') IS NULL
        ALTER TABLE matching.MatchingProfiles ADD LookingForSex nvarchar(1) NULL;
    IF COL_LENGTH(N'matching.MatchingProfiles', N'AllowMixedBreed') IS NULL
        ALTER TABLE matching.MatchingProfiles ADD AllowMixedBreed bit NOT NULL
            CONSTRAINT DF_MatchingProfiles_AllowMixedBreed DEFAULT (1);
    IF COL_LENGTH(N'matching.MatchingProfiles', N'Description') IS NULL
        ALTER TABLE matching.MatchingProfiles ADD Description nvarchar(1000) NULL;
    IF COL_LENGTH(N'matching.MatchingProfiles', N'AvailableFromUtc') IS NULL
        ALTER TABLE matching.MatchingProfiles ADD AvailableFromUtc datetime2 NULL;
    IF COL_LENGTH(N'matching.MatchRequests', N'RequesterSharePhoneNumber') IS NULL
        ALTER TABLE matching.MatchRequests ADD RequesterSharePhoneNumber bit NOT NULL
            CONSTRAINT DF_MatchRequests_RequesterSharePhoneNumber DEFAULT (0);

    IF OBJECT_ID(N'matching.PetMatches', N'U') IS NULL
    BEGIN
        CREATE TABLE matching.PetMatches
        (
            MatchId uniqueidentifier NOT NULL CONSTRAINT PK_PetMatches PRIMARY KEY,
            MatchRequestId uniqueidentifier NOT NULL,
            Pet1Id uniqueidentifier NOT NULL,
            Pet2Id uniqueidentifier NOT NULL,
            Owner1Id uniqueidentifier NOT NULL,
            Owner2Id uniqueidentifier NOT NULL,
            Owner1ShareDisplayName bit NOT NULL CONSTRAINT DF_PetMatches_Owner1Display DEFAULT (1),
            Owner1SharePhoneNumber bit NOT NULL CONSTRAINT DF_PetMatches_Owner1Phone DEFAULT (0),
            Owner2ShareDisplayName bit NOT NULL CONSTRAINT DF_PetMatches_Owner2Display DEFAULT (1),
            Owner2SharePhoneNumber bit NOT NULL CONSTRAINT DF_PetMatches_Owner2Phone DEFAULT (0),
            Status nvarchar(20) NOT NULL,
            CreatedAtUtc datetime2 NOT NULL,
            CONSTRAINT FK_PetMatches_MatchRequests FOREIGN KEY (MatchRequestId)
                REFERENCES matching.MatchRequests(MatchRequestId),
            CONSTRAINT CK_PetMatches_DifferentPets CHECK (Pet1Id <> Pet2Id),
            CONSTRAINT CK_PetMatches_DifferentOwners CHECK (Owner1Id <> Owner2Id),
            CONSTRAINT CK_PetMatches_Status CHECK (Status IN (N'Active', N'Cancelled'))
        );
    END;

    IF OBJECT_ID(N'matching.BreedingIntents', N'U') IS NULL
    BEGIN
        CREATE TABLE matching.BreedingIntents
        (
            BreedingIntentId uniqueidentifier NOT NULL CONSTRAINT PK_BreedingIntents PRIMARY KEY,
            MatchId uniqueidentifier NOT NULL,
            OpenMatchId uniqueidentifier NULL,
            ProposerOwnerId uniqueidentifier NOT NULL,
            Status nvarchar(20) NOT NULL,
            Notes nvarchar(1000) NULL,
            ExpectedDateUtc datetime2 NULL,
            CreatedAtUtc datetime2 NOT NULL,
            AcceptedAtUtc datetime2 NULL,
            CancelledAtUtc datetime2 NULL,
            CONSTRAINT FK_BreedingIntents_PetMatches FOREIGN KEY (MatchId)
                REFERENCES matching.PetMatches(MatchId),
            CONSTRAINT CK_BreedingIntents_Status
                CHECK (Status IN (N'Proposed', N'Agreed', N'Cancelled', N'Completed'))
        );
    END;

    IF COL_LENGTH(N'matching.BreedingIntents', N'OpenMatchId') IS NULL
        ALTER TABLE matching.BreedingIntents ADD OpenMatchId uniqueidentifier NULL;

    EXEC(N'
        UPDATE matching.BreedingIntents
            SET OpenMatchId = MatchId
            WHERE Status IN (N''Proposed'', N''Agreed'') AND OpenMatchId IS NULL;
        UPDATE matching.BreedingIntents
            SET OpenMatchId = NULL
            WHERE Status NOT IN (N''Proposed'', N''Agreed'') AND OpenMatchId IS NOT NULL;
    ');

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.check_constraints
        WHERE parent_object_id = OBJECT_ID(N'matching.BreedingIntents')
          AND name = N'CK_BreedingIntents_OpenMatchId'
    )
        EXEC(N'
            ALTER TABLE matching.BreedingIntents WITH CHECK
                ADD CONSTRAINT CK_BreedingIntents_OpenMatchId CHECK
                (
                    (Status IN (N''Proposed'', N''Agreed'') AND OpenMatchId = MatchId)
                    OR
                    (Status NOT IN (N''Proposed'', N''Agreed'') AND OpenMatchId IS NULL)
                );
        ');

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'matching.MatchRequests') AND name=N'UX_MatchRequests_PendingPair')
        CREATE UNIQUE INDEX UX_MatchRequests_PendingPair
            ON matching.MatchRequests(RequesterPetId, CandidatePetId) WHERE Status=N'Pending';
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'matching.PetMatches') AND name=N'UX_PetMatches_MatchRequestId')
        CREATE UNIQUE INDEX UX_PetMatches_MatchRequestId ON matching.PetMatches(MatchRequestId);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'matching.PetMatches') AND name=N'UX_PetMatches_PetPair')
        CREATE UNIQUE INDEX UX_PetMatches_PetPair ON matching.PetMatches(Pet1Id, Pet2Id);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'matching.PetMatches') AND name=N'IX_PetMatches_Owner1Id_Status')
        CREATE INDEX IX_PetMatches_Owner1Id_Status ON matching.PetMatches(Owner1Id, Status);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'matching.PetMatches') AND name=N'IX_PetMatches_Owner2Id_Status')
        CREATE INDEX IX_PetMatches_Owner2Id_Status ON matching.PetMatches(Owner2Id, Status);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'matching.BreedingIntents') AND name=N'IX_BreedingIntents_MatchId_Status')
        CREATE INDEX IX_BreedingIntents_MatchId_Status ON matching.BreedingIntents(MatchId, Status);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'matching.BreedingIntents') AND name=N'IX_BreedingIntents_ProposerOwnerId')
        CREATE INDEX IX_BreedingIntents_ProposerOwnerId ON matching.BreedingIntents(ProposerOwnerId);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'matching.BreedingIntents') AND name=N'UX_BreedingIntents_OpenMatchId')
        EXEC(N'
            CREATE UNIQUE INDEX UX_BreedingIntents_OpenMatchId
                ON matching.BreedingIntents(OpenMatchId)
                WHERE OpenMatchId IS NOT NULL;
        ');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
