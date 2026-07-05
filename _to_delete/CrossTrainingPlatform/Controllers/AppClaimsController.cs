using CrossTrainingPlatform.Models.UserManagementModels;
using CrossTrainingPlatform.Services.DatabaseServices;
using Microsoft.AspNetCore.Mvc;
using X.PagedList.Extensions;

namespace KD_Pallet.Controllers
{
    public class AppClaimsController : Controller
    {
        private readonly AppClaimService _claims;

        public AppClaimsController(AppClaimService claims)
        {
            _claims = claims;
        }

        // ---------------- INDEX ----------------
        public async Task<IActionResult> Index(int? page, string claimSearch)
        {
            int pageSize = 13;
            int pageNumber = page ?? 1;

            var appClaims = await _claims.GetAllAsync();

            if (!string.IsNullOrEmpty(claimSearch))
            {
                appClaims = appClaims
                    .Where(c => c.ClaimName.Contains(claimSearch, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var pagedAppClaims = appClaims.OrderBy(c => c.ClaimId).ToPagedList(pageNumber, pageSize);
            return View(pagedAppClaims);
        }

        // ---------------- DETAILS ----------------
        public async Task<IActionResult> Details(int id)
        {
            var appClaim = await _claims.GetByIdAsync(id);
            if (appClaim == null) return NotFound();

            return PartialView("_DetailsRolesofClaim", appClaim);
        }

        // ---------------- CREATE ----------------
        public IActionResult Create()
        {
            return PartialView("_CreateClaim");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ClaimName")] AppClaim appClaim)
        {
            if (string.IsNullOrWhiteSpace(appClaim.ClaimName))
                return Json(new { success = false, errors = new[] { "Claim-ul trebuie sa aiba un nume!" } });

            await _claims.CreateAsync(appClaim.ClaimName);
            return Json(new { success = true });
        }

        // ---------------- EDIT ----------------
        public async Task<IActionResult> Edit(int id)
        {
            var appClaim = await _claims.GetByIdAsync(id);
            if (appClaim == null) return Json(new { success = false, message = "Claim-ul nu exista." });

            return PartialView("_EditClaim", appClaim);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ClaimName")] AppClaim appClaim)
        {
            await _claims.UpdateNameAsync(id, appClaim.ClaimName);
            return Json(new { success = true });
        }

        // ---------------- DELETE ----------------
        public async Task<IActionResult> Delete(int id)
        {
            var appClaim = await _claims.GetByIdAsync(id);
            if (appClaim == null) return Json(new { success = false, message = "Claim-ul nu exista." });

            return PartialView("_DeleteClaim", appClaim);
        }

        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _claims.DeleteAsync(id);
            return Json(new { success = true });
        }
    }
}
