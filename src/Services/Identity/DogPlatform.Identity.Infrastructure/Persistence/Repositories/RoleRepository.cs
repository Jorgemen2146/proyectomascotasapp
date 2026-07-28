using DogPlatform.Identity.Domain.Aggregates.Role;
using DogPlatform.Identity.Domain.Repositories;
using DogPlatform.Identity.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace DogPlatform.Identity.Infrastructure.Persistence.Repositories;

internal sealed class RoleRepository : IRoleRepository
{
    private readonly IdentityDbContext _context;

    public RoleRepository(IdentityDbContext context) => _context = context;

    public async Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Roles.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default) =>
        await _context.Roles
            .FirstOrDefaultAsync(r => r.Name == name.Trim(), cancellationToken);

    public async Task<IReadOnlyCollection<Role>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.Roles.ToListAsync(cancellationToken);

    public async Task AddAsync(Role role, CancellationToken cancellationToken = default) =>
        await _context.Roles.AddAsync(role, cancellationToken);

    public async Task<bool> ExistsWithNameAsync(string name, CancellationToken cancellationToken = default) =>
        await _context.Roles.AnyAsync(r => r.Name == name.Trim(), cancellationToken);
}
