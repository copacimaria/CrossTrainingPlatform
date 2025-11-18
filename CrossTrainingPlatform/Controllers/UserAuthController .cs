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

            string endpoint = $"AppUser?Email=eq.{model.Email}&AccPassword=eq.{model.Password}&select=*";
            string response = await _supabase.GetAsync(endpoint);

            var users = JsonSerializer.Deserialize<List<AppUser>>(response);

            if (users == null || users.Count == 0)
            {
                ModelState.AddModelError("", "Invalid email or password");
                return View(model);
            }

            return RedirectToAction("SelectDomain", "Domain");
        }
    }
}

