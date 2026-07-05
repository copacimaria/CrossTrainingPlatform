using CrossTraining.Domain.Entities;
using CrossTraining.Domain.Interfaces;
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

    public async Task<List<Game>> GetAllAsync()
        => await _db.Games.AsNoTracking().ToListAsync();

    public async Task<List<Game>> GetByDomainAsync(int domainId)
        => await _db.Games.AsNoTracking().Where(g => g.DomainId == domainId).ToListAsync();

    public Task<Game?> GetByIdAsync(int id)
        => _db.Games.FirstOrDefaultAsync(g => g.GameId == id);

    public async Task<int> CreateAsync(Game game)
    {
        _db.Games.Add(game);
        await _db.SaveChangesAsync();
        return game.GameId;
    }

    public async Task UpdateAsync(Game game)
    {
        var existing = await _db.Games.FirstOrDefaultAsync(g => g.GameId == game.GameId);
        if (existing == null)
            return;

        existing.GameName = game.GameName;
        existing.Description = game.Description;
        existing.DownloadLink = game.DownloadLink;
        existing.DomainId = game.DomainId;
        await _db.SaveChangesAsync();
    }

    public async Task SoftDeleteAsync(int id)
    {
        var existing = await _db.Games.FirstOrDefaultAsync(g => g.GameId == id);
        if (existing == null)
            return;

        existing.IsActive = false;
        await _db.SaveChangesAsync();
    }
}
