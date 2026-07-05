using CrossTraining.Domain.Entities;
using CrossTraining.Domain.Interfaces;
using CrossTraining.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CrossTraining.Infrastructure.Repositories;

public class AppUserRepository : IAppUserRepository
{
    private readonly AppDbContext _db;

    public AppUserRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<AppUser?> GetByIdAsync(int id)
        => _db.Users.FirstOrDefaultAsync(u => u.UserId == id);

    public Task<AppUser?> GetByEmailAsync(string email)
        => _db.Users.FirstOrDefaultAsync(u => u.Email == email);

    public async Task<int> CreateAsync(AppUser user)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user.UserId;
    }

    public async Task UpdatePasswordHashAsync(int userId, string passwordHash)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null)
            return;

        user.PasswordHash = passwordHash;
        await _db.SaveChangesAsync();
    }

    public async Task SetUserDomainsAsync(int userId, IEnumerable<int> domainIds)
    {
        // Stergem legaturile existente si le inlocuim cu cele noi.
        var existing = _db.UserDomains.Where(ud => ud.UserId == userId);
        _db.UserDomains.RemoveRange(existing);

        var distinctIds = domainIds.Distinct().ToList();
        foreach (var domainId in distinctIds)
            _db.UserDomains.Add(new UserDomain { UserId = userId, DomainId = domainId });

        await _db.SaveChangesAsync();
    }

    public async Task<List<int>> GetDomainIdsAsync(int userId)
        => await _db.UserDomains
                    .Where(ud => ud.UserId == userId)
                    .Select(ud => ud.DomainId)
                    .ToListAsync();
}
