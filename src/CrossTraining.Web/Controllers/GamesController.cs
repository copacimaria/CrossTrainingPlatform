using CrossTraining.Application.Interfaces;
using CrossTraining.Domain.Entities;
using CrossTraining.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CrossTraining.Web.Controllers;

[Authorize]
public class GamesController : Controller
{
    private readonly IGameService _games;
    private readonly IDomainService _domains;

    public GamesController(IGameService games, IDomainService domains)
    {
        _games = games;
        _domains = domains;
    }

    [HttpGet]
    public async Task<IActionResult> DisplayGamesBasedOnDomain()
    {
        var userId = User.GetUserId();
        if (userId == null)
            return RedirectToAction("Login", "UserAuth");

        var vm = await _games.GetGamesForUserAsync(userId.Value);
        return View("Index", vm);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create()
    {
        await PopulateDomainsAsync();
        return PartialView("_CreateGame", new Game());
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Game game)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDomainsAsync(game.DomainId);
            return PartialView("_CreateGame", game);
        }

        await _games.CreateAsync(game);
        return Json(new { success = true, message = "Game created successfully!" });
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var game = await _games.GetByIdAsync(id);
        if (game == null)
            return NotFound();

        await PopulateDomainsAsync(game.DomainId);
        return PartialView("_EditGame", game);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Game game)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDomainsAsync(game.DomainId);
            return PartialView("_EditGame", game);
        }

        await _games.UpdateAsync(id, game);
        return Json(new { success = true, message = "Game updated successfully!" });
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var game = await _games.GetByIdAsync(id);
        if (game == null)
            return NotFound();

        return PartialView("_DeleteGame", game);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PostDelete(int gameId)
    {
        await _games.SoftDeleteAsync(gameId);
        return Json(new { success = true, message = "Game deleted successfully!" });
    }

    private async Task PopulateDomainsAsync(int? selectedId = null)
    {
        var domains = await _domains.GetActiveAsync();
        ViewBag.Domains = new SelectList(domains, "DomainId", "DomainName", selectedId);
    }
}
