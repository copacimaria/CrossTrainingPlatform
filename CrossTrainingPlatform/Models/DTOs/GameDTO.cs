using CrossTrainingPlatform.Models.UserManagementModels;
using System.ComponentModel.DataAnnotations;

namespace CrossTrainingPlatform.Models.DTOs
{
    public class GameDTO
    {
        public string GameName { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? CreatedAt { get; set; }
        public string Description { get; set; }

        public string? DownloadLink { get; set; }
        public int? DomainId { get; set; }
    }
}
