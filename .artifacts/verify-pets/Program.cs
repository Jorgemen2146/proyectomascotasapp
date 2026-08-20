using DogPlatform.Pets.Domain.ValueObjects;
using DogPlatform.Pets.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

var options = new DbContextOptionsBuilder<PetsDbContext>()
    .UseSqlServer("Server=localhost;Database=DogPlatform_PetsDb;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False;")
    .Options;

await using var context = new PetsDbContext(options);
var gender = Gender.Create("M").Value;
var query = context.Pets
    .AsNoTracking()
    .Where(p => p.Gender.Equals(gender))
    .Select(p => new { p.Id, p.Gender });

var sql = query.ToQueryString();
Console.WriteLine(sql.Contains("[p].[Gender]", StringComparison.Ordinal)
    ? "RELATIONAL_TRANSLATION_OK"
    : "RELATIONAL_TRANSLATION_MISSING_GENDER");
