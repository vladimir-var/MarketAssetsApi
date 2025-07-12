using System;
using System.Collections.Generic;
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
    public class FintachartsAssetsSyncService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly FintachartsAuthService _authService;
        private readonly ILogger<FintachartsAssetsSyncService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public FintachartsAssetsSyncService(IServiceProvider serviceProvider, FintachartsAuthService authService, ILogger<FintachartsAssetsSyncService> logger, IHttpClientFactory httpClientFactory)
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
                    var instruments = new List<Asset>();

                    var providersUri = "https://platform.fintacharts.com/api/instruments/v1/providers";
                    var providersRequest = new HttpRequestMessage(HttpMethod.Get, providersUri);
                    providersRequest.Headers.Add("Authorization", $"Bearer {token}");
                    var providersResponse = await httpClient.SendAsync(providersRequest, stoppingToken);
                    providersResponse.EnsureSuccessStatusCode();
                    var providersJson = await providersResponse.Content.ReadAsStringAsync(stoppingToken);
                    var providersDoc = JsonDocument.Parse(providersJson);
                    var providers = providersDoc.RootElement.GetProperty("data").EnumerateArray().Select(p => p.GetString()).ToList();

                    foreach (var provider in providers)
                    {
                        var uri = $"https://platform.fintacharts.com/api/instruments/v1/instruments?provider={provider}&kind=forex";
                        var request = new HttpRequestMessage(HttpMethod.Get, uri);
                        request.Headers.Add("Authorization", $"Bearer {token}");
                        var response = await httpClient.SendAsync(request, stoppingToken);
                        if (!response.IsSuccessStatusCode)
                        {
                            _logger.LogWarning($"Не вдалося отримати інструменти для провайдера {provider}");
                            continue;
                        }
                        var json = await response.Content.ReadAsStringAsync(stoppingToken);
                        _logger.LogInformation($"Отримано відповідь від Fintacharts для провайдера {provider}: {json}");
                        var doc = JsonDocument.Parse(json);

                        if (doc.RootElement.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var item in data.EnumerateArray())
                            {
                                try
                                {
                                    var symbol = item.GetProperty("symbol").GetString();
                                    var type = item.GetProperty("kind").GetString();
                                    var instrumentId = item.GetProperty("id").GetString();
                                    var description = item.TryGetProperty("description", out var descProp) ? descProp.GetString() : symbol;
                                    var name = !string.IsNullOrEmpty(description) ? description : symbol;
                                    instruments.Add(new Asset
                                    {
                                        Symbol = symbol,
                                        Name = name,
                                        Type = type,
                                        InstrumentId = instrumentId,
                                        Provider = provider
                                    });
                                    _logger.LogDebug($"Оброблено інструмент: {symbol} ({name}) [{provider}]");
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, $"Помилка обробки інструменту для провайдера {provider}");
                                }
                            }
                            _logger.LogInformation($"Знайдено {instruments.Count} інструментів у data для провайдера {provider}");
                        }
                        else if (doc.RootElement.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var item in doc.RootElement.EnumerateArray())
                            {
                                try
                                {
                                    var symbol = item.GetProperty("symbol").GetString();
                                    var type = item.GetProperty("kind").GetString();
                                    var instrumentId = item.GetProperty("id").GetString();
                                    var description = item.TryGetProperty("description", out var descProp) ? descProp.GetString() : symbol;
                                    var name = !string.IsNullOrEmpty(description) ? description : symbol;
                                    instruments.Add(new Asset
                                    {
                                        Symbol = symbol,
                                        Name = name,
                                        Type = type,
                                        InstrumentId = instrumentId,
                                        Provider = provider
                                    });
                                    _logger.LogDebug($"Оброблено інструмент: {symbol} ({name}) [{provider}]");
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, $"Помилка обробки інструменту для провайдера {provider}");
                                }
                            }
                            _logger.LogInformation($"Знайдено {instruments.Count} інструментів у кореневому масиві для провайдера {provider}");
                        }
                        else
                        {
                            _logger.LogWarning($"Не знайдено масиву активів у відповіді Fintacharts для провайдера {provider}. Структура відповіді: {{Structure}}", doc.RootElement.ValueKind);
                            _logger.LogWarning($"Доступні властивості: {{Properties}}", string.Join(", ", doc.RootElement.EnumerateObject().Select(p => p.Name)));
                        }
                    }

                    foreach (var asset in instruments)
                    {
                        if (!await db.Assets.AnyAsync(a => a.Symbol == asset.Symbol && a.Provider == asset.Provider, stoppingToken))
                        {
                            db.Assets.Add(asset);
                            _logger.LogInformation($"Додано новий актив: {asset.Symbol} [{asset.Provider}]");
                        }
                    }
                    await db.SaveChangesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Помилка синхронізації активів з Fintacharts");
                }
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }
} 