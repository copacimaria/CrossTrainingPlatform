using CrossTraining.Application.Authorization;
using CrossTraining.Application.Features.Claims;
using CrossTraining.Application.Features.Roles;
using CrossTraining.Web.Extensions;
using CrossTraining.Web.Models;
using CrossTraining.Web.Security;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using X.PagedList.Extensions;

namespace CrossTraining.Web.Controllers;

[HasPermission(Permissions.Roles.View)]
public class AppRolesController : Controller
{
    private const int PageSize = 13;

    private readonly ISender _sender;

    public AppRolesController(ISender sender)
    {
        _sender = sender;
    }

    public async Task<IActionResult> Index(int? page, string roleSearch)
    {
        // Filtrarea pe IsActive nu se mai scrie aici: o face filtrul global de
        // soft-delete. Paginarea si cautarea coboara in baza de date.
        var result = await _sender.Send(new SearchRolesQuery(roleSearch, Math.Max(page ?? 1, 1), PageSize));
        return View(result.ToPagedList());
    }

    public async Task<IActionResult> Details(int id)
    {
        var role = await _sender.Send(new GetRoleQuery(id));
        if (role == null)
            return NotFound();

        var claims = await _sender.Send(new GetRoleClaimsQuery(id));

        return View(new RoleClaimsViewModel
        {
            Role = role,
            Claims = claims.ToPagedList(1, claims.Count == 0 ? 1 : claims.Count)
        });
    }

    [HttpGet]
    [HasPermission(Permissions.Roles.Manage)]
    public IActionResult Create() => PartialView("_CreateRole", new RoleFormModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Permissions.Roles.Manage)]
    public async Task<IActionResult> Create(RoleFormModel form)
    {
        if (!ModelState.IsValid)
            return Json(new { success = false, errors = ModelState.ErrorMessages() });

        var result = await _sender.Send(new CreateRoleCommand(form.RoleName));
        return result.ToJsonResult();
    }

    [HttpGet]
    [HasPermission(Permissions.Roles.Manage)]
    public async Task<IActionResult> Edit(int id)
    {
        var role = await _sender.Send(new GetRoleQuery(id));
        if (role == null)
            return Json(new { success = false, message = "Rolul nu exista." });

        return PartialView("_EditRole", new RoleFormModel
        {
            RoleId = role.RoleId,
            RoleName = role.RoleName
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Permissions.Roles.Manage)]
    public async Task<IActionResult> Edit(int id, RoleFormModel form)
    {
        if (!ModelState.IsValid)
            return Json(new { success = false, errors = ModelState.ErrorMessages() });

        var result = await _sender.Send(new RenameRoleCommand(id, form.RoleName));
        return result.ToJsonResult();
    }

    [HasPermission(Permissions.Roles.Manage)]
    public async Task<IActionResult> Delete(int id)
    {
        var role = await _sender.Send(new GetRoleQuery(id));
        if (role == null)
            return Json(new { success = false, message = "Rolul nu exista." });

        return PartialView("_DeleteRole", role);
    }

    [HttpPost, ActionName("DeleteConfirmed")]
    [ValidateAntiForgeryToken]
    [HasPermission(Permissions.Roles.Manage)]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _sender.Send(new DeactivateRoleCommand(id));
        return result.ToJsonResult();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Permissions.Roles.Manage)]
    public async Task<IActionResult> Reactivate(int id)
    {
        var result = await _sender.Send(new ReactivateRoleCommand(id));
        return result.ToJsonResult();
    }

    // ------------------- ACORDARE / REVOCARE DREPT -------------------
    [HttpGet]
    [HasPermission(Permissions.Roles.Manage)]
    public async Task<IActionResult> AddClaim(int id)
    {
        var role = await _sender.Send(new GetRoleQuery(id));
        if (role == null)
            return Json(new { success = false, message = "Rolul nu a fost gasit." });

        return PartialView("_addClaimInRole", new RoleClaimsViewModel { Role = role });
    }

    /// <summary>
    /// Autocomplete pentru drepturile care mai pot fi adaugate rolului.
    ///
    /// Varianta anterioara primea lista completa din body-ul clientului si o filtra
    /// server-side, adica serverul avea incredere in datele pe care tot clientul i le
    /// trimitea. Acum lista se construieste in baza de date, pornind de la rol.
    /// </summary>
    [HttpGet]
    [HasPermission(Permissions.Roles.Manage)]
    public async Task<IActionResult> SearchAvailableClaims(int roleId, string? term)
        => Json(await _sender.Send(new SearchAvailableClaimsQuery(roleId, term)));

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Permissions.Roles.Manage)]
    public async Task<IActionResult> AddClaim(int roleId, int claimId)
    {
        var result = await _sender.Send(new GrantClaimToRoleCommand(roleId, claimId));
        return result.ToJsonResult();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Permissions.Roles.Manage)]
    public async Task<IActionResult> RemoveClaim(int roleId, int claimId)
    {
        var result = await _sender.Send(new RevokeClaimFromRoleCommand(roleId, claimId));
        return result.ToJsonResult();
    }
}
