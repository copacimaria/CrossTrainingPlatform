using CrossTrainingPlatform.Models.UserManagementModels;
using System.ComponentModel.DataAnnotations;

namespace CrossTrainingPlatform.Models
{
    public class Game
    {
        [Key]
        public int GameId { get; set; }
        public string GameName { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;


        [MaxLength(300, ErrorMessage = "Descrierea nu poate depasi 300 de caractere.")]
        public string Description { get; set; }

        public string? DownloadLink { get; set; }

        public List<AppUser>? UserList { get; set; }

        public Domain? Domain { get; set; }
        public int? DomainId { get; set; }
    }
}
