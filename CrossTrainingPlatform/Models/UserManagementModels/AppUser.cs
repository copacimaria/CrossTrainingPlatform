using System.ComponentModel.DataAnnotations;

namespace CrossTrainingPlatform.Models.UserManagementModels
{
    public class AppUser
    {

        [Key]
        public int UserId { get; set; }
        public string Name { get; set; }
        public string FamilyName { get; set; }
        public string Email { get; set; }
        public string AccPassword { get; set; }


        public DateTime? CreationDate { get; set; } = DateTime.Now;
        public bool isActive { get; set; } = true;

        public int? RoleId { get; set; }
        public AppRole? Role { get; set; }


        public List<Game>? GameList { get; set; }
        public List<Domain>? DomainList { get; set; }


    }


}

