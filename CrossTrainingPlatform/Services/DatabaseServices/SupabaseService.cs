using System.Text;
using Newtonsoft.Json;


namespace CrossTrainingPlatform.Services.DatabaseServices
{
    public class SupabaseService
    {
        private readonly string _url;
        private readonly string _key;

        public SupabaseService(IConfiguration config)
        {
            _url = config["Supabase:Url"];
            _key = config["Supabase:AnonKey"];
        }

        public async Task<string> GetAsync(string endpoint)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("apikey", _key);
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_key}");

            var response = await client.GetAsync($"{_url}/rest/v1/{endpoint}");
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> PostAsync(string endpoint, object data)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("apikey", _key);
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_key}");

            var json = new StringContent(
                JsonConvert.SerializeObject(data),
                Encoding.UTF8,
                "application/json"
            );

            var response = await client.PostAsync($"{_url}/rest/v1/{endpoint}", json);
            return await response.Content.ReadAsStringAsync();
        }

        // PATCH (update)
        public async Task<string> PatchAsync(string endpoint, object data)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("apikey", _key);
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_key}");

            var json = new StringContent(
                JsonConvert.SerializeObject(data),
                Encoding.UTF8,
                "application/json"
            );

            var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"{_url}/rest/v1/{endpoint}")
            {
                Content = json
            };

            var response = await client.SendAsync(request);
            return await response.Content.ReadAsStringAsync();
        }


        // DELETE
        public async Task<string> DeleteAsync(string endpoint)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("apikey", _key);
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_key}");

            var response = await client.DeleteAsync($"{_url}/rest/v1/{endpoint}");
            return await response.Content.ReadAsStringAsync();
        }
    }
}
