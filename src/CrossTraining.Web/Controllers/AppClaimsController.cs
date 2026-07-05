using CrossTraining.Application.Interfaces;
using CrossTraining.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using X.PagedList.Extensions;

namespace CrossTraining.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AppClaimsController : Controller
{
    private const int PageSize = 13;
    private readonly IClaimService _claims;

    public AppClaimsController(IClaimService claims)
    {
        _claims = claims;
    }

    public async Task<IActionResult> Index(int? page, string claimSearch)
    {
        int pageNumber = page ?? 1;
        var appClaims = await _claims.GetAllAsync();

        if (!string.IsNullOrEmpty(claimSearch))
        {
            appClaims = appClaims
                .Where(c => c.ClaimName.Contains(claimSearch, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var paged = appClaims.OrderBy(c => c.ClaimId).ToPagedList(pageNumber, PageSize);
        return View(paged);
    }

    public async Task<IActionResult> Details(int id)
    {
        var appClaim = await _claims.GetByIdAsync(id);
        if (appClaim == null)
            return NotFound();

        return PartialView("_DetailsRolesofClaim", appClaim);
    }

    public IActionResult Create() => PartialView("_CreateClaim");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ClaimName")] AppClaim appClaim)
    {
        if (string.IsNullOrWhiteSpace(appClaim.ClaimName))
            return Json(new { success = false, errors = new[] { "Claim-ul trebuie sa aiba un nume!" } });

        await _claims.CreateAsync(appClaim.ClaimName);
        return Json(new { success = true });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var appClaim = await _claims.GetByIdAsync(id);
        if (appClaim == null)
            return Json(new { success = false, message = "Claim-ul nu exista." });

        return PartialView("_EditClaim", appClaim);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("ClaimName")] AppClaim appClaim)
    {
        await _claims.UpdateNameAsync(id, appClaim.ClaimName);
        return Json(new { success = true });
    }

    public async Task<IActionResult> Delete(int id)
    {
        var appClaim = await _claims.GetByIdAsync(id);
        if (appClaim == null)
            return Json(new { success = false, message = "Claim-ul nu exista." });

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
