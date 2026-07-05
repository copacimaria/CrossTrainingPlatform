using CrossTraining.Domain.Entities;

namespace CrossTraining.Application.ViewModels;

public class GamesViewModel
{
    public List<Game> GamesForSelectedDomains { get; set; } = new();
    public List<Game> AllGames { get; set; } = new();
}
