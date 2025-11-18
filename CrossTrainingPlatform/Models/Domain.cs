using CrossTrainingPlatform.Models.UserManagementModels;
using System.ComponentModel.DataAnnotations;

namespace CrossTrainingPlatform.Models
{
    public class Domain
    {
        [Key]
        public int DomainId { get; set; }
        public bool IsActive { get; set; } = true;
        public string DomainName { get; set; }

        public List<AppUser>? UserList { get; set; }

        public Game? Game { get; set; }
        public int? GameId { get; set; }
    }
}
