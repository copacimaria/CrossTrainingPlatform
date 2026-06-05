using CrossTrainingPlatform.Models;
using CrossTrainingPlatform.Models.ViewModels;
using CrossTrainingPlatform.Services.DatabaseServices;
using Microsoft.AspNetCore.Mvc;

namespace CrossTrainingPlatform.Controllers
{
    public class ProfileController : Controller
    {
        private readonly AppUserService _users;
        private readonly AppRoleService _roles;
        private readonly DomainService _domains;

        public ProfileController(AppUserService users, AppRoleService roles, DomainService domains)
        {
            _users = users;
            _roles = roles;
            _domains = domains;
        }

        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "UserAuth");

            var user = await _users.GetByIdAsync(userId.Value);
            if (user == null)
                return NotFound("User not found");

            // rol
            string roleName = "";
            if (user.RoleId != null)
            {
                var role = await _roles.GetByIdAsync(user.RoleId.Value);
                roleName = role?.RoleName ?? "";
            }

            // domenii
            var domainIds = user.DomainList ?? new List<int>();
            var domains = await _domains.GetByIdsAsync(domainIds);
            string domainNames = string.Join(", ", domains.Select(d => d.DomainName));

            var model = new ProfileViewModel
            {
                FullName = $"{user.Name} {user.FamilyName}",
                Email = user.Email,
                Role = roleName,
                Domain = domainNames
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "UserAuth");

            var user = await _users.GetByIdAsync(userId.Value);
            if (user == null)
            {
                ModelState.AddModelError("", "User not found");
                return View(model);
            }

            // verificare parola veche
            if (user.AccPassword != model.OldPassword)
            {
                ModelState.AddModelError("OldPassword", "Parola veche este incorecta!");
                return View(model);
            }

            // verificare parola noua = parola veche
            if (model.NewPassword == model.OldPassword)
            {
                ModelState.AddModelError("NewPassword", "Parola noua nu poate fi identica cu cea veche!");
                return View(model);
            }

            await _users.UpdatePasswordAsync(userId.Value, model.NewPassword);

            ViewBag.Success = "Parola a fost schimbata cu succes!";
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ChangeDomain()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "UserAuth");

            var user = await _users.GetByIdAsync(userId.Value);
            if (user == null)
                return NotFound("User not found");

            int currentDomainId = user.DomainList != null && user.DomainList.Count > 0
                ? user.DomainList[0]
                : 0;

            var domains = await _domains.GetActiveAsync();

            var vm = new ChangeDomainVM
            {
                DomainId = currentDomainId,
                Domains = domains
            };

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> ChangeDomain(ChangeDomainVM model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "UserAuth");

            if (model.DomainId == 0)
            {
                ModelState.AddModelError("", "Selecteaza un domeniu.");
                return await ChangeDomain();
            }

            await _users.UpdateDomainListAsync(userId.Value, new List<int> { model.DomainId });

            TempData["Success"] = "Domeniul a fost schimbat.";
            return RedirectToAction("Index");
        }
    }
}
