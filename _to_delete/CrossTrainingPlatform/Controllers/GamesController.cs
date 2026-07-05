using CrossTrainingPlatform.Models;
using CrossTrainingPlatform.Models.ViewModels;
using CrossTrainingPlatform.Services.DatabaseServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CrossTrainingPlatform.Controllers
{
    public class GamesController : Controller
    {
        private readonly GameService _games;
        private readonly DomainService _domains;
        private readonly AppUserService _users;
        private readonly ILogger<GamesController> _logger;

        public GamesController(GameService games, DomainService domains, AppUserService users, ILogger<GamesController> logger)
        {
            _games = games;
            _domains = domains;
            _users = users;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> DisplayGamesBasedOnDomain()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "UserAuth");

            try
            {
                var user = await _users.GetByIdAsync(userId.Value);
                if (user == null)
                    return BadRequest("User not found");

                if (user.DomainList == null || user.DomainList.Count == 0)
                    return BadRequest("User has no domains");

                int domainId = user.DomainList.First();

                var allDomains = await _domains.GetAllAsync();

                var gamesForDomain = await _games.GetByDomainAsync(domainId);
                var allGames = await _games.GetAllAsync();

                foreach (var g in gamesForDomain)
                    g.Domain = allDomains.FirstOrDefault(d => d.DomainId == g.DomainId);

                foreach (var g in allGames)
                    g.Domain = allDomains.FirstOrDefault(d => d.DomainId == g.DomainId);

                var vm = new GamesViewModel
                {
                    AllGames = allGames,
                    GamesForSelectedDomain = gamesForDomain
                };

                return View("Index", vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Eroare la incarcarea jocurilor pentru userId {UserId}", userId);
                return View("Index", new GamesViewModel
                {
                    AllGames = new List<Game>(),
                    GamesForSelectedDomain = new List<Game>()
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var domains = await _domains.GetActiveAsync();
            ViewBag.Domains = new SelectList(domains, "DomainId", "DomainName");

            return PartialView("_CreateGame", new Game());
        }

        // POST: Games/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Game game)
        {
            if (!ModelState.IsValid)
            {
                var domains = await _domains.GetActiveAsync();
                ViewBag.Domains = new SelectList(domains, "DomainId", "DomainName");
                return PartialView("_CreateGame", game);
            }

            game.CreatedAt = DateTime.Now;
            await _games.CreateAsync(game);

            return Json(new { success = true, message = "Game created successfully!" });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var game = await _games.GetByIdAsync(id);
            if (game == null)
                return NotFound();

            var domains = await _domains.GetActiveAsync();
            ViewBag.Domains = new SelectList(domains, "DomainId", "DomainName", game.DomainId);

            return PartialView("_EditGame", game);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Game game)
        {
            if (!ModelState.IsValid)
            {
                var domains = await _domains.GetActiveAsync();
                ViewBag.Domains = new SelectList(domains, "DomainId", "DomainName", game.DomainId);
                return PartialView("_EditGame", game);
            }

            await _games.UpdateAsync(id, game);

            return Json(new { success = true, message = "Game updated successfully!" });
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var game = await _games.GetByIdAsync(id);
            if (game == null)
                return NotFound();

            return PartialView("_DeleteGame", game);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PostDelete(int gameId)
        {
            await _games.SoftDeleteAsync(gameId);

            return Json(new { success = true, message = "Game deleted successfully!" });
        }
    }
}
