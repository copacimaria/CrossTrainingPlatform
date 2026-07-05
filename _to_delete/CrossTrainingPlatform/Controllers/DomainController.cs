using CrossTrainingPlatform.Models;
using CrossTrainingPlatform.Services.DatabaseServices;
using Microsoft.AspNetCore.Mvc;

namespace CrossTrainingPlatform.Controllers
{
    public class DomainController : Controller
    {
        private readonly DomainService _domains;
        private readonly AppUserService _users;

        public DomainController(DomainService domains, AppUserService users)
        {
            _domains = domains;
            _users = users;
        }

        // ------------------- INDEX -------------------
        public async Task<IActionResult> Index()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "UserAuth");

            var domains = await _domains.GetAllAsync();
            return View(domains);
        }

        // ------------------- CREATE -------------------
        [HttpGet]
        public IActionResult Create()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "UserAuth");

            return View("_createDomain");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Domain domain)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "UserAuth");

            if (!ModelState.IsValid)
                return View(domain);

            await _domains.CreateAsync(domain);
            return RedirectToAction("Index");
        }

        // ------------------- EDIT -------------------
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "UserAuth");

            var domain = await _domains.GetByIdAsync(id);
            if (domain == null)
                return NotFound();

            return View("_editDomain", domain);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Domain domain)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "UserAuth");

            if (!ModelState.IsValid)
                return View(domain);

            await _domains.UpdateAsync(id, domain);
            return RedirectToAction("Index");
        }

        // ------------------- DELETE -------------------
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "UserAuth");

            var domain = await _domains.GetByIdAsync(id);
            if (domain == null)
                return NotFound();

            return View(domain);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "UserAuth");

            await _domains.DeleteAsync(id);
            return RedirectToAction("Index");
        }

        // ------------------- SELECT DOMAIN (la inregistrare) -------------------
        public async Task<IActionResult> SelectDomain(int userId)
        {
            var domains = await _domains.GetActiveAsync();

            ViewBag.DomainList = domains;
            ViewBag.UserId = userId;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveUserDomain(int userId, int domainId)
        {
            if (domainId == 0)
                return RedirectToAction("SelectDomain", new { userId }); // daca nu a ales nimic

            await _users.UpdateDomainListAsync(userId, new List<int> { domainId });

            // Redirect catre login
            return RedirectToAction("Login", "UserAuth");
        }

        // ------------------- HELPERS -------------------
        /// <summary>Verifica daca utilizatorul din sesiune are rolul Admin.</summary>
        private bool IsAdmin()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
                return false;

            var role = HttpContext.Session.GetString("RoleName");
            return string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
        }
    }
}
