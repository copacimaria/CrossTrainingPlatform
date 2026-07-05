using CrossTraining.Application.Interfaces;
using CrossTraining.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrossTraining.Web.Controllers;

public class DomainController : Controller
{
    private readonly IDomainService _domains;
    private readonly IUserService _users;

    public DomainController(IDomainService domains, IUserService users)
    {
        _domains = domains;
        _users = users;
    }

    // ------------------- ADMIN CRUD -------------------
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Index()
    {
        var domains = await _domains.GetAllAsync();
        return View(domains);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult Create() => View("_createDomain");

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TrainingDomain domain)
    {
        if (!ModelState.IsValid)
            return View("_createDomain", domain);

        await _domains.CreateAsync(domain);
        return RedirectToAction("Index");
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var domain = await _domains.GetByIdAsync(id);
        if (domain == null)
            return NotFound();

        return View("_editDomain", domain);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TrainingDomain domain)
    {
        if (!ModelState.IsValid)
            return View("_editDomain", domain);

        await _domains.UpdateAsync(id, domain);
        return RedirectToAction("Index");
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var domain = await _domains.GetByIdAsync(id);
        if (domain == null)
            return NotFound();

        return View("_deleteDomain", domain);
    }

    [HttpPost, ActionName("Delete")]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _domains.DeleteAsync(id);
        return RedirectToAction("Index");
    }

    // ------------------- SELECT DOMAIN (flow de inregistrare) -------------------
    // Accesibil fara autentificare: face parte din procesul de creare cont.
    [HttpGet]
    public async Task<IActionResult> SelectDomain(int userId)
    {
        var domains = await _domains.GetActiveAsync();
        ViewBag.DomainList = domains;
        ViewBag.UserId = userId;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveUserDomain(int userId, List<int> domainIds)
    {
        if (domainIds == null || domainIds.Count == 0)
            return RedirectToAction("SelectDomain", new { userId });

        await _users.SetUserDomainsAsync(userId, domainIds);
        return RedirectToAction("Login", "UserAuth");
    }
}
