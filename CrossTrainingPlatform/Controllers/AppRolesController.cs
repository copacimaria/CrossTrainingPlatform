using CrossTrainingPlatform.Models.DTOs;
using CrossTrainingPlatform.Models.UserManagementModels;
using CrossTrainingPlatform.Models.ViewModels;
using CrossTrainingPlatform.Services.DatabaseServices;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using X.PagedList.Extensions;

namespace KD_Pallet.Controllers
{
    public class AppRolesController : Controller
    {
        private readonly AppRoleService _roles;
        private readonly AppClaimService _claims;

        public AppRolesController(AppRoleService roles, AppClaimService claims)
        {
            _roles = roles;
            _claims = claims;
        }

        // ---------------- INDEX ----------------
        public async Task<IActionResult> Index(int? page, string roleSearch)
        {
            int pageSize = 13;
            int pageNumber = page ?? 1;

            var roles = await _roles.GetAllAsync();
            IEnumerable<AppRole> activeRoles = roles.Where(r => r.IsActive);

            if (!string.IsNullOrEmpty(roleSearch))
            {
                activeRoles = activeRoles
                    .Where(r => r.RoleName.Contains(roleSearch, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var pagedRoles = activeRoles.OrderBy(r => r.RoleName).ToPagedList(pageNumber, pageSize);
            return View(pagedRoles);
        }

        // ---------------- DETAILS ----------------
        public async Task<IActionResult> Details(int id)
        {
            var role = await _roles.GetByIdAsync(id);
            if (role == null)
                return NotFound();

            // claim-urile asociate prin tabela pivot AppClaim_AppRole
            var claims = await _roles.GetClaimsForRoleAsync(id);

            var vm = new AppRolesClaimsViewModel
            {
                Role = role,
                Claims = claims.ToPagedList(1, claims.Count == 0 ? 1 : claims.Count)
            };

            return View(vm);
        }

        // ---------------- CREATE ----------------
        public IActionResult Create()
        {
            return PartialView("_CreateRole");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("RoleName")] AppRole appRole)
        {
            if (string.IsNullOrWhiteSpace(appRole.RoleName))
                return Json(new { success = false, errors = new[] { "Rolul trebuie sa aibe un nume!" } });

            await _roles.CreateAsync(appRole.RoleName);
            return Json(new { success = true });
        }

        // ---------------- EDIT ----------------
        public async Task<IActionResult> Edit(int id)
        {
            var role = await _roles.GetByIdAsync(id);
            if (role == null) return Json(new { success = false, message = "Rolul nu exista." });

            return PartialView("_EditRole", role);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string RoleName)
        {
            if (string.IsNullOrWhiteSpace(RoleName))
                return Json(new { success = false, errors = new[] { "Completati numele rolului!" } });

            await _roles.UpdateNameAsync(id, RoleName);
            return Json(new { success = true });
        }

        // ---------------- DELETE ----------------
        public async Task<IActionResult> Delete(int id)
        {
            var role = await _roles.GetByIdAsync(id);
            if (role == null) return Json(new { success = false, message = "Rolul nu exista." });

            return PartialView("_DeleteRole", role);
        }

        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Soft delete
            await _roles.SoftDeleteAsync(id);
            return Json(new { success = true });
        }

        // ---------------- ADD CLAIM ----------------
        public async Task<IActionResult> AddClaim(int id)
        {
            var role = await _roles.GetByIdAsync(id);
            if (role == null) return Json(new { success = false, message = "Rolul nu a fost gasit." });

            var allClaims = await _claims.GetAllAsync();
            var existingClaimIds = await _roles.GetClaimIdsForRoleAsync(id);

            // Claims care NU sunt deja in rol
            var normalClaims = allClaims
                .Where(c => !existingClaimIds.Contains(c.ClaimId))
                .Select(c => new ClaimDto
                {
                    Token = Convert.ToBase64String(Encoding.UTF8.GetBytes(c.ClaimId.ToString())),
                    ClaimName = c.ClaimName
                })
                .ToList();

            var vm = new AppRolesClaimsViewModel
            {
                Role = role,
                NormalClaims = normalClaims
            };

            return PartialView("_addClaimInRole", vm);
        }

        [HttpPost]
        public IActionResult SearchClaim([FromBody] SearchClaimRequest request)
        {
            if (string.IsNullOrEmpty(request.SearchTerm))
                return Json(new { success = false, message = "Search field is empty." });

            var filtered = request.Claims
                .Where(c => c.ClaimName.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return Json(new { success = true, data = filtered });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddClaim(int roleId, string claimToken)
        {
            string idString;
            try { idString = Encoding.UTF8.GetString(Convert.FromBase64String(claimToken)); }
            catch { return Json(new { success = false, errors = new[] { "Token invalid!" } }); }

            if (!int.TryParse(idString, out var claimId))
                return Json(new { success = false, errors = new[] { "Token invalid!" } });

            await _roles.AddClaimAsync(roleId, claimId);
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveClaim(int RoleID, int ClaimID)
        {
            await _roles.RemoveClaimAsync(RoleID, ClaimID);
            return Json(new { success = true });
        }
    }
}
