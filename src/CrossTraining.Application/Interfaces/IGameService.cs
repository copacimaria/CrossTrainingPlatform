using CrossTraining.Application.ViewModels;
using CrossTraining.Domain.Entities;

namespace CrossTraining.Application.Interfaces;

public interface IGameService
{
    /// <summary>Construieste modelul pentru pagina de jocuri pe baza domeniilor utilizatorului.</summary>
    Task<GamesViewModel> GetGamesForUserAsync(int userId);

    Task<Game?> GetByIdAsync(int id);
    Task<int> CreateAsync(Game game);
    Task UpdateAsync(int id, Game game);
    Task SoftDeleteAsync(int id);
}
