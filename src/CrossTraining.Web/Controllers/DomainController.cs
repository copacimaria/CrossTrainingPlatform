using CrossTraining.Application.Authorization;
using CrossTraining.Application.Features.Domains;
using CrossTraining.Application.Features.Users;
using CrossTraining.Web.Models;
using CrossTraining.Web.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrossTraining.Web.Controllers;

/// <summary>
/// Administrarea domeniilor, plus pasul de alegere a domeniilor din fluxul de inregistrare.
/// Lista se vede cu domains.view, modificarile cer domains.manage - la fel ca la drepturi si
/// roluri. Cele doua actiuni din inregistrare sunt marcate explicit ca anonime.
/// </summary>
[HasPermission(Permissions.Domains.View)]
public class DomainController : Controller
{
    private readonly ISender _sender;
    private readonly RegistrationTokenProtector _registrationTokens;

    public DomainController(ISender sender, RegistrationTokenProtector registrationTokens)
    {
        _sender = sender;
        _registrationTokens = registrationTokens;
    }

    // ------------------- ADMIN CRUD -------------------
    public async Task<IActionResult> Index()
        => View(await _sender.Send(new GetAllDomainsForAdminQuery()));

    [HttpGet]
    [HasPermission(Permissions.Domains.Manage)]
    public IActionResult Create() => View("_createDomain", new DomainFormModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Permissions.Domains.Manage)]
    public async Task<IActionResult> Create(DomainFormModel form)
    {
        if (!ModelState.IsValid)
            return View("_createDomain", form);

        await _sender.Send(new CreateDomainCommand(form.DomainName, form.IsActive));
        return RedirectToAction("Index");
    }

    [HttpGet]
    [HasPermission(Permissions.Domains.Manage)]
    public async Task<IActionResult> Edit(int id)
    {
        var domain = await _sender.Send(new GetDomainQuery(id));
        if (domain == null)
            return NotFound();

        return View("_editDomain", DomainFormModel.FromDto(domain));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Permissions.Domains.Manage)]
    public async Task<IActionResult> Edit(int id, DomainFormModel form)
    {
        if (!ModelState.IsValid)
            return View("_editDomain", form);

        await _sender.Send(new UpdateDomainCommand(id, form.DomainName, form.IsActive));
        return RedirectToAction("Index");
    }

    [HttpGet]
    [HasPermission(Permissions.Domains.Manage)]
    public async Task<IActionResult> Delete(int id)
    {
        var domain = await _sender.Send(new GetDomainQuery(id));
        if (domain == null)
            return NotFound();

        return View("_deleteDomain", domain);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [HasPermission(Permissions.Domains.Manage)]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _sender.Send(new DeactivateDomainCommand(id));
        return RedirectToAction("Index");
    }

    // ------------------- SELECT DOMAIN (flow de inregistrare) -------------------
    // Se petrece intre register si login, deci nu exista utilizator autentificat.
    // Identitatea vine dintr-un token semnat, emis la register - nu dintr-un id
    // trimis de client, care ar permite oricui sa rescrie domeniile oricarui cont.
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> SelectDomain(string? token)
    {
        if (_registrationTokens.Unprotect(token) == null)
            return RedirectToAction("Register", "UserAuth");

        return View(new SelectDomainViewModel
        {
            Token = token,
            Domains = await _sender.Send(new GetActiveDomainsQuery())
        });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveUserDomain(string? token, List<int> domainIds)
    {
        var userId = _registrationTokens.Unprotect(token);
        if (userId == null)
            return RedirectToAction("Register", "UserAuth");

        if (domainIds == null || domainIds.Count == 0)
            return RedirectToAction("SelectDomain", new { token });

        await _sender.Send(new SetUserDomainsCommand(userId.Value, domainIds));
        return RedirectToAction("Login", "UserAuth");
    }
}
