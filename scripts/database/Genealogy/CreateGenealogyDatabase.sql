/* DogPlatform.Genealogy - idempotent SQL Server schema. Do not run automatically. */
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
GO

USE [master];
GO

IF DB_ID(N'DogPlatform_GenealogyDb') IS NULL
    CREATE DATABASE [DogPlatform_GenealogyDb];
GO

USE [DogPlatform_GenealogyDb];
GO

IF SCHEMA_ID(N'genealogy') IS NULL EXEC(N'CREATE SCHEMA genealogy');
GO

IF OBJECT_ID(N'genealogy.PetRelationships', N'U') IS NULL
BEGIN
    CREATE TABLE genealogy.PetRelationships
    (
        RelationshipId uniqueidentifier NOT NULL CONSTRAINT PK_PetRelationships PRIMARY KEY,
        ChildPetId uniqueidentifier NOT NULL,
        ParentPetId uniqueidentifier NOT NULL,
        ParentRole nvarchar(20) NOT NULL,
        Status nvarchar(20) NOT NULL,
        CreatedByUserId uniqueidentifier NOT NULL,
        CreatedAtUtc datetime2 NOT NULL,
        ActivatedAtUtc datetime2 NULL,
        DeletedAtUtc datetime2 NULL,
        CONSTRAINT CK_PetRelationships_NotSelf CHECK (ChildPetId <> ParentPetId),
        CONSTRAINT CK_PetRelationships_ParentRole CHECK (ParentRole IN (N'Father', N'Mother')),
        CONSTRAINT CK_PetRelationships_Status CHECK (Status IN (N'Pending', N'Active', N'Rejected', N'Cancelled'))
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'genealogy.PetRelationships') AND name=N'IX_PetRelationships_ChildPetId')
    CREATE INDEX IX_PetRelationships_ChildPetId ON genealogy.PetRelationships(ChildPetId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'genealogy.PetRelationships') AND name=N'IX_PetRelationships_ParentPetId')
    CREATE INDEX IX_PetRelationships_ParentPetId ON genealogy.PetRelationships(ParentPetId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'genealogy.PetRelationships') AND name=N'IX_PetRelationships_Status')
    CREATE INDEX IX_PetRelationships_Status ON genealogy.PetRelationships(Status);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'genealogy.PetRelationships') AND name=N'UX_PetRelationships_ActiveChildRole')
    CREATE UNIQUE INDEX UX_PetRelationships_ActiveChildRole
        ON genealogy.PetRelationships(ChildPetId, ParentRole)
        WHERE Status=N'Active' AND DeletedAtUtc IS NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'genealogy.PetRelationships') AND name=N'UX_PetRelationships_CurrentPairRole')
    CREATE UNIQUE INDEX UX_PetRelationships_CurrentPairRole
        ON genealogy.PetRelationships(ChildPetId, ParentPetId, ParentRole)
        WHERE DeletedAtUtc IS NULL;
GO

/* One-time idempotent compatibility migration from the former aggregate table. */
IF OBJECT_ID(N'genealogy.PetLineages', N'U') IS NOT NULL
BEGIN
    INSERT INTO genealogy.PetRelationships
        (RelationshipId, ChildPetId, ParentPetId, ParentRole, Status,
         CreatedByUserId, CreatedAtUtc, ActivatedAtUtc, DeletedAtUtc)
    SELECT NEWID(), lineage.PetId, lineage.FatherId, N'Father', N'Active',
           lineage.OwnerId, lineage.CreatedAt, lineage.CreatedAt, NULL
    FROM genealogy.PetLineages lineage
    WHERE lineage.FatherId IS NOT NULL
      AND NOT EXISTS
      (
          SELECT 1 FROM genealogy.PetRelationships relationship
          WHERE relationship.ChildPetId=lineage.PetId
            AND relationship.ParentRole=N'Father'
            AND relationship.Status=N'Active'
            AND relationship.DeletedAtUtc IS NULL
      );

    INSERT INTO genealogy.PetRelationships
        (RelationshipId, ChildPetId, ParentPetId, ParentRole, Status,
         CreatedByUserId, CreatedAtUtc, ActivatedAtUtc, DeletedAtUtc)
    SELECT NEWID(), lineage.PetId, lineage.MotherId, N'Mother', N'Active',
           lineage.OwnerId, lineage.CreatedAt, lineage.CreatedAt, NULL
    FROM genealogy.PetLineages lineage
    WHERE lineage.MotherId IS NOT NULL
      AND NOT EXISTS
      (
          SELECT 1 FROM genealogy.PetRelationships relationship
          WHERE relationship.ChildPetId=lineage.PetId
            AND relationship.ParentRole=N'Mother'
            AND relationship.Status=N'Active'
            AND relationship.DeletedAtUtc IS NULL
      );
