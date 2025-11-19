using CrossTrainingPlatform.Models.UserManagementModels;
using CrossTrainingPlatform.Models.ViewModels;
using CrossTrainingPlatform.Services.DatabaseServices;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace CrossTrainingPlatform.Controllers
{
    public class UserAuthController : Controller
    {
        private readonly SupabaseService _supabase;

        public UserAuthController(SupabaseService supabase)
        {
            _supabase = supabase;
        }

        // --------------------- REGISTER ---------------------
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var newUser = new
            {
                Name = model.Name,
                FamilyName = model.FamilyName,
                Email = model.Email,
                AccPassword = model.Password,
                CreationDate = DateTime.Now,
                isActive = true,
                RoleId = 1
            };

            var result = await _supabase.PostAsync("AppUser", newUser);

            return RedirectToAction("Login");
        }

        // --------------------- LOGIN ---------------------
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Query Supabase
            string endpoint = $"AppUser?Email=eq.{model.Email}&AccPassword=eq.{model.Password}&select=*";
            string response = await _supabase.GetAsync(endpoint);

            var users = Newtonsoft.Json.JsonConvert.DeserializeObject<List<AppUser>>(response);

            if (users == null || users.Count == 0)
            {
                ModelState.AddModelError("", "Invalid email or password");
                return View(model);
            }

            var user = users.First();

            // -------------------------------
            // SAVE USER IN SESSION
            // ------------------------------
            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("FullName", $"{user.Name} {user.FamilyName}");
            HttpContext.Session.SetString("Email", user.Email ?? "");

            if (user.Role != null)
                HttpContext.Session.SetString("RoleName", user.Role.RoleName);

            // daca userul are domenii
            if (user.DomainList != null && user.DomainList.Count > 0)
                HttpContext.Session.SetString("DomainName", user.DomainList.First().DomainName);

            // redirect
            return RedirectToAction("SelectDomain", "Domain");
        }

    }
}

