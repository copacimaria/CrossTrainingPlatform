namespace CrossTraining.Domain.Entities;

/// <summary>Joc educational asociat unui domeniu.</summary>
public class Game
{
    public int GameId { get; set; }
    public string GameName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime? CreatedAt { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? DownloadLink { get; set; }

    public int? DomainId { get; set; }
    public TrainingDomain? Domain { get; set; }
}
