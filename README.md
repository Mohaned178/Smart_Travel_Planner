# 🌍 Smart Travel Planner

**Smart Travel Planner** is a high-performance backend service designed to generate personalized, weather-aware, and route-optimized travel itineraries. Built with .NET 10 and C#, it leverages multiple external APIs to create realistic multi-day plans tailored to user interests and budget constraints.

![Project Banner](https://img.shields.io/badge/.NET-10.0-blue.svg) ![PostgreSQL](https://img.shields.io/badge/Database-PostgreSQL-336791.svg) ![Clean Architecture](https://img.shields.io/badge/Architecture-Clean-brightgreen.svg)

---

## ✨ Key Features

- 🗺️ **Personalized Itineraries**: Generate 1–14 day plans based on target city, budget, and an expanded catalog of interests.
- 🌦️ **Weather-Aware Planning**: Intelligence engine that schedules outdoor activities on clear days and prioritizes indoor locations (museums, cafes, galleries) during rainy weather.
- 🏎️ **Route Optimization**: Uses geographic data and distance matrices to sequence activities efficiently, minimizing travel time.
- 💰 **Budget Management**: Integrated cost estimation that automatically enforces budget limits by prioritizing higher-rated activities and adjusting dining tiers.
- 🍽️ **Dining & Activity Deduplication**: Smart logic that ensures a location is never suggested as both an activity and a restaurant in the same plan.
- 📍 **Intelligent Filtering**: Automatically excludes forbidden categories (bars, gas stations, transit) and enforces a 5km geographic radius from the city center.
- 📈 **Guaranteed Activity Density**: Ensure at least 3 high-quality activities per day through automated "top-up" logic if primary interests are limited.

---

## 🧩 Itinerary Generation Details

The generation engine uses advanced logic to ensure realistic and high-quality plans:

- **Interest Mapping**: Maps human-friendly interests (e.g., `history`, `nature`, `art`) to specific Google Place Types (e.g., `tourist_attraction`, `park`, `art_gallery`).
- **Realistic Durations**: Assigns visit times based on category (e.g., 150m for Museums, 90m for Landmarks).
- **Schedule Recalculation**: Automatically adjusts activity start and end times after every filtering step (budget, weather, proximity).
- **Supported Interests**: `museums`, `parks`, `food`, `nightlife`, `shopping`, `history`, `landmarks`, `adventure`, `beaches`, `art`, `nature`.

---

## 🚀 Tech Stack

- **Core**: .NET 10 (LTS) / ASP.NET Core Web API
- **Architecture**: Domain-Driven Design (DDD) & Clean Architecture
- **Database**: PostgreSQL with Entity Framework Core
- **External API Integrations**:
  - **Google Places API**: Discovery of places, landmarks, and dining.
  - **OpenWeather API**: High-resolution weather forecasting.
  - **Nominatim (OSM)**: Geocoding and location resolution.
  - **OpenRouteService**: Distance and travel time calculations.
  - **Frankfurter**: Real-time currency exchange rates.
- **Tools & Libraries**:
  - **FluentValidation**: Robust input validation.
  - **Serilog**: Structured logging and observability.
  - **Polly**: API resilience and circuit breaker patterns.
  - **xUnit / Moq / FluentAssertions**: Comprehensive testing suite.

---

## 📂 Project Structure

The solution follows **Clean Architecture** principles to ensure maintainability and testability:

```text
src/
├── SmartTravelPlanner.Api            # Presentation Layer (Controllers, Middleware, DTOs)
├── SmartTravelPlanner.Application    # Application Logic (Business Services, Validators)
├── SmartTravelPlanner.Domain         # Domain Layer (Entities, Value Objects, Interfaces)
└── SmartTravelPlanner.Infrastructure # Infrastructure (Persistence, External API Clients)
tests/
├── SmartTravelPlanner.UnitTests      # Logic & Service Testing
└── SmartTravelPlanner.IntegrationTests # API & Client Testing
```

---

## 🛠️ Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [PostgreSQL](https://www.postgresql.org/download/)

### Installation & Setup

1. **Clone the Repository**

   ```bash
   git clone https://github.com/Mohaned178/Smart_Travel_Planner.git
   cd Smart_Travel_Planner
   ```

2. **Configure Environment**
   Update the connection string and API keys in `src/SmartTravelPlanner.Api/appsettings.json` or use .NET User Secrets:

   ```bash
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=SmartPlanner;Username=postgres;Password=your_password"
   ```

3. **Database Migration**
   Apply migrations to set up the PostgreSQL schema:

   ```bash
   dotnet ef database update --project src/SmartTravelPlanner.Infrastructure --startup-project src/SmartTravelPlanner.Api
   ```

4. **Run the Application**

   ```bash
   dotnet run --project src/SmartTravelPlanner.Api
   ```

   The API will be available at `http://localhost:5030` (or as configured in `launchSettings.json`).

---

## 📖 API Documentation

Once the application is running, you can explore the API using Swagger UI at:
`http://localhost:5030/swagger/index.html`

### Key Endpoints

- `POST /api/auth/register` - Create a new user account.
- `POST /api/auth/login` - Authenticate and receive a JWT token.
- `POST /api/itineraries/generate` - Generate a new travel plan.
- `GET /api/itineraries/{id}` - Retrieve a saved itinerary.
- `GET /api/health` - Check system and dependency status.
- `GET /api/itineraries/history` - Retrieve a user's recent generation history.

#### Example Generation Request

```json
{
  "cityName": "Paris",
  "totalBudget": 1500.0,
  "currencyCode": "EUR",
  "durationDays": 2,
  "interests": ["museums", "history", "nature"],
  "includeRestaurants": true,
  "tripStartDate": "2026-06-20T09:00:00Z"
}
```

---

## 🧪 Running Tests

```bash
# Run all tests
dotnet test

# Run unit tests only
dotnet test tests/SmartTravelPlanner.UnitTests

# Run integration tests
dotnet test tests/SmartTravelPlanner.IntegrationTests
```

---

## 📜 License

This project is licensed under the MIT License - see the LICENSE file for details.
