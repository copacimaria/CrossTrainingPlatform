using CrossTrainingPlatform.Models;
using CrossTrainingPlatform.Models.UserManagementModels;
using CrossTrainingPlatform.Models.ViewModels;
using CrossTrainingPlatform.Services.DatabaseServices;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Data;

namespace CrossTrainingPlatform.Controllers
{
    public class ProfileController : Controller
    {
        private readonly SupabaseService _supabase;

        public ProfileController(SupabaseService supabase)
        {
            _supabase = supabase;
        }

        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "UserAuth");

            // get user
            string json = await _supabase.GetAsync($"AppUser?UserId=eq.{userId}&select=*");
            var list = JsonConvert.DeserializeObject<List<AppUser>>(json);

            if (list == null || list.Count == 0)
                return NotFound("User not found");

            var user = list.First();

            // get role
            string roleJson = await _supabase.GetAsync($"AppRole?RoleId=eq.{user.RoleId}");
            var roleList = JsonConvert.DeserializeObject<List<AppRole>>(roleJson);
            string roleName = roleList.FirstOrDefault()?.RoleName ?? "";

            // get domains
            var domainIds = user.DomainList ?? new List<int>();
            List<Domain> domains = new List<Domain>();

            if (domainIds.Count > 0)
            {
                string idList = string.Join(",", domainIds);
                string domainJson = await _supabase.GetAsync($"Domain?DomainId=in.({idList})");
                domains = JsonConvert.DeserializeObject<List<Domain>>(domainJson);
            }

            string domainNames = string.Join(", ", domains.Select(d => d.DomainName));

            // view model
            var model = new ProfileViewModel
            {
                FullName = $"{user.Name} {user.FamilyName}",
                Email = user.Email,
                Role = roleName,
                Domain = domainNames
            };

            return View(model);
        }

    }
}
