using CrossTraining.Application.DTOs;
using CrossTraining.Application.Features.Authentication;
using CrossTraining.Web.Models;
using CrossTraining.Web.Security;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CrossTraining.Web.Controllers;

[AllowAnonymous]
public class UserAuthController : Controller
{
    private readonly ISender _sender;
    private readonly RegistrationTokenProtector _registrationTokens;
    private readonly TimeProvider _clock;

    public UserAuthController(
        ISender sender,
        RegistrationTokenProtector registrationTokens,
        TimeProvider clock)
    {
        _sender = sender;
        _registrationTokens = registrationTokens;
        _clock = clock;
    }

    // --------------------- REGISTER ---------------------
    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(RateLimitPolicies.Authentication)]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _sender.Send(
            new RegisterUserCommand(model.Name, model.FamilyName, model.Email, model.Password));

        if (!result.Succeeded)
        {
            ModelState.AddModelError("", result.Error!);
            return View(model);
        }

        // Id-ul pleaca semnat, nu in clar: pasul urmator se petrece inainte de login,
        // deci nu are de unde sa stie cine e utilizatorul altfel decat din acest token.
        var token = _registrationTokens.Protect(result.NewUserId);
        return RedirectToAction("SelectDomain", "Domain", new { token });
    }

    // --------------------- LOGIN ---------------------
    [HttpGet]
    public IActionResult Login() => View(new LoginViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(RateLimitPolicies.Authentication)]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _sender.Send(new LoginUserQuery(model.Email, model.Password));
        if (!result.Succeeded)
        {
            ModelState.AddModelError("", result.Error!);
            return View(model);
        }

        await SignInUserAsync(result);
        return RedirectToAction("DisplayGamesBasedOnDomain", "Games");
    }

    // --------------------- LOGOUT ---------------------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    // --------------------- ACCES INTERZIS ---------------------
    [HttpGet]
    public IActionResult AccessDenied(string? returnUrl)
    {
        // Pastram calea ceruta doar daca e locala, altfel devine open redirect.
        ViewBag.ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null;
        return View();
    }

    // --------------------- HELPERS ---------------------
    private async Task SignInUserAsync(AuthResult result)
    {
        var user = result.User!;

        // Aceeasi fabrica ca la reimprospatarea cookie-ului, ca sesiunea sa nu piarda
        // tacut un claim dupa prima revalidare.
        var context = new UserSecurityContextDto(
            user.UserId,
            user.FullName,
            user.Email,
            result.RoleName,
            result.Permissions);

        var principal = UserPrincipalFactory.Create(context, _clock.GetUtcNow());

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    }
}
