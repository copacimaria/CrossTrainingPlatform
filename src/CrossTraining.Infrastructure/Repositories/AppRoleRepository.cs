using CrossTraining.Domain.Entities;
using CrossTraining.Domain.Interfaces;
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

    public async Task<List<AppRole>> GetAllAsync()
        => await _db.Roles.AsNoTracking().ToListAsync();

    public Task<AppRole?> GetByIdAsync(int id)
        => _db.Roles.FirstOrDefaultAsync(r => r.RoleId == id);

    public async Task<int> CreateAsync(string roleName)
    {
        var role = new AppRole { RoleName = roleName, IsActive = true };
        _db.Roles.Add(role);
        await _db.SaveChangesAsync();
        return role.RoleId;
    }

    public async Task UpdateNameAsync(int id, string roleName)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.RoleId == id);
        if (role == null)
            return;

        role.RoleName = roleName;
        await _db.SaveChangesAsync();
    }

    public async Task SoftDeleteAsync(int id)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.RoleId == id);
        if (role == null)
            return;

        role.IsActive = false;
        await _db.SaveChangesAsync();
    }

    public async Task<List<AppClaim>> GetClaimsForRoleAsync(int roleId)
        => await _db.RoleClaims.AsNoTracking()
                    .Where(rc => rc.RoleId == roleId)
                    .Select(rc => rc.Claim!)
                    .ToListAsync();

    public async Task<List<int>> GetClaimIdsForRoleAsync(int roleId)
        => await _db.RoleClaims.AsNoTracking()
                    .Where(rc => rc.RoleId == roleId)
                    .Select(rc => rc.ClaimId)
                    .ToListAsync();

    public async Task AddClaimAsync(int roleId, int claimId)
    {
        bool exists = await _db.RoleClaims.AnyAsync(rc => rc.RoleId == roleId && rc.ClaimId == claimId);
        if (exists)
            return;

        _db.RoleClaims.Add(new RoleClaim { RoleId = roleId, ClaimId = claimId });
        await _db.SaveChangesAsync();
    }

    public async Task RemoveClaimAsync(int roleId, int claimId)
    {
        var link = await _db.RoleClaims.FirstOrDefaultAsync(rc => rc.RoleId == roleId && rc.ClaimId == claimId);
        if (link == null)
            return;

        _db.RoleClaims.Remove(link);
        await _db.SaveChangesAsync();
    }
}
