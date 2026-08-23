/*
  DogPlatform.Health - complete database, schema, tables and technical seed.
  Run from SSMS with a SQL Server administrator account.
  Idempotent: existing database objects and seed rows are preserved.
*/
USE [master];
GO

SET NOCOUNT ON;

IF DB_ID(N'DogPlatform_HealthDb') IS NULL
BEGIN
    CREATE DATABASE [DogPlatform_HealthDb];
END;
GO

USE [DogPlatform_HealthDb];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF SCHEMA_ID(N'health') IS NULL
        EXEC(N'CREATE SCHEMA [health]');

    IF OBJECT_ID(N'[health].[Vaccines]', N'U') IS NULL
    BEGIN
        CREATE TABLE [health].[Vaccines]
        (
            [VaccineId] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Vaccines] PRIMARY KEY,
            [SpeciesId] int NOT NULL,
            [Name] nvarchar(150) NOT NULL,
            [Description] nvarchar(500) NULL,
            [IsCore] bit NOT NULL CONSTRAINT [DF_Vaccines_IsCore] DEFAULT (0),
            [IsActive] bit NOT NULL CONSTRAINT [DF_Vaccines_IsActive] DEFAULT (1),
            [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_Vaccines_CreatedAt] DEFAULT (SYSUTCDATETIME()),
            [UpdatedAt] datetime2 NULL,
            CONSTRAINT [CK_Vaccines_SpeciesId] CHECK ([SpeciesId] IN (1, 2))
        );
    END;

    IF OBJECT_ID(N'[health].[VaccineSchedules]', N'U') IS NULL
    BEGIN
        CREATE TABLE [health].[VaccineSchedules]
        (
            [VaccineScheduleId] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_VaccineSchedules] PRIMARY KEY,
            [VaccineId] int NOT NULL,
            [DoseNumber] int NOT NULL,
            [MinAgeWeeks] int NULL,
            [IntervalDays] int NULL,
            [BoosterIntervalDays] int NULL,
            [IsActive] bit NOT NULL CONSTRAINT [DF_VaccineSchedules_IsActive] DEFAULT (1),
            [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_VaccineSchedules_CreatedAt] DEFAULT (SYSUTCDATETIME()),
            [UpdatedAt] datetime2 NULL,
            CONSTRAINT [FK_VaccineSchedules_Vaccines]
                FOREIGN KEY ([VaccineId]) REFERENCES [health].[Vaccines] ([VaccineId]),
            CONSTRAINT [CK_VaccineSchedules_DoseNumber] CHECK ([DoseNumber] > 0),
            CONSTRAINT [CK_VaccineSchedules_MinAgeWeeks]
                CHECK ([MinAgeWeeks] IS NULL OR [MinAgeWeeks] >= 0),
            CONSTRAINT [CK_VaccineSchedules_IntervalDays]
                CHECK ([IntervalDays] IS NULL OR [IntervalDays] > 0),
            CONSTRAINT [CK_VaccineSchedules_BoosterIntervalDays]
                CHECK ([BoosterIntervalDays] IS NULL OR [BoosterIntervalDays] > 0)
        );
    END;

    IF OBJECT_ID(N'[health].[PetVaccinations]', N'U') IS NULL
    BEGIN
        CREATE TABLE [health].[PetVaccinations]
        (
            [PetVaccinationId] uniqueidentifier NOT NULL CONSTRAINT [PK_PetVaccinations] PRIMARY KEY,
            [PetId] uniqueidentifier NOT NULL,
            [VaccineId] int NOT NULL,
            [DoseNumber] int NULL,
            [AppliedAtUtc] datetime2 NOT NULL,
            [NextDueAtUtc] datetime2 NULL,
            [VeterinarianName] nvarchar(200) NULL,
            [ClinicName] nvarchar(200) NULL,
            [BatchNumber] nvarchar(100) NULL,
            [Notes] nvarchar(1000) NULL,
            [CreatedAtUtc] datetime2 NOT NULL CONSTRAINT [DF_PetVaccinations_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
            [UpdatedAtUtc] datetime2 NULL,
            [IsDeleted] bit NOT NULL CONSTRAINT [DF_PetVaccinations_IsDeleted] DEFAULT (0),
            CONSTRAINT [FK_PetVaccinations_Vaccines]
                FOREIGN KEY ([VaccineId]) REFERENCES [health].[Vaccines] ([VaccineId]),
            CONSTRAINT [CK_PetVaccinations_DoseNumber]
                CHECK ([DoseNumber] IS NULL OR [DoseNumber] > 0)
        );
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [object_id] = OBJECT_ID(N'[health].[Vaccines]') AND [name] = N'UX_Vaccines_SpeciesId_Name')
        CREATE UNIQUE INDEX [UX_Vaccines_SpeciesId_Name]
            ON [health].[Vaccines] ([SpeciesId], [Name]);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [object_id] = OBJECT_ID(N'[health].[Vaccines]') AND [name] = N'IX_Vaccines_SpeciesId_IsActive')
        CREATE INDEX [IX_Vaccines_SpeciesId_IsActive]
            ON [health].[Vaccines] ([SpeciesId], [IsActive]);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [object_id] = OBJECT_ID(N'[health].[VaccineSchedules]') AND [name] = N'UX_VaccineSchedules_VaccineId_DoseNumber')
        CREATE UNIQUE INDEX [UX_VaccineSchedules_VaccineId_DoseNumber]
            ON [health].[VaccineSchedules] ([VaccineId], [DoseNumber]);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [object_id] = OBJECT_ID(N'[health].[PetVaccinations]') AND [name] = N'IX_PetVaccinations_PetId_AppliedAtUtc')
        CREATE INDEX [IX_PetVaccinations_PetId_AppliedAtUtc]
            ON [health].[PetVaccinations] ([PetId], [AppliedAtUtc]);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [object_id] = OBJECT_ID(N'[health].[PetVaccinations]') AND [name] = N'IX_PetVaccinations_PetId_VaccineId')
        CREATE INDEX [IX_PetVaccinations_PetId_VaccineId]
            ON [health].[PetVaccinations] ([PetId], [VaccineId]);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [object_id] = OBJECT_ID(N'[health].[PetVaccinations]') AND [name] = N'IX_PetVaccinations_NextDueAtUtc')
        CREATE INDEX [IX_PetVaccinations_NextDueAtUtc]
            ON [health].[PetVaccinations] ([NextDueAtUtc]);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [object_id] = OBJECT_ID(N'[health].[PetVaccinations]') AND [name] = N'IX_PetVaccinations_IsDeleted')
        CREATE INDEX [IX_PetVaccinations_IsDeleted]
            ON [health].[PetVaccinations] ([IsDeleted]);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [object_id] = OBJECT_ID(N'[health].[PetVaccinations]') AND [name] = N'UX_PetVaccinations_ExactActive')
        CREATE UNIQUE INDEX [UX_PetVaccinations_ExactActive]
            ON [health].[PetVaccinations] ([PetId], [VaccineId], [AppliedAtUtc], [DoseNumber])
            WHERE [IsDeleted] = 0;

    /*
      Technical/configurable initial data. These schedules are not universal
      veterinary recommendations and must be reviewed for each deployment.
      Existing rows are not overwritten.
    */
    DECLARE @Vaccines TABLE
    (
        [SpeciesId] int NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [Description] nvarchar(500) NULL,
        [IsCore] bit NOT NULL
    );

    INSERT INTO @Vaccines ([SpeciesId], [Name], [Description], [IsCore])
    VALUES
        (1, N'Rabia', N'Vacuna contra la rabia.', 1),
        (1, N'Parvovirus', N'Vacuna contra el parvovirus canino.', 1),
        (1, N'Moquillo', N'Vacuna contra el virus del moquillo canino.', 1),
        (1, N'Adenovirus', N'Vacuna contra el adenovirus canino.', 1),
        (1, N'Leptospirosis', N'Vacuna configurable segun riesgo epidemiologico local.', 0),
        (1, N'Bordetella', N'Vacuna configurable segun exposicion y criterio veterinario.', 0),
        (2, N'Rabia', N'Vacuna contra la rabia.', 1),
        (2, N'Panleucopenia', N'Vacuna contra la panleucopenia felina.', 1),
        (2, N'Herpesvirus Felino', N'Vacuna contra el herpesvirus felino.', 1),
        (2, N'Calicivirus Felino', N'Vacuna contra el calicivirus felino.', 1),
        (2, N'Leucemia Felina (FeLV)', N'Vacuna configurable segun edad, exposicion y pruebas veterinarias.', 0);

    INSERT INTO [health].[Vaccines]
        ([SpeciesId], [Name], [Description], [IsCore], [IsActive], [CreatedAt])
    SELECT source.[SpeciesId], source.[Name], source.[Description], source.[IsCore], 1, SYSUTCDATETIME()
    FROM @Vaccines AS source
    WHERE NOT EXISTS
    (
        SELECT 1 FROM [health].[Vaccines] AS target
        WHERE target.[SpeciesId] = source.[SpeciesId]
          AND target.[Name] = source.[Name]
    );

    DECLARE @Schedules TABLE
    (
        [SpeciesId] int NOT NULL,
        [VaccineName] nvarchar(150) NOT NULL,
        [DoseNumber] int NOT NULL,
        [MinAgeWeeks] int NULL,
        [IntervalDays] int NULL,
        [BoosterIntervalDays] int NULL
    );

    INSERT INTO @Schedules
        ([SpeciesId], [VaccineName], [DoseNumber], [MinAgeWeeks], [IntervalDays], [BoosterIntervalDays])
    VALUES
        (1, N'Rabia', 1, 12, NULL, 365),
        (1, N'Parvovirus', 1, 6, NULL, NULL),
        (1, N'Parvovirus', 2, 10, 28, NULL),
        (1, N'Parvovirus', 3, 14, 28, 365),
        (1, N'Moquillo', 1, 6, NULL, NULL),
        (1, N'Moquillo', 2, 10, 28, NULL),
        (1, N'Moquillo', 3, 14, 28, 365),
        (1, N'Adenovirus', 1, 6, NULL, NULL),
        (1, N'Adenovirus', 2, 10, 28, NULL),
        (1, N'Adenovirus', 3, 14, 28, 365),
        (1, N'Leptospirosis', 1, 8, NULL, NULL),
        (1, N'Leptospirosis', 2, 12, 28, 365),
        (1, N'Bordetella', 1, 8, NULL, 365),
        (2, N'Rabia', 1, 12, NULL, 365),
        (2, N'Panleucopenia', 1, 6, NULL, NULL),
        (2, N'Panleucopenia', 2, 10, 28, NULL),
        (2, N'Panleucopenia', 3, 14, 28, 365),
        (2, N'Herpesvirus Felino', 1, 6, NULL, NULL),
        (2, N'Herpesvirus Felino', 2, 10, 28, NULL),
        (2, N'Herpesvirus Felino', 3, 14, 28, 365),
        (2, N'Calicivirus Felino', 1, 6, NULL, NULL),
        (2, N'Calicivirus Felino', 2, 10, 28, NULL),
        (2, N'Calicivirus Felino', 3, 14, 28, 365),
        (2, N'Leucemia Felina (FeLV)', 1, 8, NULL, NULL),
        (2, N'Leucemia Felina (FeLV)', 2, 12, 28, 365);

    INSERT INTO [health].[VaccineSchedules]
        ([VaccineId], [DoseNumber], [MinAgeWeeks], [IntervalDays], [BoosterIntervalDays], [IsActive], [CreatedAt])
    SELECT vaccine.[VaccineId], schedule.[DoseNumber], schedule.[MinAgeWeeks],
           schedule.[IntervalDays], schedule.[BoosterIntervalDays], 1, SYSUTCDATETIME()
    FROM @Schedules AS schedule
    INNER JOIN [health].[Vaccines] AS vaccine
        ON vaccine.[SpeciesId] = schedule.[SpeciesId]
       AND vaccine.[Name] = schedule.[VaccineName]
    WHERE NOT EXISTS
    (
        SELECT 1 FROM [health].[VaccineSchedules] AS target
        WHERE target.[VaccineId] = vaccine.[VaccineId]
          AND target.[DoseNumber] = schedule.[DoseNumber]
    );

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

/* Informational validation only. */
SELECT DB_NAME() AS [DatabaseName];

SELECT [TABLE_SCHEMA], [TABLE_NAME]
FROM INFORMATION_SCHEMA.TABLES
WHERE [TABLE_SCHEMA] = N'health'
ORDER BY [TABLE_NAME];

SELECT COUNT(*) AS [Vaccines]
FROM [health].[Vaccines];

SELECT COUNT(*) AS [VaccineSchedules]
FROM [health].[VaccineSchedules];
GO
