using CrossTraining.Application.Abstractions.Persistence;
using CrossTraining.Domain.Entities;
using CrossTraining.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CrossTraining.Infrastructure.Repositories;

public class GameRepository : IGameRepository
{
    private readonly AppDbContext _db;

    public GameRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Game>> GetActiveWithDomainAsync()
        => await _db.Games.AsNoTracking()
                    .Include(g => g.Domain)
                    .OrderBy(g => g.GameName)
                    .ToListAsync();

    public Task<Game?> GetForAdminAsync(int id)
        => _db.Games
              .IgnoreQueryFilters()
              .Include(g => g.Domain)
              .FirstOrDefaultAsync(g => g.GameId == id);

    public void Add(Game game) => _db.Games.Add(game);
}
