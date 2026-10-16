using CrossTraining.Application.Features.Domains;
using CrossTraining.Application.Features.Users;
using CrossTraining.Web.Extensions;
using CrossTraining.Web.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrossTraining.Web.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly ISender _sender;

    public ProfileController(ISender sender)
    {
        _sender = sender;
    }

    public async Task<IActionResult> Index()
    {
        var userId = User.GetUserId();
        if (userId == null)
            return RedirectToAction("Login", "UserAuth");

        var profile = await _sender.Send(new GetUserProfileQuery(userId.Value));
        if (profile == null)
            return NotFound("User not found");

        return View(profile);
    }

    // ------------------- CHANGE PASSWORD -------------------
    [HttpGet]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var userId = User.GetUserId();
        if (userId == null)
            return RedirectToAction("Login", "UserAuth");

        var result = await _sender.Send(
            new ChangePasswordCommand(userId.Value, model.OldPassword, model.NewPassword));

        if (!result.Succeeded)
        {
            ModelState.AddModelError(result.ErrorField ?? "", result.Error!);
            return View(model);
        }

        ViewBag.Success = "Parola a fost schimbata cu succes!";
        return View(new ChangePasswordViewModel());
    }

    // ------------------- CHANGE DOMAINS (many-to-many) -------------------
    [HttpGet]
    public async Task<IActionResult> ChangeDomain()
    {
        var userId = User.GetUserId();
        if (userId == null)
            return RedirectToAction("Login", "UserAuth");

        return View(new ChangeDomainViewModel
        {
            SelectedDomainIds = await _sender.Send(new GetUserDomainIdsQuery(userId.Value)),
            AvailableDomains = await _sender.Send(new GetActiveDomainsQuery())
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeDomain(ChangeDomainViewModel model)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return RedirectToAction("Login", "UserAuth");

        if (model.SelectedDomainIds == null || model.SelectedDomainIds.Count == 0)
        {
            ModelState.AddModelError("", "Selecteaza cel putin un domeniu.");
            model.AvailableDomains = await _sender.Send(new GetActiveDomainsQuery());
            return View(model);
        }

        await _sender.Send(new SetUserDomainsCommand(userId.Value, model.SelectedDomainIds));

        TempData["Success"] = "Domeniile au fost actualizate.";
        return RedirectToAction("Index");
    }
}
