# DogPlatform.Health

## Cross-service boundary

Vaccination history endpoints validate ownership by forwarding the caller's bearer token to
`GET /api/v1/pets/{petId}/health-context`. The authenticated cross-service response provides
`PetId`, `SpeciesId`, `BirthDate`, and `Name`. A successful response proves that the caller owns
the pet; Health never reads `PetsDb` or references Pets Infrastructure.

Health compares the vaccine's numeric `SpeciesId` with the pet context before create/update. For a
pet with `BirthDate`, active vaccines without history become `NotStarted` only after the minimum
age of their first active schedule. Their recommended date is calculated as
`BirthDate + (MinAgeWeeks * 7 days)` and is not persisted as a vaccination record. Pets without a
known birth date remain excluded from age-based `NotStarted` calculation.

Schedules in `SeedVaccines.sql` are development seed data only. They must remain configurable and
be reviewed for country, product/manufacturer, and veterinary criteria before production use.
