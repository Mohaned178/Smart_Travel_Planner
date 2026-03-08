# Quickstart: Travel Itinerary Generator

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [PostgreSQL 16+](https://www.postgresql.org/download/) (or Docker)
- OpenTripMap API key (free: [opentripmap.com](https://opentripmap.com))
- OpenRouteService API key (free: [openrouteservice.org](https://openrouteservice.org))

> **Note**: Open-Meteo, Nominatim, and Frankfurter do **not** require API keys.

---

## 1. Clone & Checkout

```bash
git clone <repository-url>
cd Smart-Travel-Planner
git checkout 001-travel-itinerary-generator
```

## 2. Restore Dependencies

```bash
dotnet restore
```

## 3. Configure Secrets

```bash
cd src/SmartTravelPlanner.Api

# Initialize user secrets
dotnet user-secrets init

# Set required secrets
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=smart_travel_planner;Username=postgres;Password=YOUR_PASSWORD"
dotnet user-secrets set "ExternalApis:OpenTripMap:ApiKey" "YOUR_OPENTRIPMAP_KEY"
dotnet user-secrets set "ExternalApis:OpenRouteService:ApiKey" "YOUR_ORS_KEY"
dotnet user-secrets set "Jwt:Secret" "YOUR_JWT_SECRET_MIN_32_CHARS_LONG"
```

## 4. Set Up Database

```bash
# Option A: Local PostgreSQL
# Create the database first, then apply migrations:
cd src/SmartTravelPlanner.Api
dotnet ef database update --project ../SmartTravelPlanner.Infrastructure --startup-project .

# Option B: Docker PostgreSQL
docker run -d --name travel-planner-db \
  -e POSTGRES_DB=smart_travel_planner \
  -e POSTGRES_PASSWORD=YOUR_PASSWORD \
  -p 5432:5432 \
  postgres:16

# Then apply migrations:
dotnet ef database update --project ../SmartTravelPlanner.Infrastructure --startup-project .
```

> **Note**: Always specify `--startup-project .` when running EF commands from the Api directory. The Infrastructure project contains the DbContext but requires the Api project for configuration resolution in the multi-project structure.

## 5. Run the API

```bash
cd src/SmartTravelPlanner.Api
dotnet run
```

The API will start at `https://localhost:5001` (HTTPS) or `http://localhost:5000` (HTTP).

Interest seed data (the 10 predefined interest categories) is automatically applied on startup.

## 6. Explore the API

Open Swagger UI at: `https://localhost:5001/swagger`

### Quick test flow

```bash
# 1. Register
curl -X POST https://localhost:5001/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test@1234","displayName":"Test User"}'

# 2. Login
curl -X POST https://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test@1234"}'
# → Copy the accessToken from response

# 3. Generate itinerary
curl -X POST https://localhost:5001/api/itineraries/generate \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "cityName": "Paris",
    "totalBudget": 500,
    "currencyCode": "USD",
    "durationDays": 3,
    "tripStartDate": "2026-04-10",
    "interests": ["museums", "parks", "food"],
    "includeRestaurants": true
  }'
# → Response will have status "Draft" and an itineraryId

# 4. Save the itinerary
curl -X POST https://localhost:5001/api/itineraries/{itineraryId}/save \
  -H "Authorization: Bearer YOUR_TOKEN"

# 5. Retrieve saved itinerary
curl https://localhost:5001/api/itineraries/{itineraryId} \
  -H "Authorization: Bearer YOUR_TOKEN"

# 6. Check health
curl https://localhost:5001/api/health
```

## 7. Run Tests

```bash
# Unit tests
dotnet test tests/SmartTravelPlanner.UnitTests

# Integration tests
dotnet test tests/SmartTravelPlanner.IntegrationTests

# All tests
dotnet test
```

---

## Configuration Reference

### `appsettings.json` Structure

```json
{
  "ExternalApis": {
    "OpenMeteo": {
      "BaseUrl": "https://api.open-meteo.com/v1"
    },
    "OpenTripMap": {
      "BaseUrl": "https://api.opentripmap.com/0.1/en/places",
      "ApiKey": "SET_VIA_USER_SECRETS"
    },
    "OpenRouteService": {
      "BaseUrl": "https://api.openrouteservice.org",
      "ApiKey": "SET_VIA_USER_SECRETS",
      "Profile": "driving-car"
    },
    "Nominatim": {
      "BaseUrl": "https://nominatim.openstreetmap.org",
      "UserAgent": "SmartTravelPlanner/1.0"
    },
    "Frankfurter": {
      "BaseUrl": "https://api.frankfurter.dev"
    }
  },
  "Jwt": {
    "Secret": "SET_VIA_USER_SECRETS",
    "Issuer": "SmartTravelPlanner",
    "Audience": "SmartTravelPlannerClients",
    "ExpirationMinutes": 60
  },
  "ConnectionStrings": {
    "DefaultConnection": "SET_VIA_USER_SECRETS"
  },
  "Caching": {
    "PlaceCacheExpirationDays": 7,
    "WeatherCacheExpirationHours": 6,
    "ExchangeRateCacheExpirationHours": 24,
    "DraftExpirationHours": 24
  },
  "TransportRates": {
    "CostPerKm": 0.15,
    "MealCostEstimates": {
      "Budget": 8.00,
      "Mid": 20.00,
      "Upscale": 45.00
    }
  }
}
```

### Configuration notes

| Key | Purpose | Where to set |
|-----|---------|-------------|
| `ConnectionStrings:DefaultConnection` | PostgreSQL connection string | User Secrets (dev), env var (prod) |
| `ExternalApis:OpenTripMap:ApiKey` | OpenTripMap API key | User Secrets (dev), env var (prod) |
| `ExternalApis:OpenRouteService:ApiKey` | OpenRouteService API key | User Secrets (dev), env var (prod) |
| `Jwt:Secret` | JWT signing key (min 32 chars) | User Secrets (dev), env var (prod) |
| `TransportRates:CostPerKm` | Per-km rate for transport cost estimates | `appsettings.json` |
| `TransportRates:MealCostEstimates` | Meal cost estimates by tier | `appsettings.json` |
| `Caching:DraftExpirationHours` | How long Draft itineraries are kept before cleanup | `appsettings.json` |