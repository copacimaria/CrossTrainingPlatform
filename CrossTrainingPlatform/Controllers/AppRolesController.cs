using CrossTrainingPlatform.Models.DTOs;
using CrossTrainingPlatform.Models.UserManagementModels;
using CrossTrainingPlatform.Models.ViewModels;
using CrossTrainingPlatform.Services.DatabaseServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using X.PagedList.Extensions;

namespace KD_Pallet.Controllers
{
    [Authorize(Roles = "ViewRoles")]
    public class AppRolesController : Controller
    {
        private readonly SupabaseService _supabase;

        public AppRolesController(SupabaseService supabase)
        {
            _supabase = supabase;
        }

        // ---------------- INDEX ----------------
        [Authorize(Roles = "ViewRoles")]
        public async Task<IActionResult> Index(int? page, string roleSearch)
        {
            int pageSize = 13;
            int pageNumber = page ?? 1;

            string response = await _supabase.GetAsync("AppRole?select=*");
            var roles = JsonSerializer.Deserialize<List<AppRole>>(response) ?? new List<AppRole>();

            if (!string.IsNullOrEmpty(roleSearch))
            {
                roles = roles
                    .Where(r => r.RoleName.Contains(roleSearch, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var pagedRoles = roles.OrderBy(r => r.RoleName).ToPagedList(pageNumber, pageSize);
            return View(pagedRoles);
        }

        // ---------------- DETAILS ----------------
        [Authorize(Roles = "ViewClaim")]
        public async Task<IActionResult> Details(int id)
        {
            string response = await _supabase.GetAsync($"AppRole?Id=eq.{id}");
            var role = JsonSerializer.Deserialize<List<AppRole>>(response)?.FirstOrDefault();
            if (role == null) return NotFound();

            // Preluare claim-uri asociate
            string claimsResponse = await _supabase.GetAsync($"AppClaim?select=*,RoleList(Id)&RoleList.Id=eq.{id}");
            var claims = JsonSerializer.Deserialize<List<AppClaim>>(claimsResponse) ?? new List<AppClaim>();

            AppRolesClaimsViewModel vm = new AppRolesClaimsViewModel
            {
                Role = role,
                Claims = claims.ToPagedList(1, claims.Count) // simplu, fara paginare complexa
            };

            return View(vm);
        }

        // ---------------- CREATE ----------------
        [Authorize(Roles = "CreateRoles")]
        public IActionResult Create()
        {
            return PartialView("_CreateRole");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "CreateRoles")]
        public async Task<IActionResult> Create([Bind("RoleName")] AppRole appRole)
        {
            if (string.IsNullOrWhiteSpace(appRole.RoleName))
                return Json(new { success = false, errors = new[] { "Rolul trebuie sa aibe un nume!" } });

            await _supabase.PostAsync("AppRole", new { RoleName = appRole.RoleName, IsActive = true });
            return Json(new { success = true });
        }

        // ---------------- EDIT ----------------
        [Authorize(Roles = "EditRoles")]
        public async Task<IActionResult> Edit(int id)
        {
            string response = await _supabase.GetAsync($"AppRole?Id=eq.{id}");
            var role = JsonSerializer.Deserialize<List<AppRole>>(response)?.FirstOrDefault();
            if (role == null) return Json(new { success = false, message = "Rolul nu exista." });

            return PartialView("_EditRole", role);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "EditRoles")]
        public async Task<IActionResult> Edit(int id, string RoleName)
        {
            if (string.IsNullOrWhiteSpace(RoleName))
                return Json(new { success = false, errors = new[] { "Completati numele rolului!" } });

            await _supabase.PatchAsync($"AppRole?Id=eq.{id}", new { RoleName });
            return Json(new { success = true });
        }

        // ---------------- DELETE ----------------
        [Authorize(Roles = "DeleteRoles")]
        public async Task<IActionResult> Delete(int id)
        {
            string response = await _supabase.GetAsync($"AppRole?Id=eq.{id}");
            var role = JsonSerializer.Deserialize<List<AppRole>>(response)?.FirstOrDefault();
            if (role == null) return Json(new { success = false, message = "Rolul nu exista." });

            return PartialView("_DeleteRole", role);
        }

        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "DeleteRoles")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Soft delete
            await _supabase.PatchAsync($"AppRole?Id=eq.{id}", new { IsActive = false });
            return Json(new { success = true });
        }

        // ---------------- ADD CLAIM ----------------
        [Authorize(Roles = "AddClaim")]
        public async Task<IActionResult> AddClaim(int id)
        {
            string roleResponse = await _supabase.GetAsync($"AppRole?Id=eq.{id}");
            var role = JsonSerializer.Deserialize<List<AppRole>>(roleResponse)?.FirstOrDefault();
            if (role == null) return Json(new { success = false, message = "Rolul nu a fost gasit." });

            string allClaimsResponse = await _supabase.GetAsync("AppClaim?select=*");
            var allClaims = JsonSerializer.Deserialize<List<AppClaim>>(allClaimsResponse) ?? new List<AppClaim>();

            // Claims care nu sunt in rol
            var normalClaims = allClaims
                .Where(c => c.RoleList == null || !c.RoleList.Any(r => r.RoleId == id))
                .Select(c => new ClaimDto
                {
                    Token = Convert.ToBase64String(Encoding.UTF8.GetBytes(c.ClaimId.ToString())),
                    ClaimName = c.ClaimName
                })
                .ToList();

            AppRolesClaimsViewModel vm = new AppRolesClaimsViewModel
            {
                Role = role,
                NormalClaims = normalClaims
            };

            return PartialView("_addClaimInRole", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "AddClaim")]
        public async Task<IActionResult> AddClaim(int roleId, string claimToken)
        {
            string idString;
            try { idString = Encoding.UTF8.GetString(Convert.FromBase64String(claimToken)); }
            catch { return Json(new { success = false, errors = new[] { "Token invalid!" } }); }

            if (!int.TryParse(idString, out var claimId))
                return Json(new { success = false, errors = new[] { "Token invalid!" } });

            // Adaugare claim la rol - aici trebuie sa ai tabel many-to-many in Supabase
            await _supabase.PostAsync("AppRolesClaims", new { RoleId = roleId, ClaimId = claimId });

            return Json(new { success = true });
        }

        [HttpPost]
        [Authorize(Roles = "RemoveClaim")]
        public async Task<IActionResult> RemoveClaim(int RoleID, int ClaimID)
        {
            // stergere in tabel many-to-many AppRolesClaims
            await _supabase.DeleteAsync($"AppRolesClaims?RoleId=eq.{RoleID}&ClaimId=eq.{ClaimID}");
            return Json(new { success = true });
        }

        private bool AppRoleExists(int id)
        {
            // optional, poate fi ignorat sau facut un GET simplu
            return true;
        }
    }
}
