using CrossTraining.Application.Abstractions.Persistence;
using CrossTraining.Application.DTOs;
using CrossTraining.Domain.Entities;
using CrossTraining.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CrossTraining.Infrastructure.Repositories;

public class AppClaimRepository : IAppClaimRepository
{
    private readonly AppDbContext _db;

    public AppClaimRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<AppClaim>> SearchForAdminAsync(string? searchTerm, int page, int pageSize)
    {
        // IgnoreQueryFilters: ecranul de administrare trebuie sa vada si drepturile
        // dezactivate, altfel nu ar mai putea fi reactivate.
        var query = LikeFilter.Apply(
            _db.Claims.AsNoTracking().IgnoreQueryFilters(),
            searchTerm,
            c => c.ClaimName);

        var total = await query.CountAsync();

        var items = await query
            .OrderBy(c => c.ClaimId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<AppClaim>(items, page, pageSize, total);
    }

    public Task<AppClaim?> GetForAdminAsync(int id)
        => _db.Claims
              .IgnoreQueryFilters()
              .Include(c => c.RoleClaims)
                  .ThenInclude(rc => rc.Role)
              .FirstOrDefaultAsync(c => c.ClaimId == id);

    // Egalitatea se traduce in SQL, deci compara dupa colatia bazei - exact ca indexul
    // unic UX_AppClaim_ClaimName. O comparatie facuta in C# ar putea spune "liber" pentru
    // un nume pe care indexul il respinge.
    public Task<AppClaim?> GetByNameForAdminAsync(string claimName)
        => _db.Claims
              .AsNoTracking()
              .IgnoreQueryFilters()
              .FirstOrDefaultAsync(c => c.ClaimName == claimName);

    public async Task<List<AppClaim>> SearchAvailableForRoleAsync(int roleId, string? searchTerm, int limit)
    {
        // Filtrul global lasa deja doar drepturile active.
        var query = LikeFilter.Apply(
            _db.Claims.AsNoTracking().Where(c => !c.RoleClaims.Any(rc => rc.RoleId == roleId)),
            searchTerm,
            c => c.ClaimName);

        return await query.OrderBy(c => c.ClaimName)
                          .Take(limit)
                          .ToListAsync();
    }

    public void Add(AppClaim claim) => _db.Claims.Add(claim);
}
