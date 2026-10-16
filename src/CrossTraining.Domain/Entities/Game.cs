namespace CrossTraining.Domain.Entities;

/// <summary>Joc educational asociat unui domeniu.</summary>
public class Game : ISoftDeletable
{
    public int GameId { get; set; }
    public string GameName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime? CreatedAt { get; set; }
    /// <summary>Coloana permite NULL in baza de date, deci tipul o reflecta.</summary>
    public string? Description { get; set; }
    public string? DownloadLink { get; set; }

    public int? DomainId { get; set; }
    public TrainingDomain? Domain { get; set; }
}
