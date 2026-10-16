using CrossTraining.Application.Abstractions.Persistence;
using CrossTraining.Domain.Entities;
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

    public Task<bool> EmailExistsAsync(string email)
        => _db.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == email);

    public void Add(AppUser user) => _db.Users.Add(user);

    public async Task<List<int>> GetDomainIdsAsync(int userId)
        => await _db.UserDomains
                    .Where(ud => ud.UserId == userId)
                    .Select(ud => ud.DomainId)
                    .ToListAsync();

    public async Task ReplaceUserDomainsAsync(int userId, IEnumerable<int> domainIds)
    {
        // IgnoreQueryFilters: inlocuim toate legaturile, inclusiv cele catre domenii
        // dezactivate - altfel ar ramane randuri orfane pe care nimic nu le mai sterge.
        var existing = await _db.UserDomains.IgnoreQueryFilters()
                                            .Where(ud => ud.UserId == userId)
                                            .ToListAsync();
        _db.UserDomains.RemoveRange(existing);

        foreach (var domainId in domainIds.Distinct())
            _db.UserDomains.Add(new UserDomain { UserId = userId, DomainId = domainId });
    }
}
