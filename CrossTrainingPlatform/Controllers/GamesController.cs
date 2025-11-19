using CrossTrainingPlatform.Models;
using CrossTrainingPlatform.Models.DTOs;
using CrossTrainingPlatform.Models.UserManagementModels;
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



        [HttpGet]
        public async Task<IActionResult> DisplayGamesBasedOnDomain()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "UserAuth");

            try
            {
                // luam userul (fara joinuri, DomainList vine din JSONB)
                string endpointUser = $"AppUser?UserId=eq.{userId}&select=*";
                var responseUser = await _supabase.GetAsync(endpointUser);
                var users = Newtonsoft.Json.JsonConvert.DeserializeObject<List<AppUser>>(responseUser);

                if (users == null || users.Count == 0)
                    return BadRequest("User not found");

                var user = users.First();

                // DomainList e lista de int
                if (user.DomainList == null || user.DomainList.Count == 0)
                    return BadRequest("User has no domains");

                // luam primul DomainId
                int domainId = user.DomainList.First();

                // jocurile pentru domeniu
                string endpointGameForSelectedDomain = $"Game?DomainId=eq.{domainId}";
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




