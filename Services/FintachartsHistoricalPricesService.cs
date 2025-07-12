using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MarketAssetsApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MarketAssetsApi.Services
{
    public class FintachartsHistoricalPricesService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly FintachartsAuthService _authService;
        private readonly ILogger<FintachartsHistoricalPricesService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public FintachartsHistoricalPricesService(IServiceProvider serviceProvider, FintachartsAuthService authService, ILogger<FintachartsHistoricalPricesService> logger, IHttpClientFactory httpClientFactory)
        {
            _serviceProvider = serviceProvider;
            _authService = authService;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<MarketAssetsDbContext>();
                    var httpClient = _httpClientFactory.CreateClient();
                    var token = await _authService.GetAccessTokenAsync();
                    var assets = await db.Assets.ToListAsync(stoppingToken);
                    foreach (var asset in assets)
                    {
                        if (string.IsNullOrEmpty(asset.InstrumentId) || string.IsNullOrEmpty(asset.Provider))
                            continue;
                        var instrumentId = asset.InstrumentId;
                        var provider = asset.Provider;
                        var uri = $"https://platform.fintacharts.com/api/bars/v1/bars/count-back?instrumentId={instrumentId}&provider={provider}&interval=1&periodicity=minute&barsCount=100";
                        var request = new HttpRequestMessage(HttpMethod.Get, uri);
                        request.Headers.Add("Authorization", $"Bearer {token}");
                        var response = await httpClient.SendAsync(request, stoppingToken);
                        if (!response.IsSuccessStatusCode)
                        {
                            _logger.LogWarning($"Не вдалося отримати історичні ціни для {asset.Symbol}");
                            continue;
                        }
                        var json = await response.Content.ReadAsStringAsync(stoppingToken);
                        var doc = JsonDocument.Parse(json);
                        if (!doc.RootElement.TryGetProperty("bars", out var bars))
                            continue;
                        foreach (var bar in bars.EnumerateArray())
                        {
                            var price = bar.GetProperty("close").GetDecimal();
                            var updatedAt = bar.GetProperty("endTime").GetDateTime();
                            if (updatedAt.Kind == DateTimeKind.Local)
                                updatedAt = updatedAt.ToUniversalTime();
                            else if (updatedAt.Kind == DateTimeKind.Unspecified)
                                updatedAt = DateTime.SpecifyKind(updatedAt, DateTimeKind.Utc);
                            // Уникаємо дублювання
                            bool exists = await db.Prices.AnyAsync(p => p.AssetId == asset.Id && p.UpdatedAt == updatedAt, stoppingToken);
                            if (!exists)
                            {
                                db.Prices.Add(new Price
                                {
                                    AssetId = asset.Id,
                                    Value = price,
                                    UpdatedAt = updatedAt
                                });
                            }
                        }
                        await db.SaveChangesAsync(stoppingToken);
                        _logger.LogInformation($"Історичні ціни для {asset.Symbol} оновлено");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Помилка при оновленні історичних цін з Fintacharts");
                }
                await Task.Delay(TimeSpan.FromHours(6), stoppingToken); // Оновлювати раз на 6 годин
            }
        }
    }
} 