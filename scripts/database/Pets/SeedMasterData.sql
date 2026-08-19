USE [DogPlatform_PetsDb];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'[catalog].[Species]', N'U') IS NULL
BEGIN
    THROW 50001, 'Required table [catalog].[Species] does not exist.', 1;
END;

IF OBJECT_ID(N'[catalog].[Breeds]', N'U') IS NULL
BEGIN
    THROW 50002, 'Required table [catalog].[Breeds] does not exist.', 1;
END;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @PerroSpeciesId int;
    DECLARE @GatoSpeciesId int;

    SELECT TOP (1)
        @PerroSpeciesId = [SpeciesId]
    FROM [catalog].[Species] WITH (UPDLOCK, HOLDLOCK)
    WHERE [Name] = N'Perro'
    ORDER BY [SpeciesId];

    IF @PerroSpeciesId IS NULL
    BEGIN
        INSERT INTO [catalog].[Species] ([Name])
        VALUES (N'Perro');

        SET @PerroSpeciesId = CONVERT(int, SCOPE_IDENTITY());
    END;

    SELECT TOP (1)
        @GatoSpeciesId = [SpeciesId]
    FROM [catalog].[Species] WITH (UPDLOCK, HOLDLOCK)
    WHERE [Name] = N'Gato'
    ORDER BY [SpeciesId];

    IF @GatoSpeciesId IS NULL
    BEGIN
        INSERT INTO [catalog].[Species] ([Name])
        VALUES (N'Gato');

        SET @GatoSpeciesId = CONVERT(int, SCOPE_IDENTITY());
    END;

    DECLARE @RequiredBreeds TABLE
    (
        [SpeciesId] int NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        PRIMARY KEY ([SpeciesId], [Name])
    );

    INSERT INTO @RequiredBreeds ([SpeciesId], [Name])
    VALUES
        (@PerroSpeciesId, N'Labrador Retriever'),
        (@PerroSpeciesId, N'Golden Retriever'),
        (@PerroSpeciesId, N'German Shepherd'),
        (@PerroSpeciesId, N'French Bulldog'),
        (@PerroSpeciesId, N'Bulldog'),
        (@PerroSpeciesId, N'Beagle'),
        (@PerroSpeciesId, N'Poodle'),
        (@PerroSpeciesId, N'Rottweiler'),
        (@PerroSpeciesId, N'Yorkshire Terrier'),
        (@PerroSpeciesId, N'Boxer'),
        (@PerroSpeciesId, N'Siberian Husky'),
        (@PerroSpeciesId, N'Chihuahua'),
        (@PerroSpeciesId, N'Shih Tzu'),
        (@PerroSpeciesId, N'Dachshund'),
        (@PerroSpeciesId, N'Border Collie'),
        (@PerroSpeciesId, N'Australian Shepherd'),
        (@PerroSpeciesId, N'Great Dane'),
        (@PerroSpeciesId, N'Doberman'),
        (@PerroSpeciesId, N'Pug'),
        (@PerroSpeciesId, N'Cocker Spaniel'),
        (@PerroSpeciesId, N'Schnauzer'),
        (@PerroSpeciesId, N'Maltese'),
        (@PerroSpeciesId, N'Pomeranian'),
        (@PerroSpeciesId, N'Akita'),
        (@PerroSpeciesId, N'Samoyed'),
        (@PerroSpeciesId, N'Pit Bull Terrier'),
        (@PerroSpeciesId, N'Cane Corso'),
        (@PerroSpeciesId, N'Belgian Malinois'),
        (@PerroSpeciesId, N'Bernese Mountain Dog'),
        (@PerroSpeciesId, N'Mixed Breed'),
        (@GatoSpeciesId, N'Domestic Shorthair'),
        (@GatoSpeciesId, N'Domestic Longhair'),
        (@GatoSpeciesId, N'Persian'),
        (@GatoSpeciesId, N'Siamese'),
        (@GatoSpeciesId, N'Maine Coon'),
        (@GatoSpeciesId, N'Bengal'),
        (@GatoSpeciesId, N'Ragdoll'),
        (@GatoSpeciesId, N'British Shorthair'),
        (@GatoSpeciesId, N'Sphynx'),
        (@GatoSpeciesId, N'Scottish Fold'),
        (@GatoSpeciesId, N'Russian Blue'),
        (@GatoSpeciesId, N'Abyssinian'),
        (@GatoSpeciesId, N'Burmese'),
        (@GatoSpeciesId, N'Norwegian Forest Cat'),
        (@GatoSpeciesId, N'American Shorthair'),
        (@GatoSpeciesId, N'Mixed Breed');

    INSERT INTO [catalog].[Breeds] ([SpeciesId], [Name])
    SELECT source.[SpeciesId], source.[Name]
    FROM @RequiredBreeds AS source
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM [catalog].[Breeds] AS target WITH (UPDLOCK, HOLDLOCK)
        WHERE target.[SpeciesId] = source.[SpeciesId]
          AND target.[Name] = source.[Name]
    );

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
    BEGIN
        ROLLBACK TRANSACTION;
    END;

    THROW;
END CATCH;

SELECT [SpeciesId], [Name]
FROM [catalog].[Species]
ORDER BY [SpeciesId];

SELECT [BreedId], [SpeciesId], [Name], [Size], [CreatedAt]
FROM [catalog].[Breeds]
ORDER BY [SpeciesId], [Name];
