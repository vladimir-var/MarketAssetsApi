using MarketAssetsApi.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Додаємо DbContext з підключенням до PostgreSQL
builder.Services.AddDbContext<MarketAssetsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Додаємо Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<MarketAssetsDbContext>();

builder.Services.AddHttpClient<MarketAssetsApi.Services.FintachartsAuthService>();
builder.Services.AddHttpClient<MarketAssetsApi.Services.FintachartsRestService>();
builder.Services.AddSingleton<MarketAssetsApi.Services.FintachartsWebSocketService>();
builder.Services.AddHostedService<MarketAssetsApi.Services.FintachartsPriceUpdaterService>();
builder.Services.AddHostedService<MarketAssetsApi.Services.FintachartsAssetsSyncService>();
builder.Services.AddHostedService<MarketAssetsApi.Services.FintachartsHistoricalPricesService>();

var app = builder.Build();

// Автоматичне створення бази даних та таблиць
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<MarketAssetsDbContext>();
    context.Database.EnsureCreated();
}

// Явно вказуємо порт 80 для Docker
builder.WebHost.UseUrls("http://*:80");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Додаємо health check endpoint
app.MapHealthChecks("/health");

app.Run();
