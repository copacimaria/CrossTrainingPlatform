using System.Security.Claims;
using CrossTraining.Application.Interfaces;
using CrossTraining.Application.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace CrossTraining.Web.Controllers;

public class UserAuthController : Controller
{
    private readonly IAuthService _auth;

    public UserAuthController(IAuthService auth)
    {
        _auth = auth;
    }

    // --------------------- REGISTER ---------------------
    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _auth.RegisterAsync(model);
        if (!result.Succeeded)
        {
            ModelState.AddModelError("", result.Error!);
            return View(model);
        }

        return RedirectToAction("SelectDomain", "Domain", new { userId = result.NewUserId });
    }

    // --------------------- LOGIN ---------------------
    [HttpGet]
    public IActionResult Login() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _auth.LoginAsync(model);
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

    // --------------------- HELPERS ---------------------
    private async Task SignInUserAsync(Application.DTOs.AuthResult result)
    {
        var user = result.User!;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, $"{user.Name} {user.FamilyName}"),
            new(ClaimTypes.Email, user.Email)
        };

        if (!string.IsNullOrEmpty(result.RoleName))
            claims.Add(new Claim(ClaimTypes.Role, result.RoleName));

        // Fiecare permisiune devine un claim; view-urile folosesc User.HasClaim(c => c.Value == "...").
        foreach (var permission in result.Permissions)
            claims.Add(new Claim("permission", permission));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    }
}
