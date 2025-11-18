using System.ComponentModel.DataAnnotations;

namespace CrossTrainingPlatform.Models.UserManagementModels
{
    public class AppRole
    {
        [Key]
        public int RoleId { get; set; }
        public string RoleName { get; set; }

        public bool IsActive { get; set; } = true;

        
        public List<AppClaim>? ClaimList { get; set; }
    }
}
