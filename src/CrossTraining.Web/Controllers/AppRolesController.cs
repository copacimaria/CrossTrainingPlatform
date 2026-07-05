using System.Text;
using CrossTraining.Application.DTOs;
using CrossTraining.Application.ViewModels;
using CrossTraining.Application.Interfaces;
using CrossTraining.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using X.PagedList.Extensions;

namespace CrossTraining.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AppRolesController : Controller
{
    private const int PageSize = 13;
    private readonly IRoleService _roles;
    private readonly IClaimService _claims;

    public AppRolesController(IRoleService roles, IClaimService claims)
    {
        _roles = roles;
        _claims = claims;
    }

    public async Task<IActionResult> Index(int? page, string roleSearch)
    {
        int pageNumber = page ?? 1;
        var roles = await _roles.GetAllAsync();
        IEnumerable<AppRole> activeRoles = roles.Where(r => r.IsActive);

        if (!string.IsNullOrEmpty(roleSearch))
        {
            activeRoles = activeRoles
                .Where(r => r.RoleName.Contains(roleSearch, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var paged = activeRoles.OrderBy(r => r.RoleName).ToPagedList(pageNumber, PageSize);
        return View(paged);
    }

    public async Task<IActionResult> Details(int id)
    {
        var role = await _roles.GetByIdAsync(id);
        if (role == null)
            return NotFound();

        var claims = await _roles.GetClaimsForRoleAsync(id);

        var vm = new AppRolesClaimsViewModel
        {
            Role = role,
            Claims = claims.ToPagedList(1, claims.Count == 0 ? 1 : claims.Count)
        };

        return View(vm);
    }

    public IActionResult Create() => PartialView("_CreateRole");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("RoleName")] AppRole appRole)
    {
        if (string.IsNullOrWhiteSpace(appRole.RoleName))
            return Json(new { success = false, errors = new[] { "Rolul trebuie sa aibe un nume!" } });

        await _roles.CreateAsync(appRole.RoleName);
        return Json(new { success = true });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var role = await _roles.GetByIdAsync(id);
        if (role == null)
            return Json(new { success = false, message = "Rolul nu exista." });

        return PartialView("_EditRole", role);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
            return Json(new { success = false, errors = new[] { "Completati numele rolului!" } });

        await _roles.UpdateNameAsync(id, roleName);
        return Json(new { success = true });
    }

    public async Task<IActionResult> Delete(int id)
    {
        var role = await _roles.GetByIdAsync(id);
        if (role == null)
            return Json(new { success = false, message = "Rolul nu exista." });

        return PartialView("_DeleteRole", role);
    }

    [HttpPost, ActionName("DeleteConfirmed")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _roles.SoftDeleteAsync(id);
        return Json(new { success = true });
    }

    // ------------------- ADD / REMOVE CLAIM -------------------
    public async Task<IActionResult> AddClaim(int id)
    {
        var role = await _roles.GetByIdAsync(id);
        if (role == null)
            return Json(new { success = false, message = "Rolul nu a fost gasit." });

        var allClaims = await _claims.GetAllAsync();
        var existingClaimIds = await _roles.GetClaimIdsForRoleAsync(id);

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
    public async Task<IActionResult> RemoveClaim(int roleId, int claimId)
    {
        await _roles.RemoveClaimAsync(roleId, claimId);
        return Json(new { success = true });
    }
}
