using CrossTraining.Application.Abstractions.Persistence;
using CrossTraining.Application.DTOs;
using CrossTraining.Domain.Entities;
using CrossTraining.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CrossTraining.Infrastructure.Repositories;

public class AppRoleRepository : IAppRoleRepository
{
    private readonly AppDbContext _db;

    public AppRoleRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<AppRole>> SearchForAdminAsync(string? searchTerm, int page, int pageSize)
    {
        // IgnoreQueryFilters: ecranul de administrare arata si rolurile dezactivate, ca la
        // drepturi. Inainte le ascundea, iar un rol dezactivat nu mai putea fi gasit.
        var query = LikeFilter.Apply(
            _db.Roles.AsNoTracking().IgnoreQueryFilters(),
            searchTerm,
            r => r.RoleName);

        var total = await query.CountAsync();

        var items = await query
            .OrderBy(r => r.RoleName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<AppRole>(items, page, pageSize, total);
    }

    public Task<AppRole?> GetByIdAsync(int id)
        => _db.Roles.FirstOrDefaultAsync(r => r.RoleId == id);

    public Task<AppRole?> GetForAdminAsync(int id)
        => _db.Roles.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.RoleId == id);

    // Comparatia se face in SQL, dupa colatia bazei, ca indexul unic UX_AppRole_RoleName.
    public Task<AppRole?> GetByNameForAdminAsync(string roleName)
        => _db.Roles
              .AsNoTracking()
              .IgnoreQueryFilters()
              .FirstOrDefaultAsync(r => r.RoleName == roleName);

    public void Add(AppRole role) => _db.Roles.Add(role);

    // Pornim de la Claims, nu de la tabelul pivot: asa filtrul de soft-delete se aplica
    // direct pe entitatea proiectata si nu poate rezulta un null in lista.
    public async Task<List<AppClaim>> GetClaimsForRoleAsync(int roleId)
        => await _db.Claims.AsNoTracking()
                    .Where(c => c.RoleClaims.Any(rc => rc.RoleId == roleId))
                    .OrderBy(c => c.ClaimName)
                    .ToListAsync();

    // IgnoreQueryFilters: administrarea legaturilor trebuie sa functioneze si cand rolul
    // sau dreptul este dezactivat, altfel o legatura veche nu ar mai putea fi stearsa.
    public Task<RoleClaim?> GetRoleClaimAsync(int roleId, int claimId)
        => _db.RoleClaims.IgnoreQueryFilters()
                         .FirstOrDefaultAsync(rc => rc.RoleId == roleId && rc.ClaimId == claimId);

    public void AddClaim(RoleClaim link) => _db.RoleClaims.Add(link);

    public void RemoveClaim(RoleClaim link) => _db.RoleClaims.Remove(link);
}
