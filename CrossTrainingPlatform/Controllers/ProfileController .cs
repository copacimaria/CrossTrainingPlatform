using CrossTrainingPlatform.Models.UserManagementModels;
using CrossTrainingPlatform.Models.ViewModels;
using CrossTrainingPlatform.Services.DatabaseServices;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

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
            // ia userId din sesiune (sau de unde il ai tu)
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
                return RedirectToAction("Login", "UserAuth");

            // query Supabase
            string json = await _supabase.GetAsync($"AppUser?UserId=eq.{userId}");

            // parse
            var list = JsonConvert.DeserializeObject<List<AppUser>>(json);

            if (list == null || list.Count == 0)
                return NotFound("User not found");

            var user = list.First();

            // daca vrei role + domain trebuie sa-ti confirm cum sunt legate in Supabase
            string roleName = user.Role?.RoleName ?? "";
            //string domkainId = user.DomainList != null && user.DomainList.Count > 0
            //    ? user.DomainList[0];

            var model = new ProfileViewModel
            {
                FullName = $"{user.Name} {user.FamilyName}",
                Email = user.Email,
                Role = roleName,
                //Domain = domainName
            };

            return View(model);
        }
    }
}
