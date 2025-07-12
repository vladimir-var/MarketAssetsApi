FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Встановлюємо curl для health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["MarketAssetsApi.csproj", "MarketAssetsApi/"]
RUN dotnet restore "MarketAssetsApi/MarketAssetsApi.csproj"
COPY . .
WORKDIR "/src/MarketAssetsApi"
RUN dotnet build "MarketAssetsApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MarketAssetsApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MarketAssetsApi.dll"] 