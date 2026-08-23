/*
  DogPlatform.Health - Vaccination schema
  Idempotent DDL only. This script is not executed automatically.
*/
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
BEGIN TRANSACTION;

IF SCHEMA_ID(N'health') IS NULL EXEC(N'CREATE SCHEMA health');

IF OBJECT_ID(N'health.Vaccines', N'U') IS NULL
BEGIN
    CREATE TABLE health.Vaccines
    (
        VaccineId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Vaccines PRIMARY KEY,
        SpeciesId int NOT NULL,
        Name nvarchar(150) NOT NULL,
        Description nvarchar(500) NULL,
        IsCore bit NOT NULL CONSTRAINT DF_Vaccines_IsCore DEFAULT (0),
        IsActive bit NOT NULL CONSTRAINT DF_Vaccines_IsActive DEFAULT (1),
        CreatedAt datetime2 NOT NULL CONSTRAINT DF_Vaccines_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt datetime2 NULL,
        CONSTRAINT CK_Vaccines_SpeciesId CHECK (SpeciesId IN (1, 2))
    );
END;

IF OBJECT_ID(N'health.VaccineSchedules', N'U') IS NULL
BEGIN
    CREATE TABLE health.VaccineSchedules
    (
        VaccineScheduleId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_VaccineSchedules PRIMARY KEY,
        VaccineId int NOT NULL,
        DoseNumber int NOT NULL,
        MinAgeWeeks int NULL,
        IntervalDays int NULL,
        BoosterIntervalDays int NULL,
        IsActive bit NOT NULL CONSTRAINT DF_VaccineSchedules_IsActive DEFAULT (1),
        CreatedAt datetime2 NOT NULL CONSTRAINT DF_VaccineSchedules_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt datetime2 NULL,
        CONSTRAINT FK_VaccineSchedules_Vaccines FOREIGN KEY (VaccineId) REFERENCES health.Vaccines(VaccineId),
        CONSTRAINT CK_VaccineSchedules_DoseNumber CHECK (DoseNumber > 0),
        CONSTRAINT CK_VaccineSchedules_MinAgeWeeks CHECK (MinAgeWeeks IS NULL OR MinAgeWeeks >= 0),
        CONSTRAINT CK_VaccineSchedules_IntervalDays CHECK (IntervalDays IS NULL OR IntervalDays > 0),
        CONSTRAINT CK_VaccineSchedules_BoosterIntervalDays CHECK (BoosterIntervalDays IS NULL OR BoosterIntervalDays > 0)
    );
END;

IF OBJECT_ID(N'health.PetVaccinations', N'U') IS NULL
BEGIN
    CREATE TABLE health.PetVaccinations
    (
        PetVaccinationId uniqueidentifier NOT NULL CONSTRAINT PK_PetVaccinations PRIMARY KEY,
        PetId uniqueidentifier NOT NULL,
        VaccineId int NOT NULL,
        DoseNumber int NULL,
        AppliedAtUtc datetime2 NOT NULL,
        NextDueAtUtc datetime2 NULL,
        VeterinarianName nvarchar(200) NULL,
        ClinicName nvarchar(200) NULL,
        BatchNumber nvarchar(100) NULL,
        Notes nvarchar(1000) NULL,
        CreatedAtUtc datetime2 NOT NULL CONSTRAINT DF_PetVaccinations_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        UpdatedAtUtc datetime2 NULL,
        IsDeleted bit NOT NULL CONSTRAINT DF_PetVaccinations_IsDeleted DEFAULT (0),
        CONSTRAINT FK_PetVaccinations_Vaccines FOREIGN KEY (VaccineId) REFERENCES health.Vaccines(VaccineId),
        CONSTRAINT CK_PetVaccinations_DoseNumber CHECK (DoseNumber IS NULL OR DoseNumber > 0)
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'health.Vaccines') AND name = N'UX_Vaccines_SpeciesId_Name')
    CREATE UNIQUE INDEX UX_Vaccines_SpeciesId_Name ON health.Vaccines(SpeciesId, Name);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'health.Vaccines') AND name = N'IX_Vaccines_SpeciesId_IsActive')
    CREATE INDEX IX_Vaccines_SpeciesId_IsActive ON health.Vaccines(SpeciesId, IsActive);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'health.VaccineSchedules') AND name = N'UX_VaccineSchedules_VaccineId_DoseNumber')
    CREATE UNIQUE INDEX UX_VaccineSchedules_VaccineId_DoseNumber ON health.VaccineSchedules(VaccineId, DoseNumber);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'health.PetVaccinations') AND name = N'IX_PetVaccinations_PetId_AppliedAtUtc')
    CREATE INDEX IX_PetVaccinations_PetId_AppliedAtUtc ON health.PetVaccinations(PetId, AppliedAtUtc);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'health.PetVaccinations') AND name = N'IX_PetVaccinations_PetId_VaccineId')
    CREATE INDEX IX_PetVaccinations_PetId_VaccineId ON health.PetVaccinations(PetId, VaccineId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'health.PetVaccinations') AND name = N'IX_PetVaccinations_NextDueAtUtc')
    CREATE INDEX IX_PetVaccinations_NextDueAtUtc ON health.PetVaccinations(NextDueAtUtc);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'health.PetVaccinations') AND name = N'IX_PetVaccinations_IsDeleted')
    CREATE INDEX IX_PetVaccinations_IsDeleted ON health.PetVaccinations(IsDeleted);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'health.PetVaccinations') AND name = N'UX_PetVaccinations_ExactActive')
    CREATE UNIQUE INDEX UX_PetVaccinations_ExactActive
        ON health.PetVaccinations(PetId, VaccineId, AppliedAtUtc, DoseNumber)
        WHERE IsDeleted = 0;

COMMIT TRANSACTION;
