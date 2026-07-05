using CrossTrainingPlatform.Models.UserManagementModels;
using CrossTrainingPlatform.Models.ViewModels;
using CrossTrainingPlatform.Services.DatabaseServices;
using CrossTrainingPlatform.Services.Security;
using Microsoft.AspNetCore.Mvc;

namespace CrossTrainingPlatform.Controllers
{
    public class UserAuthController : Controller
    {
        private readonly AppUserService _users;
        private readonly AppRoleService _roles;
        private readonly PasswordService _passwords;

        public UserAuthController(AppUserService users, AppRoleService roles, PasswordService passwords)
        {
            _users = users;
            _roles = roles;
            _passwords = passwords;
        }

        // --------------------- REGISTER ---------------------
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
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
                CreationDate = DateTime.Now,
                isActive = true,
                RoleId = 1,
                DomainList = new List<int>()
            };

            // Parola se stocheaza doar ca hash, niciodata in text clar
            newUser.AccPassword = _passwords.Hash(newUser, model.Password);

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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _users.GetByEmailAsync(model.Email);

            // Acelasi mesaj pentru email inexistent sau parola gresita (nu divulgam care e cauza)
            if (user == null || !_passwords.Verify(user, user.AccPassword, model.Password))
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

        // --------------------- LOGOUT ---------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
