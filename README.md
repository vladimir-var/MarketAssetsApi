# MarketAssetsApi

REST API сервіс для отримання інформації про ціни ринкових активів (EUR/USD, GOOG, тощо).

## Опис

Сервіс інтегрується з платформою Fintacharts для отримання real-time та історичних даних про ціни активів. Дані зберігаються в PostgreSQL базі даних.

## API Endpoints

- `GET /api/assets` - отримання списку всіх підтримуваних активів
- `GET /api/prices?assets=EUR/USD,GOOG` - отримання цін для вказаних активів з часом оновлення
- `GET /health` - перевірка стану сервісу та бази даних

## Запуск через Docker

### Вимоги
- Docker
- Docker Compose

### Інструкція запуску

1. Клонуйте репозиторій
2. Перейдіть в директорію проекту:
   ```bash
   cd MarketAssetsApi
   ```

3. Запустіть сервіси:
   ```bash
   docker-compose up -d
   ```

4. API буде доступне за адресою: `http://localhost:7031`
5. Swagger документація: `http://localhost:7031/swagger`
6. Health check: `http://localhost:7031/health`

### Що відбувається при запуску

1. **PostgreSQL** запускається і створює базу даних `marketassetsdb`
2. **API** чекає готовності бази даних
3. **Entity Framework** автоматично створює таблиці `assets` та `prices`
4. **Background Services** починають синхронізацію даних з Fintacharts

### Зупинка
```bash
docker-compose down
```

### Перегляд логів
```bash
docker-compose logs -f api
```

### Вирішення проблем

Якщо виникає помилка збірки:
```bash
# Очистити кеш Docker
docker system prune -a

# Перезапустити збірку
docker-compose build --no-cache
docker-compose up -d
```

## Запуск локально

### Вимоги
- .NET 8 SDK
- PostgreSQL

### Інструкція

1. Створіть базу даних PostgreSQL
2. Виконайте SQL скрипт `create_database.sql` (опціонально - EF Core створить таблиці автоматично)
3. Налаштуйте connection string в `appsettings.json`
4. Запустіть проект:
   ```bash
   cd MarketAssetsApi
   dotnet run
   ```

## Конфігурація

Налаштування Fintacharts API знаходяться в `appsettings.json`:
- AuthUri - URL для автентифікації
- Username/Password - облікові дані
- ClientId - ідентифікатор клієнта 