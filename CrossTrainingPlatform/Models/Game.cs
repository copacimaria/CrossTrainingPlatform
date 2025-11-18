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
       

        public List<AppUser>? UserList { get; set; }


        public Domain? Domain { get; set; }
        public int? DomainId { get; set; }
    }
}
