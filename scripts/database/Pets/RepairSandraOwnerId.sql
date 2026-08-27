/*
  Repairs the owner of Sandra after the JWT claim-mapping defect created the pet
  with Guid.Empty. This script is intentionally specific and idempotent.
*/
USE [DogPlatform_PetsDb];
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @PetId uniqueidentifier = '0929638D-12B6-47DD-9A6C-8A4479E4C823';
DECLARE @ExpectedOwnerId uniqueidentifier = 'C7963769-B8D9-4881-946B-81176E23248D';
DECLARE @EmptyOwnerId uniqueidentifier = '00000000-0000-0000-0000-000000000000';

IF NOT EXISTS
(
    SELECT 1
    FROM [DogPlatform_IdentityDb].[auth].[Users]
    WHERE UserId = @ExpectedOwnerId
      AND Email = N'gonzales823@hotmail.com'
      AND IsActive = 1
)
    THROW 51000, 'Expected active owner account was not found.', 1;

IF NOT EXISTS
(
    SELECT 1
    FROM pets.Pets
    WHERE PetId = @PetId
      AND Name = N'sandra'
)
    THROW 51001, 'Expected Sandra pet record was not found.', 1;

IF EXISTS
(
    SELECT 1
    FROM pets.Pets
    WHERE PetId = @PetId
      AND OwnerId NOT IN (@EmptyOwnerId, @ExpectedOwnerId)
)
    THROW 51002, 'Sandra already has a different non-empty owner.', 1;

UPDATE pets.Pets
SET OwnerId = @ExpectedOwnerId,
    UpdatedAt = SYSUTCDATETIME()
WHERE PetId = @PetId
  AND Name = N'sandra'
  AND OwnerId = @EmptyOwnerId;

COMMIT TRANSACTION;
GO

SELECT PetId, Name, OwnerId, CreatedAt, UpdatedAt
FROM pets.Pets
WHERE PetId = '0929638D-12B6-47DD-9A6C-8A4479E4C823';
GO
