using CrossTraining.Application.DTOs;
using CrossTraining.Application.Features.Domains;
using CrossTraining.Application.Features.Games;
using CrossTraining.Domain.Authorization;
using CrossTraining.Web.Extensions;
using CrossTraining.Web.Models;
using CrossTraining.Web.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CrossTraining.Web.Controllers;

[Authorize]
public class GamesController : Controller
{
    private readonly ISender _sender;

    public GamesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> DisplayGamesBasedOnDomain()
    {
        var userId = User.GetUserId();
        if (userId == null)
            return RedirectToAction("Login", "UserAuth");

        return View("Index", await _sender.Send(new GetGamesForUserQuery(userId.Value)));
    }

    [HttpGet]
    [HasPermission(Permissions.Games.Manage)]
    public async Task<IActionResult> Create()
    {
        await PopulateDomainsAsync();
        return PartialView("_CreateGame", new GameFormModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Permissions.Games.Manage)]
    public async Task<IActionResult> Create(GameFormModel form)
    {
        if (!ModelState.IsValid)
            return Json(new { success = false, errors = ModelState.ErrorMessages() });

        var result = await _sender.Send(new CreateGameCommand(
            form.GameName, form.Description, form.DownloadLink, form.DomainId));

        return result.ToJsonResult();
    }

    [HttpGet]
    [HasPermission(Permissions.Games.Manage)]
    public async Task<IActionResult> Edit(int id)
    {
        var game = await _sender.Send(new GetGameQuery(id));
        if (game == null)
            return NotFound();

        await PopulateDomainsAsync(game.DomainId);
        return PartialView("_EditGame", GameFormModel.FromDto(game));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Permissions.Games.Manage)]
    public async Task<IActionResult> Edit(int id, GameFormModel form)
    {
        if (!ModelState.IsValid)
            return Json(new { success = false, errors = ModelState.ErrorMessages() });

        var result = await _sender.Send(new UpdateGameCommand(
            id, form.GameName, form.Description, form.DownloadLink, form.DomainId));

        return result.ToJsonResult();
    }

    [HttpGet]
    [HasPermission(Permissions.Games.Manage)]
    public async Task<IActionResult> Delete(int id)
    {
        var game = await _sender.Send(new GetGameQuery(id));
        if (game == null)
            return NotFound();

        return PartialView("_DeleteGame", game);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Permissions.Games.Manage)]
    public async Task<IActionResult> PostDelete(int gameId)
    {
        var result = await _sender.Send(new DeactivateGameCommand(gameId));
        return result.ToJsonResult();
    }

    private async Task PopulateDomainsAsync(int? selectedId = null)
    {
        var domains = await _sender.Send(new GetActiveDomainsQuery());
        ViewBag.Domains = new SelectList(
            domains, nameof(DomainDto.DomainId), nameof(DomainDto.DomainName), selectedId);
    }
}
