using CrossTraining.Application.Features.Claims;
using CrossTraining.Domain.Authorization;
using CrossTraining.Web.Extensions;
using CrossTraining.Web.Models;
using CrossTraining.Web.Security;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CrossTraining.Web.Controllers;

[HasPermission(Permissions.Claims.View)]
public class AppClaimsController : Controller
{
    private const int PageSize = 13;

    private readonly ISender _sender;

    public AppClaimsController(ISender sender)
    {
        _sender = sender;
    }

    public async Task<IActionResult> Index(int? page, string claimSearch)
    {
        // Filtrarea, numararea si decuparea se fac in baza de date; aici doar adaptam
        // rezultatul la componenta de paginare din view.
        var result = await _sender.Send(new SearchClaimsQuery(claimSearch, Math.Max(page ?? 1, 1), PageSize));
        return View(result.ToPagedList());
    }

    public async Task<IActionResult> Details(int id)
    {
        var claim = await _sender.Send(new GetClaimQuery(id));
        if (claim == null)
            return NotFound();

        return PartialView("_DetailsRolesofClaim", claim);
    }

    [HttpGet]
    [HasPermission(Permissions.Claims.Manage)]
    public IActionResult Create() => PartialView("_CreateClaim", new ClaimFormModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Permissions.Claims.Manage)]
    public async Task<IActionResult> Create(ClaimFormModel form)
    {
        if (!ModelState.IsValid)
            return Json(new { success = false, errors = ModelState.ErrorMessages() });

        var result = await _sender.Send(new CreateClaimCommand(form.ClaimName));
        return result.ToJsonResult();
    }

    [HttpGet]
    [HasPermission(Permissions.Claims.Manage)]
    public async Task<IActionResult> Edit(int id)
    {
        var claim = await _sender.Send(new GetClaimQuery(id));
        if (claim == null)
            return Json(new { success = false, message = "Claim-ul nu exista." });

        return PartialView("_EditClaim", new ClaimFormModel
        {
            ClaimId = claim.ClaimId,
            ClaimName = claim.ClaimName
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Permissions.Claims.Manage)]
    public async Task<IActionResult> Edit(int id, ClaimFormModel form)
    {
        if (!ModelState.IsValid)
            return Json(new { success = false, errors = ModelState.ErrorMessages() });

        var result = await _sender.Send(new RenameClaimCommand(id, form.ClaimName));
        return result.ToJsonResult();
    }

    [HasPermission(Permissions.Claims.Manage)]
    public async Task<IActionResult> Delete(int id)
    {
        var claim = await _sender.Send(new GetClaimQuery(id));
        if (claim == null)
            return Json(new { success = false, message = "Claim-ul nu exista." });

        return PartialView("_DeleteClaim", claim);
    }

    [HttpPost, ActionName("DeleteConfirmed")]
    [ValidateAntiForgeryToken]
    [HasPermission(Permissions.Claims.Manage)]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _sender.Send(new DeactivateClaimCommand(id));
        return result.ToJsonResult();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Permissions.Claims.Manage)]
    public async Task<IActionResult> Reactivate(int id)
    {
        var result = await _sender.Send(new ReactivateClaimCommand(id));
        return result.ToJsonResult();
    }
}
