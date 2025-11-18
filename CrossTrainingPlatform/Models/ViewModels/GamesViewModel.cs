namespace CrossTrainingPlatform.Models.ViewModels
{
    public class GamesViewModel
    {
        public List<Game> GamesForSelectedDomain { get; set; } = new List<Game>();
        public List<Game> AllGames { get; set; } = new List<Game>();
    }
}
