using MarketAssetsApi.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<MarketAssetsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<MarketAssetsDbContext>();
builder.Services.AddHttpClient<MarketAssetsApi.Services.FintachartsAuthService>();
builder.Services.AddHttpClient<MarketAssetsApi.Services.FintachartsRestService>();
builder.Services.AddSingleton<MarketAssetsApi.Services.FintachartsWebSocketService>();
builder.Services.AddHostedService<MarketAssetsApi.Services.FintachartsPriceUpdaterService>();
builder.Services.AddHostedService<MarketAssetsApi.Services.FintachartsAssetsSyncService>();
builder.Services.AddHostedService<MarketAssetsApi.Services.FintachartsHistoricalPricesService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<MarketAssetsDbContext>();
    context.Database.EnsureCreated();
}

builder.WebHost.UseUrls("http://*:80");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");

app.Run();