END;
GO

IF OBJECT_ID(N'genealogy.RelationshipInvitations', N'U') IS NULL
BEGIN
    CREATE TABLE genealogy.RelationshipInvitations
    (
        InvitationId uniqueidentifier NOT NULL CONSTRAINT PK_RelationshipInvitations PRIMARY KEY,
        ChildPetId uniqueidentifier NOT NULL,
        ParentRole nvarchar(20) NOT NULL,
        RequesterUserId uniqueidentifier NOT NULL,
        RequesterDisplayName nvarchar(200) NOT NULL,
        TargetUserId uniqueidentifier NULL,
        TargetEmail nvarchar(320) NOT NULL,
        SelectedTargetPetId uniqueidentifier NULL,
        TokenHash char(64) NOT NULL,
        ExpiresAtUtc datetime2 NOT NULL,
        Status nvarchar(20) NOT NULL,
        CreatedAtUtc datetime2 NOT NULL,
        AcceptedAtUtc datetime2 NULL,
        RejectedAtUtc datetime2 NULL,
        CancelledAtUtc datetime2 NULL,
        CONSTRAINT CK_RelationshipInvitations_ParentRole CHECK (ParentRole IN (N'Father', N'Mother')),
        CONSTRAINT CK_RelationshipInvitations_Status CHECK (Status IN (N'Pending', N'Accepted', N'Rejected', N'Cancelled', N'Expired'))
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'genealogy.RelationshipInvitations') AND name=N'UX_RelationshipInvitations_TokenHash')
    CREATE UNIQUE INDEX UX_RelationshipInvitations_TokenHash ON genealogy.RelationshipInvitations(TokenHash);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'genealogy.RelationshipInvitations') AND name=N'IX_RelationshipInvitations_RequesterUserId')
    CREATE INDEX IX_RelationshipInvitations_RequesterUserId ON genealogy.RelationshipInvitations(RequesterUserId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'genealogy.RelationshipInvitations') AND name=N'IX_RelationshipInvitations_TargetUserId')
    CREATE INDEX IX_RelationshipInvitations_TargetUserId ON genealogy.RelationshipInvitations(TargetUserId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'genealogy.RelationshipInvitations') AND name=N'IX_RelationshipInvitations_Status')
    CREATE INDEX IX_RelationshipInvitations_Status ON genealogy.RelationshipInvitations(Status);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'genealogy.RelationshipInvitations') AND name=N'IX_RelationshipInvitations_ExpiresAtUtc')
    CREATE INDEX IX_RelationshipInvitations_ExpiresAtUtc ON genealogy.RelationshipInvitations(ExpiresAtUtc);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'genealogy.RelationshipInvitations') AND name=N'UX_RelationshipInvitations_PendingEquivalent')
    CREATE UNIQUE INDEX UX_RelationshipInvitations_PendingEquivalent
        ON genealogy.RelationshipInvitations(ChildPetId, ParentRole, TargetEmail)
        WHERE Status=N'Pending';
GO
