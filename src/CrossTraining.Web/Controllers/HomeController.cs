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
    public IActionResult Index() => View();

    /// <summary>
    /// Pagina pentru erorile neasteptate din cererile de pagina. Exceptia a fost deja
    /// logata de middleware-ul de exceptii; aici doar se afiseaza codul cererii, ca
    /// utilizatorul sa-l poata da la suport si eroarea sa fie gasita in log.
    /// </summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
        => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
