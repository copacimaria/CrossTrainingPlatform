using CrossTrainingPlatform.Models;
using CrossTrainingPlatform.Models.DTOs;
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


        [HttpPost]
        public async Task<IActionResult> DisplayGamesBasedOnDomain(int? DomainId)
        {
            string endpoint = DomainId.HasValue
                ? $"Game?DomainId=eq.{DomainId.Value}"
                : "Game";

            string resultJson;
            try
            {
                resultJson = await _supabase.GetAsync(endpoint);
            }
            catch (HttpRequestException)
            {
                // Supabase GET failed (network/error); return empty list or show an error view/message
                return View("Index", new List<Game>());
            }

            var games = new List<Game>();

            try
            {
                if (!string.IsNullOrWhiteSpace(resultJson))
                {
                    var token = JToken.Parse(resultJson);

                    if (token.Type == JTokenType.Array)
                    {
                        games = token.ToObject<List<Game>>() ?? new List<Game>();
                    }
                    else if (token.Type == JTokenType.Object)
                    {
                        // If Supabase wrapped the data, try common properties
                        var arr = token.SelectToken("data") ?? token.SelectToken("result") ?? token;
                        if (arr != null && arr.Type == JTokenType.Array)
                            games = arr.ToObject<List<Game>>() ?? new List<Game>();
                        else
                            games = new List<Game>();
                    }
                }
            }
            catch (JsonReaderException)
            {
                // Malformed JSON — fall back to empty list
                games = new List<Game>();
            }

            return View("Index", games);
        }
    }
}



