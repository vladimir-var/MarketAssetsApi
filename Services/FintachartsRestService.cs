using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace MarketAssetsApi.Services
{
    public class FintachartsRestService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly FintachartsAuthService _authService;

        public FintachartsRestService(HttpClient httpClient, IConfiguration configuration, FintachartsAuthService authService)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _authService = authService;
        }

        public async Task<string> GetHistoricalBarsAsync(string instrumentId, string provider, string interval, int barsCount)
        {
            var token = await _authService.GetAccessTokenAsync();
            var uri = $"https://platform.fintacharts.com/api/bars/v1/bars/count-back?instrumentId={instrumentId}&provider={provider}&interval={interval}&barsCount={barsCount}";
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.Add("Authorization", $"Bearer {token}");
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }
} 