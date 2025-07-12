using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace MarketAssetsApi.Services
{
    public class FintachartsAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public FintachartsAuthService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<string> GetAccessTokenAsync()
        {
            var uri = _configuration["Fintacharts:AuthUri"] ?? "https://platform.fintacharts.com/identity/realms/fintatech/protocol/openid-connect/token";
            var username = _configuration["Fintacharts:Username"];
            var password = _configuration["Fintacharts:Password"];
            var clientId = _configuration["Fintacharts:ClientId"] ?? "app-cli";

            var content = new StringContent($"grant_type=password&client_id={clientId}&username={username}&password={password}", Encoding.UTF8, "application/x-www-form-urlencoded");

            var response = await _httpClient.PostAsync(uri, content);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("access_token").GetString();
        }
    }
} 