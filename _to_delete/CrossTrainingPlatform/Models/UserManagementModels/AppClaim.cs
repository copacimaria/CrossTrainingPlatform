using System.ComponentModel.DataAnnotations;

namespace CrossTrainingPlatform.Models.UserManagementModels
{
    public class AppClaim
    {
        [Key]
        public int ClaimId { get; set; }
        public string ClaimName { get; set; }
        public bool IsActive { get; set; } = true;
        

        public List<AppRole>? RoleList { get; set; }
    }
}
