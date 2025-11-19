using CrossTrainingPlatform.Models.UserManagementModels;
using CrossTrainingPlatform.Services.DatabaseServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using X.PagedList.Extensions;

namespace KD_Pallet.Controllers
{
    //[Authorize(Roles = "ViewClaim")]
    public class AppClaimsController : Controller
    {
        private readonly SupabaseService _supabase;

        public AppClaimsController(SupabaseService supabase)
        {
            _supabase = supabase;
        }

        // ---------------- INDEX ----------------
       // [Authorize(Roles = "ViewClaim")]
        public async Task<IActionResult> Index(int? page, string claimSearch)
        {
            int pageSize = 13;
            int pageNumber = page ?? 1;

            string response = await _supabase.GetAsync("AppClaim?select=*");
            var appClaims = JsonSerializer.Deserialize<List<AppClaim>>(response) ?? new List<AppClaim>();

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
       // [Authorize(Roles = "ViewClaim")]
        public async Task<IActionResult> Details(int id)
        {
            string response = await _supabase.GetAsync($"AppClaim?ClaimId=eq.{id}");
            var appClaim = JsonSerializer.Deserialize<List<AppClaim>>(response)?.FirstOrDefault();
            if (appClaim == null) return NotFound();

            return PartialView("_DetailsRolesofClaim", appClaim);
        }

        // ---------------- CREATE ----------------
       // [Authorize(Roles = "CreateClaim")]
        public IActionResult Create()
        {
            return PartialView("_CreateClaim");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
       // [Authorize(Roles = "CreateClaim")]
        public async Task<IActionResult> Create([Bind("ClaimName")] AppClaim appClaim)
        {
            if (string.IsNullOrWhiteSpace(appClaim.ClaimName))
                return Json(new { success = false, errors = new[] { "Claim-ul trebuie sa aiba un nume!" } });

            await _supabase.PostAsync("AppClaim", new { ClaimName = appClaim.ClaimName });
            return Json(new { success = true });
        }

        // ---------------- EDIT ----------------
       // [Authorize(Roles = "EditClaim")]
        public async Task<IActionResult> Edit(int id)
        {
            string response = await _supabase.GetAsync($"AppClaim?ClaimId=eq.{id}");
            var appClaim = JsonSerializer.Deserialize<List<AppClaim>>(response)?.FirstOrDefault();
            if (appClaim == null) return Json(new { success = false, message = "Claim-ul nu exista." });

            return PartialView("_EditClaim", appClaim);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        //[Authorize(Roles = "EditClaim")]
        public async Task<IActionResult> Edit(int id, [Bind("ClaimName")] AppClaim appClaim)
        {
            await _supabase.PatchAsync($"AppClaim?ClaimId=eq.{id}", new { ClaimName = appClaim.ClaimName });
            return Json(new { success = true });
        }

        // ---------------- DELETE ----------------
        //[Authorize(Roles = "DeleteClaim")]
        public async Task<IActionResult> Delete(int id)
        {
            string response = await _supabase.GetAsync($"AppClaim?ClaimId=eq.{id}");
            var appClaim = JsonSerializer.Deserialize<List<AppClaim>>(response)?.FirstOrDefault();
            if (appClaim == null) return Json(new { success = false, message = "Claim-ul nu exista." });

            return PartialView("_DeleteClaim", appClaim);
        }

        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        //[Authorize(Roles = "DeleteClaim")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _supabase.DeleteAsync($"AppClaim?ClaimId=eq.{id}");
            return Json(new { success = true });
        }
    }
}
