using DogPlatform.Pets.Application.Common;
using DogPlatform.Pets.Application.Features.Pets.GetMine;
using DogPlatform.Pets.Application.Queries;
using DogPlatform.Pets.Infrastructure.Persistence.Context;
using DogPlatform.Pets.Application.Storage;
using DogPlatform.Pets.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace DogPlatform.Pets.Infrastructure.Persistence.Queries;

/// <summary>
/// Read-only EF Core implementation for pet list queries.
/// Uses projection to avoid loading full aggregates and left-joins the main photo
/// in a single SQL query to prevent N+1.
/// </summary>
public sealed class PetQueryService : IPetQueryService
{
    private readonly PetsDbContext _context;
    private readonly IPhotoStorageService _photoStorage;

    public PetQueryService(PetsDbContext context, IPhotoStorageService photoStorage)
    {
        _context = context;
        _photoStorage = photoStorage;
    }

    public async Task<PagedResult<MyPetResponse>> GetMyPetsAsync(
        Guid ownerId,
        GetMyPetsQuery query,
        CancellationToken cancellationToken = default)
    {
        // Base query — AsNoTracking, filtered by owner, excludes soft-deleted.
        // The global query filter on Pet already excludes IsDeleted=true,
        // but we filter explicitly for clarity and safety.
        var baseQuery = _context.Pets
            .AsNoTracking()
            .Where(p => p.OwnerId == ownerId && !p.IsDeleted);

        // ── Optional filters ────────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            var name = query.Name.Trim();
            baseQuery = baseQuery.Where(p => p.Name.Contains(name));
        }

        if (query.SpeciesId.HasValue)
        {
            var speciesId = query.SpeciesId.Value;
            baseQuery = baseQuery.Where(p =>
                _context.Breeds
                    .Any(b => b.Id == p.BreedId && b.SpeciesId == speciesId));
        }

        if (query.BreedId.HasValue)
            baseQuery = baseQuery.Where(p => p.BreedId == query.BreedId.Value);

        if (!string.IsNullOrWhiteSpace(query.Sex))
        {
            var gender = Gender.Create(query.Sex).Value;
            baseQuery = baseQuery.Where(p => p.Gender.Equals(gender));
        }

        // ── Count before pagination ─────────────────────────────────────────
        var totalItems = await baseQuery.CountAsync(cancellationToken);

        if (totalItems == 0)
            return PagedResult<MyPetResponse>.Create(
                Array.Empty<MyPetResponse>(), 0, query.PageNumber, query.PageSize);

        // ── Ordering (explicit switch — no dynamic reflection) ──────────────
        var ordered = query.SortBy.ToUpperInvariant() switch
        {
            "NAME"      => query.SortDirection.ToUpperInvariant() == "ASC"
                               ? baseQuery.OrderBy(p => p.Name)
                               : baseQuery.OrderByDescending(p => p.Name),
            "BIRTHDATE" => query.SortDirection.ToUpperInvariant() == "ASC"
                               ? baseQuery.OrderBy(p => p.BirthDate)
                               : baseQuery.OrderByDescending(p => p.BirthDate),
            "UPDATEDAT" => query.SortDirection.ToUpperInvariant() == "ASC"
                               ? baseQuery.OrderBy(p => p.UpdatedAt)
                               : baseQuery.OrderByDescending(p => p.UpdatedAt),
            _           => query.SortDirection.ToUpperInvariant() == "ASC"   // default: CreatedAt
                               ? baseQuery.OrderBy(p => p.CreatedAt)
                               : baseQuery.OrderByDescending(p => p.CreatedAt)
        };

        // ── Pagination ──────────────────────────────────────────────────────
        var skip = (query.PageNumber - 1) * query.PageSize;

        // ── Single SQL query: left-join main photo + breed + species ─────────
        // This avoids N+1. The main photo URL (stored as object key) is projected
        // directly — no per-pet secondary queries.
        var items = await ordered
            .Skip(skip)
            .Take(query.PageSize)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.BreedId,
                p.BirthDate,
                p.CreatedAt,
                p.UpdatedAt,
                p.Gender,
                BreedName    = _context.Breeds
                                   .Where(b => b.Id == p.BreedId)
                                   .Select(b => b.Name)
                                   .FirstOrDefault() ?? string.Empty,
                SpeciesId    = _context.Breeds
                                   .Where(b => b.Id == p.BreedId)
                                   .Select(b => b.SpeciesId)
                                   .FirstOrDefault(),
                SpeciesName  = _context.Breeds
                                   .Where(b => b.Id == p.BreedId)
                                   .SelectMany(b => _context.Species
                                       .Where(s => s.Id == b.SpeciesId)
                                       .Select(s => s.Name))
                                   .FirstOrDefault() ?? string.Empty,
                // Left-join to PetPhotos for the single main photo URL (object key)
                MainPhotoUrl = _context.PetPhotos
                                   .Where(pp => pp.PetId == p.Id && pp.IsMain)
                                   .Select(pp => pp.Url)
                                   .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var responses = items
            .Select(i => new MyPetResponse(
                i.Id,
                i.Name,
                i.SpeciesId,
                i.SpeciesName,
                i.BreedId,
                i.BreedName,
                i.Gender.Value,
                i.BirthDate,
                i.MainPhotoUrl is null ? null : _photoStorage.ResolvePublicUrl(i.MainPhotoUrl),
                i.CreatedAt,
                i.UpdatedAt))
            .ToList()
            .AsReadOnly();

        return PagedResult<MyPetResponse>.Create(responses, totalItems, query.PageNumber, query.PageSize);
    }
}
