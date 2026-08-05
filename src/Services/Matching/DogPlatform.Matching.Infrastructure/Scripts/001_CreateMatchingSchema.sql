-- ============================================================================
-- DogPlatform.Matching — Exact SQL schema (SQL Server)
-- Generated to match the EF Core configurations under:
--   src/Services/Matching/DogPlatform.Matching.Infrastructure/Persistence/Configurations
-- Run against the MatchingDb database (see ConnectionStrings:MatchingDb).
-- ============================================================================

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'matching')
	EXEC('CREATE SCHEMA matching');
GO

-- ── matching.MatchingProfiles ───────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID('matching.MatchingProfiles'))
BEGIN
	CREATE TABLE matching.MatchingProfiles
	(
		MatchingProfileId               UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
		PetId                            UNIQUEIDENTIFIER NOT NULL,
		OwnerId                          UNIQUEIDENTIFIER NOT NULL,
		IsActive                         BIT              NOT NULL,
		MinimumAgeMonths                 INT              NOT NULL,
		MaximumAgeMonths                 INT              NOT NULL,
		RequirePedigree                  BIT              NOT NULL,
		RequireGenealogyValidation       BIT              NOT NULL,
		MaximumEstimatedInbreedingCoefficient FLOAT       NOT NULL,
		MinimumCompatibilityScore        INT              NOT NULL,
		CreatedAt                        DATETIME2        NOT NULL,
		UpdatedAt                        DATETIME2        NULL,

		CONSTRAINT CK_MatchingProfiles_AgeRange
			CHECK ([MinimumAgeMonths] <= [MaximumAgeMonths]),
		CONSTRAINT CK_MatchingProfiles_InbreedingCoefficient
			CHECK ([MaximumEstimatedInbreedingCoefficient] >= 0 AND [MaximumEstimatedInbreedingCoefficient] <= 1),
		CONSTRAINT CK_MatchingProfiles_Score
			CHECK ([MinimumCompatibilityScore] >= 0 AND [MinimumCompatibilityScore] <= 100)
	);

	CREATE UNIQUE INDEX IX_MatchingProfiles_PetId ON matching.MatchingProfiles (PetId);
	CREATE INDEX IX_MatchingProfiles_OwnerId_IsActive ON matching.MatchingProfiles (OwnerId, IsActive);
END
GO

-- ── matching.MatchingProfileBreedPreferences ────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID('matching.MatchingProfileBreedPreferences'))
BEGIN
	CREATE TABLE matching.MatchingProfileBreedPreferences
	(
		MatchingProfileBreedPreferenceId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
		MatchingProfileId                UNIQUEIDENTIFIER NOT NULL,
		BreedId                          INT              NOT NULL,

		CONSTRAINT FK_MatchingProfileBreedPreferences_MatchingProfiles
			FOREIGN KEY (MatchingProfileId)
			REFERENCES matching.MatchingProfiles (MatchingProfileId)
			ON DELETE CASCADE
	);

	CREATE UNIQUE INDEX IX_MatchingProfileBreedPreferences_ProfileId_BreedId
		ON matching.MatchingProfileBreedPreferences (MatchingProfileId, BreedId);
END
GO

-- ── matching.FavoriteCandidates ─────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID('matching.FavoriteCandidates'))
BEGIN
	CREATE TABLE matching.FavoriteCandidates
	(
		FavoriteCandidateId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
		SourcePetId         UNIQUEIDENTIFIER NOT NULL,
		SourceOwnerId       UNIQUEIDENTIFIER NOT NULL,
		CandidatePetId      UNIQUEIDENTIFIER NOT NULL,
		CreatedAt           DATETIME2        NOT NULL
	);

	CREATE UNIQUE INDEX IX_FavoriteCandidates_SourcePetId_CandidatePetId
		ON matching.FavoriteCandidates (SourcePetId, CandidatePetId);
END
GO

-- ── matching.MatchRequests ───────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID('matching.MatchRequests'))
BEGIN
	CREATE TABLE matching.MatchRequests
	(
		MatchRequestId                          UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
		RequesterPetId                          UNIQUEIDENTIFIER NOT NULL,
		RequesterOwnerId                        UNIQUEIDENTIFIER NOT NULL,
		CandidatePetId                          UNIQUEIDENTIFIER NOT NULL,
		CandidateOwnerId                        UNIQUEIDENTIFIER NOT NULL,
		Status                                  NVARCHAR(20)     NOT NULL,
		Message                                 NVARCHAR(500)    NULL,
		CompatibilityScoreSnapshot              INT              NOT NULL,
		EstimatedInbreedingCoefficientSnapshot  FLOAT            NOT NULL,
		RelationshipTypeSnapshot                NVARCHAR(40)     NOT NULL,
		CreatedAt                               DATETIME2        NOT NULL,
		UpdatedAt                               DATETIME2        NULL,
		RespondedAt                             DATETIME2        NULL,
		CancelledAt                             DATETIME2        NULL,
		ExpiresAt                               DATETIME2        NULL,

		CONSTRAINT CK_MatchRequests_CompatibilityScore
			CHECK ([CompatibilityScoreSnapshot] >= 0 AND [CompatibilityScoreSnapshot] <= 100),
		CONSTRAINT CK_MatchRequests_InbreedingCoefficient
			CHECK ([EstimatedInbreedingCoefficientSnapshot] >= 0 AND [EstimatedInbreedingCoefficientSnapshot] <= 1)
	);

	CREATE INDEX IX_MatchRequests_RequesterPetId_CandidatePetId_Status
		ON matching.MatchRequests (RequesterPetId, CandidatePetId, Status);
	CREATE INDEX IX_MatchRequests_RequesterOwnerId_Status_CreatedAt
		ON matching.MatchRequests (RequesterOwnerId, Status, CreatedAt);
	CREATE INDEX IX_MatchRequests_CandidateOwnerId_Status_CreatedAt
		ON matching.MatchRequests (CandidateOwnerId, Status, CreatedAt);
END
GO

-- ── matching.MatchRequestStatusHistory ──────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID('matching.MatchRequestStatusHistory'))
BEGIN
	CREATE TABLE matching.MatchRequestStatusHistory
	(
		MatchRequestStatusHistoryId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
		MatchRequestId              UNIQUEIDENTIFIER NOT NULL,
		Status                      NVARCHAR(20)     NOT NULL,
		OccurredAt                  DATETIME2        NOT NULL,

		CONSTRAINT FK_MatchRequestStatusHistory_MatchRequests
			FOREIGN KEY (MatchRequestId)
			REFERENCES matching.MatchRequests (MatchRequestId)
			ON DELETE CASCADE
	);

	CREATE INDEX IX_MatchRequestStatusHistory_MatchRequestId
		ON matching.MatchRequestStatusHistory (MatchRequestId);
END
GO
