using System.Diagnostics;
using CrossTraining.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrossTraining.Web.Controllers;

// Pagina de prezentare si pagina de eroare trebuie sa ramana accesibile fara cont;
// restul aplicatiei cere autentificare prin politica implicita din Program.cs.
[AllowAnonymous]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index() => View();

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

        // TODO(etapa 7): inlocuit cu middleware global de exceptii + ProblemDetails.
        var feature = HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        _logger.LogError(feature?.Error, "Cerere esuata. RequestId: {RequestId}, Path: {Path}", requestId, feature?.Path);

        return View(new ErrorViewModel { RequestId = requestId });
    }
}
