# 🌍 Smart Travel Planner

**Smart Travel Planner** is a high-performance backend service designed to generate personalized, weather-aware, and route-optimized travel itineraries. Built with .NET 10 and C#, it leverages multiple external APIs to create realistic multi-day plans tailored to user interests and budget constraints.

![Project Banner](https://img.shields.io/badge/.NET-10.0-blue.svg) ![PostgreSQL](https://img.shields.io/badge/Database-PostgreSQL-336791.svg) ![Clean Architecture](https://img.shields.io/badge/Architecture-Clean-brightgreen.svg)

---

## ✨ Key Features

- 🗺️ **Personalized Itineraries**: Generate 1–14 day plans based on target city, budget, and a catalog of interests.
- 🌦️ **Weather-Aware Planning**: Intelligence engine that schedules outdoor activities on clear days and prioritizes indoor locations during rain or extreme heat.
- 🏎️ **Route Optimization**: Uses geographic data to sequence activities efficiently, minimizing travel time and distance.
- 💰 **Budget Management**: Integrated cost estimation with real-time currency conversion for global travel.
- 🍽️ **Dining Suggestions**: Optional curated restaurant recommendations integrated into the daily schedule.
- 🔒 **Secure User Accounts**: JWT-based authentication for saving, retrieving, and managing private itineraries.
- 📉 **Cost Breakdown**: Detailed reporting of spending per category (activities, dining, transport) and per day.

---

## 🚀 Tech Stack

- **Core**: .NET 10 (LTS) / ASP.NET Core Web API
- **Architecture**: Domain-Driven Design (DDD) & Clean Architecture
- **Database**: PostgreSQL with Entity Framework Core
- **External API Integrations**:
  - **OpenTripMap**: Discovery of places and landmarks.
  - **Open-Meteo**: High-resolution weather forecasting.
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
