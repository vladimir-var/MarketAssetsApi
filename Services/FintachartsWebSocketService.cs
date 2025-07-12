using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace MarketAssetsApi.Services
{
    public class FintachartsWebSocketService
    {
        private readonly IConfiguration _configuration;
        private readonly FintachartsAuthService _authService;

        public FintachartsWebSocketService(IConfiguration configuration, FintachartsAuthService authService)
        {
            _configuration = configuration;
            _authService = authService;
        }

        public async Task ConnectAndSubscribeAsync(string symbol)
        {
            var token = await _authService.GetAccessTokenAsync();
            var wsUri = $"wss://platform.fintacharts.com/api/streaming/ws/v1/realtime?token={token}";
            using var ws = new ClientWebSocket();
            await ws.ConnectAsync(new Uri(wsUri), CancellationToken.None);

            var subscribeMessage = $"{{\"type\":\"subscribe\",\"symbol\":\"{symbol}\"}}";
            var bytes = Encoding.UTF8.GetBytes(subscribeMessage);
            await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);

            var buffer = new byte[4096];
            var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
            Console.WriteLine($"WS message: {message}");
        }
    }
} 