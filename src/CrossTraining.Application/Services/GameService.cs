using CrossTraining.Application.Interfaces;
using CrossTraining.Application.ViewModels;
using CrossTraining.Domain.Entities;
using CrossTraining.Domain.Interfaces;

namespace CrossTraining.Application.Services;

public class GameService : IGameService
{
    private readonly IGameRepository _games;
    private readonly IDomainRepository _domains;
    private readonly IAppUserRepository _users;

    public GameService(IGameRepository games, IDomainRepository domains, IAppUserRepository users)
    {
        _games = games;
        _domains = domains;
        _users = users;
    }

    public async Task<GamesViewModel> GetGamesForUserAsync(int userId)
    {
        var domainIds = await _users.GetDomainIdsAsync(userId);
        var allDomains = await _domains.GetAllAsync();
        var allGames = await _games.GetAllAsync();

        // Jocurile pentru domeniile utilizatorului (poate avea mai multe domenii).
        var gamesForDomains = allGames
            .Where(g => g.DomainId != null && domainIds.Contains(g.DomainId.Value))
            .ToList();

        AttachDomains(gamesForDomains, allDomains);
        AttachDomains(allGames, allDomains);

        return new GamesViewModel
        {
            AllGames = allGames,
            GamesForSelectedDomains = gamesForDomains
        };
    }

    public Task<Game?> GetByIdAsync(int id) => _games.GetByIdAsync(id);

    public Task<int> CreateAsync(Game game)
    {
        game.CreatedAt = DateTime.Now;
        return _games.CreateAsync(game);
    }

    public Task UpdateAsync(int id, Game game)
    {
        game.GameId = id;
        return _games.UpdateAsync(game);
    }

    public Task SoftDeleteAsync(int id) => _games.SoftDeleteAsync(id);

    private static void AttachDomains(List<Game> games, List<TrainingDomain> allDomains)
    {
        foreach (var g in games)
            g.Domain = allDomains.FirstOrDefault(d => d.DomainId == g.DomainId);
    }
}
