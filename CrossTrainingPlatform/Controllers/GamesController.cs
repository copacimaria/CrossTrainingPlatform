using CrossTrainingPlatform.Models;
using CrossTrainingPlatform.Models.DTOs;
using CrossTrainingPlatform.Models.ViewModels;
using CrossTrainingPlatform.Services.DatabaseServices;
using Microsoft.AspNetCore.Mvc;
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



        [HttpPost]
        public async Task<IActionResult> DisplayGamesBasedOnDomain()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "UserAuth");

            // luam domeniul din baza de date, asociat userId
            string endpoint = $"UserDomains?UserId=eq.{userId}&select=DomainId";
            string response = await _supabase.GetAsync(endpoint);
            var userDomains = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Domain>>(response);

            if (userDomains == null || userDomains.Count == 0)
            {
                // user nu are domenii
                return BadRequest("No domains assigned to user");
            }

            int DomainId = userDomains.First().DomainId;

            string endpointGameForSelectedDomain = (DomainId > 0)
                ? $"Game?DomainId=eq.{DomainId}"
                : "Game";

            try
            {
                var resultSelectedDomain = await _supabase.GetAsync(endpointGameForSelectedDomain);
                var resultAll = await _supabase.GetAsync("Game");

                var gamesForDomain = ParseGames(resultSelectedDomain);
                var allGames = ParseGames(resultAll);

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

    }
}



