using CrossTrainingPlatform.Models;
using CrossTrainingPlatform.Models.DTOs;
using CrossTrainingPlatform.Models.UserManagementModels;
using CrossTrainingPlatform.Models.ViewModels;
using CrossTrainingPlatform.Services.DatabaseServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CrossTrainingPlatform.Controllers
{
    public class GamesController : Controller
    {
        private readonly SupabaseService _supabase;

        public GamesController(SupabaseService supabase)
        {
            _supabase = supabase;
        }


        private List<Game> ParseGames(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new List<Game>();

            try
            {
                var token = JToken.Parse(json);

                if (token.Type == JTokenType.Array)
                    return token.ToObject<List<Game>>() ?? new List<Game>();

                var arr =
                    token["data"] ??
                    token["result"] ??
                    token;

                if (arr.Type == JTokenType.Array)
                    return arr.ToObject<List<Game>>() ?? new List<Game>();

                return new List<Game>();
            }
            catch
            {
                return new List<Game>();
            }
        }



        [HttpGet]
        public async Task<IActionResult> DisplayGamesBasedOnDomain()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "UserAuth");

            try
            {
                // luam userul
                string endpointUser = $"AppUser?UserId=eq.{userId}&select=*";
                var responseUser = await _supabase.GetAsync(endpointUser);
                var users = Newtonsoft.Json.JsonConvert.DeserializeObject<List<AppUser>>(responseUser);

                if (users == null || users.Count == 0)
                    return BadRequest("User not found");

                var user = users.First();

                if (user.DomainList == null || user.DomainList.Count == 0)
                    return BadRequest("User has no domains");

                int domainId = user.DomainList.First();

                // luam toate domeniile
                var responseDomains = await _supabase.GetAsync("Domain");
                var allDomains = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Domain>>(responseDomains);

                // jocurile pentru domeniu
                var resultSelectedDomain = await _supabase.GetAsync($"Game?DomainId=eq.{domainId}");
                var resultAll = await _supabase.GetAsync("Game");

                var gamesForDomain = ParseGames(resultSelectedDomain)
                    .Select(g =>
                    {
                        g.Domain = allDomains.FirstOrDefault(d => d.DomainId == g.DomainId);
                        return g;
                    })
                    .ToList();

                var allGames = ParseGames(resultAll)
                    .Select(g =>
                    {
                        g.Domain = allDomains.FirstOrDefault(d => d.DomainId == g.DomainId);
                        return g;
                    })
                    .ToList();

                var vm = new GamesViewModel
                {
                    AllGames = allGames,
                    GamesForSelectedDomain = gamesForDomain
                };

                return View("Index", vm);
            }
            catch
            {
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
            string domainJson = await _supabase.GetAsync("Domain?IsActive=eq.true&select=*");
            var domains = JsonConvert.DeserializeObject<List<Domain>>(domainJson);

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
                string domainJson = await _supabase.GetAsync("Domain?IsActive=eq.true&select=*");
                var domains = JsonConvert.DeserializeObject<List<Domain>>(domainJson);
                ViewBag.Domains = new SelectList(domains, "DomainId", "DomainName");
                return PartialView("_CreateGame", game);
            }

            game.CreatedAt = DateTime.Now;

            GameDTO gameDto = new GameDTO
            {
                GameName = game.GameName,
                IsActive = game.IsActive,
                CreatedAt = game.CreatedAt,
                Description = game.Description,
                DownloadLink = game.DownloadLink,
                DomainId = game.DomainId
            };

            var result = await _supabase.PostAsync("Game", gameDto);

            return Json(new { success = true, message = "Game created successfully!" });
        }

    }




} 







