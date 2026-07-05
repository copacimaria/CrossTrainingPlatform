using CrossTraining.Domain.Entities;
using CrossTraining.Domain.Interfaces;
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

    public async Task<List<AppClaim>> GetAllAsync()
        => await _db.Claims.AsNoTracking().ToListAsync();

    public Task<AppClaim?> GetByIdAsync(int id)
        => _db.Claims
              .Include(c => c.RoleClaims)
                  .ThenInclude(rc => rc.Role)
              .FirstOrDefaultAsync(c => c.ClaimId == id);

    public async Task<int> CreateAsync(string claimName)
    {
        var claim = new AppClaim { ClaimName = claimName, IsActive = true };
        _db.Claims.Add(claim);
        await _db.SaveChangesAsync();
        return claim.ClaimId;
    }

    public async Task UpdateNameAsync(int id, string claimName)
    {
        var claim = await _db.Claims.FirstOrDefaultAsync(c => c.ClaimId == id);
        if (claim == null)
            return;

        claim.ClaimName = claimName;
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var claim = await _db.Claims.FirstOrDefaultAsync(c => c.ClaimId == id);
        if (claim == null)
            return;

        // Legaturile din pivot se sterg prin cascade (vezi RoleClaimConfiguration).
        _db.Claims.Remove(claim);
        await _db.SaveChangesAsync();
    }
}
