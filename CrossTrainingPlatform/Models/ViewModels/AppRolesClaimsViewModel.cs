using CrossTrainingPlatform.Models.DTOs;
using CrossTrainingPlatform.Models.UserManagementModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using X.PagedList;

namespace CrossTrainingPlatform.Models.ViewModels
{
    public class AppRolesClaimsViewModel
    {
        public AppRole Role { get; set; }
        public IPagedList<AppClaim> Claims { get; set; }
        public List<ClaimDto> NormalClaims {  get; set; }

    }
}
