namespace CrossTraining.Application.DTOs;

public class GameDto
{
    public string GameName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime? CreatedAt { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? DownloadLink { get; set; }
    public int? DomainId { get; set; }
}
