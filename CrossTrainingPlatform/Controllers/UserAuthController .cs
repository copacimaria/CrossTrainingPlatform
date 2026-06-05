using CrossTrainingPlatform.Models.UserManagementModels;
using CrossTrainingPlatform.Models.ViewModels;
using CrossTrainingPlatform.Services.DatabaseServices;
using Microsoft.AspNetCore.Mvc;

namespace CrossTrainingPlatform.Controllers
{
    public class UserAuthController : Controller
    {
        private readonly AppUserService _users;
        private readonly AppRoleService _roles;

        public UserAuthController(AppUserService users, AppRoleService roles)
        {
            _users = users;
            _roles = roles;
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

            // Verifica daca emailul exista deja
            var existing = await _users.GetByEmailAsync(model.Email);
            if (existing != null)
            {
                ModelState.AddModelError("", "Exista deja un cont cu acest email.");
                return View(model);
            }

            var newUser = new AppUser
            {
                Name = model.Name,
                FamilyName = model.FamilyName,
                Email = model.Email,
                AccPassword = model.Password,
                CreationDate = DateTime.Now,
                isActive = true,
                RoleId = 1,
                DomainList = new List<int>()
            };

            int newUserId = await _users.CreateAsync(newUser);

            // Redirect catre SelectDomain cu userId
            return RedirectToAction("SelectDomain", "Domain", new { userId = newUserId });
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

            var user = await _users.GetByCredentialsAsync(model.Email, model.Password);

            if (user == null)
            {
                ModelState.AddModelError("", "Invalid email or password!");
                return View(model);
            }

            // -------------------------------
            // SAVE USER IN SESSION
            // -------------------------------
            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("FullName", $"{user.Name} {user.FamilyName}");
            HttpContext.Session.SetString("Email", user.Email ?? "");

            if (user.RoleId != null)
            {
                var role = await _roles.GetByIdAsync(user.RoleId.Value);
                if (role != null)
                    HttpContext.Session.SetString("RoleName", role.RoleName);
            }

            return RedirectToAction("DisplayGamesBasedOnDomain", "Games");
        }
    }
}
