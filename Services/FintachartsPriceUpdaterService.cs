using System;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
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
    public class FintachartsPriceUpdaterService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly FintachartsAuthService _authService;
        private readonly ILogger<FintachartsPriceUpdaterService> _logger;

        public FintachartsPriceUpdaterService(IServiceProvider serviceProvider, FintachartsAuthService authService, ILogger<FintachartsPriceUpdaterService> logger)
        {
            _serviceProvider = serviceProvider;
            _authService = authService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<MarketAssetsDbContext>();
                    var assets = await db.Assets.ToListAsync(stoppingToken);
                    if (!assets.Any())
                    {
                        _logger.LogInformation("No assets found in DB. Waiting...");
                        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                        continue;
                    }

                    var token = await _authService.GetAccessTokenAsync();
                    var wsUri = $"wss://platform.fintacharts.com/api/streaming/ws/v1/realtime?token={token}";
                    using var ws = new ClientWebSocket();
                    await ws.ConnectAsync(new Uri(wsUri), stoppingToken);

                    foreach (var asset in assets)
                    {
                        if (string.IsNullOrEmpty(asset.InstrumentId) || string.IsNullOrEmpty(asset.Provider))
                            continue;
                        var subscribeMessage = new
                        {
                            type = "l1-subscription",
                            id = asset.InstrumentId,
                            instrumentId = asset.InstrumentId,
                            provider = asset.Provider,
                            subscribe = true,
                            kinds = new[] { "ask", "bid", "last" }
                        };
                        var json = System.Text.Json.JsonSerializer.Serialize(subscribeMessage);
                        var bytes = Encoding.UTF8.GetBytes(json);
                        await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, stoppingToken);
                    }

                    var buffer = new byte[4096];
                    while (ws.State == WebSocketState.Open && !stoppingToken.IsCancellationRequested)
                    {
                        var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), stoppingToken);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by service", stoppingToken);
                            break;
                        }
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        try
                        {
                            using var doc = JsonDocument.Parse(message);
                            var root = doc.RootElement;
                            if (root.TryGetProperty("instrumentId", out var instrumentIdProp) && root.TryGetProperty("last", out var lastProp))
                            {
                                var instrumentId = instrumentIdProp.GetString();
                                var price = lastProp.GetProperty("price").GetDecimal();
                                var updatedAt = lastProp.GetProperty("timestamp").GetDateTime();
                                if (updatedAt.Kind == DateTimeKind.Local)
                                    updatedAt = updatedAt.ToUniversalTime();
                                else if (updatedAt.Kind == DateTimeKind.Unspecified)
                                    updatedAt = DateTime.SpecifyKind(updatedAt, DateTimeKind.Utc);

                                var asset = await db.Assets.FirstOrDefaultAsync(a => a.InstrumentId == instrumentId, stoppingToken);
                                if (asset != null)
                                {
                                    var priceEntity = new Price
                                    {
                                        AssetId = asset.Id,
                                        Value = price,
                                        UpdatedAt = updatedAt
                                    };
                                    db.Prices.Add(priceEntity);
                                    await db.SaveChangesAsync(stoppingToken);
                                    _logger.LogInformation($"Updated price for {asset.Symbol}: {price} at {updatedAt}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"Failed to process WS message: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in FintachartsPriceUpdaterService. Reconnecting in 30s...");
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
            }
        }
    }
} 