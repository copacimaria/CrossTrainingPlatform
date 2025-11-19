using CrossTrainingPlatform.Models;
using CrossTrainingPlatform.Services.DatabaseServices;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace CrossTrainingPlatform.Controllers
{
    public class DomainController : Controller
    {
        private readonly SupabaseService _supabase;

        public DomainController(SupabaseService supabase)
        {
            _supabase = supabase;
        }

        // ------------------- INDEX -------------------
        public async Task<IActionResult> Index()
        {
            string response = await _supabase.GetAsync("Domain?select=*");
            var domains = JsonSerializer.Deserialize<List<Domain>>(response);
            return View(domains);
        }

        // ------------------- CREATE -------------------
        [HttpGet]
        public IActionResult Create()
        {
            return View("_createDomain");
        }

        [HttpPost]
        public async Task<IActionResult> Create(Domain domain)
        {
            if (!ModelState.IsValid)
                return View(domain);

            var newDomain = new
            {
                DomainName = domain.DomainName,
                IsActive = domain.IsActive,
                GameId = domain.GameId
            };

            await _supabase.PostAsync("Domain", newDomain);
            return RedirectToAction("Index");
        }

        // ------------------- EDIT -------------------
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            string response = await _supabase.GetAsync($"Domain?DomainId=eq.{id}");
            var domain = JsonSerializer.Deserialize<List<Domain>>(response)?.FirstOrDefault();
            if (domain == null)
                return NotFound();

            return View("_editDomain", domain);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, Domain domain)
        {
            if (!ModelState.IsValid)
                return View(domain);

            var updatedDomain = new
            {
                DomainName = domain.DomainName,
                IsActive = domain.IsActive,
                GameId = domain.GameId
            };

            await _supabase.PatchAsync($"Domain?DomainId=eq.{id}", updatedDomain);
            return RedirectToAction("Index");
        }

        // ------------------- DELETE -------------------
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            string response = await _supabase.GetAsync($"Domain?DomainId=eq.{id}");
            var domain = JsonSerializer.Deserialize<List<Domain>>(response)?.FirstOrDefault();
            if (domain == null)
                return NotFound();

            return View(domain);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _supabase.DeleteAsync($"Domain?DomainId=eq.{id}");
            return RedirectToAction("Index");
        }

     
        public async Task<IActionResult> SelectDomain(int userId)
        {
            string response = await _supabase.GetAsync("Domain?IsActive=eq.true&select=*");
            var domains = JsonSerializer.Deserialize<List<Domain>>(response);

            ViewBag.DomainList = domains;
            ViewBag.UserId = userId;

            return View();
        }


        [HttpPost]
        public async Task<IActionResult> SaveUserDomain(int userId, int domainId)
        {
            if (domainId == 0)
                return RedirectToAction("SelectDomain", new { userId }); // daca nu a ales nimic

            // Patch in Supabase pentru user
            
              var updateData = new Dictionary<string, object>
              {
                  { "DomainList", new int[] { domainId } }
              };

            await _supabase.PatchAsync($"AppUser?UserId=eq.{userId}", updateData);


            // Redirect catre login
            return RedirectToAction("Login", "UserAuth");
        }



    }
}
