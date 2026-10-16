using CrossTraining.Application.Abstractions.Persistence;
using CrossTraining.Domain.Entities;
using CrossTraining.Domain.ValueObjects;
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

    // IgnoreQueryFilters: legaturile catre domenii dezactivate trebuie incarcate si ele,
    // altfel ar ramane randuri orfane pe care nimic nu le mai sterge. Filtrul de activ pe
    // utilizator se pune explicit, ca sa ramana acelasi comportament ca la GetByIdAsync.
    public Task<AppUser?> GetWithDomainLinksAsync(int id)
        => _db.Users
              .IgnoreQueryFilters()
              .Include(u => u.UserDomains)
              .FirstOrDefaultAsync(u => u.UserId == id && u.IsActive);

    // Comparatia se traduce in SQL pe coloana convertita, deci foloseste colatia bazei -
    // aceeasi ca indexul unic UX_AppUser_Email.
    public Task<AppUser?> GetByEmailAsync(Email email)
        => _db.Users.FirstOrDefaultAsync(u => u.Email == email);

    public Task<bool> EmailExistsAsync(Email email)
        => _db.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == email);

    public void Add(AppUser user) => _db.Users.Add(user);

    public async Task<List<int>> GetDomainIdsAsync(int userId)
        => await _db.UserDomains
                    .Where(ud => ud.UserId == userId)
                    .Select(ud => ud.DomainId)
                    .ToListAsync();
}
