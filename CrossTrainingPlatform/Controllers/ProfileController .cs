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

            // luam userul
            string userJson = await _supabase.GetAsync($"AppUser?UserId=eq.{userId}");
            var list = JsonConvert.DeserializeObject<List<AppUser>>(userJson);
            var user = list.FirstOrDefault();

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

            // update parola in Supabase
            var updateData = new
            {
                AccPassword = model.NewPassword
            };

            await _supabase.PatchAsync($"AppUser?UserId=eq.{userId}", updateData);

            ViewBag.Success = "Parola a fost schimbata cu succes!";

            return View();
        }


        [HttpGet]
        public async Task<IActionResult> ChangeDomain()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "UserAuth");

            string userJson = await _supabase.GetAsync($"AppUser?UserId=eq.{userId}");
            var list = JsonConvert.DeserializeObject<List<AppUser>>(userJson);
            var user = list.FirstOrDefault();

            if (user == null)
                return NotFound("User not found");

            int currentDomainId = user.DomainList != null && user.DomainList.Count > 0
                ? user.DomainList[0]
                : 0;

            string domainJson = await _supabase.GetAsync("Domain");
            var domains = JsonConvert.DeserializeObject<List<Domain>>(domainJson);

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

            var updateData = new
            {
                DomainList = new List<int> { model.DomainId }
            };

            await _supabase.PatchAsync($"AppUser?UserId=eq.{userId}", updateData);

            TempData["Success"] = "Domeniul a fost schimbat.";
            return RedirectToAction("Index");
        }



    }
}
