using CrossTraining.Application.Interfaces;
using CrossTraining.Application.ViewModels;
using CrossTraining.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrossTraining.Web.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly IUserService _users;
    private readonly IDomainService _domains;

    public ProfileController(IUserService users, IDomainService domains)
    {
        _users = users;
        _domains = domains;
    }

    public async Task<IActionResult> Index()
    {
        var userId = User.GetUserId();
        if (userId == null)
            return RedirectToAction("Login", "UserAuth");

        var profile = await _users.GetProfileAsync(userId.Value);
        if (profile == null)
            return NotFound("User not found");

        return View(profile);
    }

    // ------------------- CHANGE PASSWORD -------------------
    [HttpGet]
    public IActionResult ChangePassword() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var userId = User.GetUserId();
        if (userId == null)
            return RedirectToAction("Login", "UserAuth");

        var result = await _users.ChangePasswordAsync(userId.Value, model);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(result.ErrorField ?? "", result.Error!);
            return View(model);
        }

        ViewBag.Success = "Parola a fost schimbata cu succes!";
        return View();
    }

    // ------------------- CHANGE DOMAINS (many-to-many) -------------------
    [HttpGet]
    public async Task<IActionResult> ChangeDomain()
    {
        var userId = User.GetUserId();
        if (userId == null)
            return RedirectToAction("Login", "UserAuth");

        var vm = new ChangeDomainViewModel
        {
            SelectedDomainIds = await _users.GetUserDomainIdsAsync(userId.Value),
            AvailableDomains = await _domains.GetActiveAsync()
        };

        return View(vm);
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
            model.AvailableDomains = await _domains.GetActiveAsync();
            return View(model);
        }

        await _users.SetUserDomainsAsync(userId.Value, model.SelectedDomainIds);

        TempData["Success"] = "Domeniile au fost actualizate.";
        return RedirectToAction("Index");
    }
}
