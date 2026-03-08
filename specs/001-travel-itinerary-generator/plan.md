# Implementation Plan: Travel Itinerary Generator

**Branch**: `001-travel-itinerary-generator` | **Date**: 2026-03-06 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-travel-itinerary-generator/spec.md`

## Summary

Build a backend service that generates personalized, day-by-day travel itineraries based on city, budget, duration, and interests. The service orchestrates multiple external APIs (weather, places, routing, geocoding, forex) to produce weather-aware, route-optimized, budget-constrained itineraries with optional restaurant suggestions. Built on ASP.NET Core Web API with clean architecture, PostgreSQL persistence, and JWT authentication.

## Technical Context

**Language/Version**: C# / .NET 10 (LTS)
**Primary Dependencies**: ASP.NET Core Web API, Entity Framework Core, FluentValidation, Serilog, Polly (resilience), Swashbuckle (OpenAPI)
**Storage**: PostgreSQL via `Npgsql.EntityFrameworkCore.PostgreSQL`
**Testing**: xUnit + Moq + FluentAssertions
**Target Platform**: Linux/Windows server (Azure App Service / Docker)
**Project Type**: Web Service (RESTful API)
**Performance Goals**: <10s itinerary response (SC-001); 50 concurrent requests (SC-004)
**Constraints**: <10s warm-cache response; external API failures must not cause 500 errors; all API keys outside source control
**Scale/Scope**: Global cities >100k population; 1–14 day trips; authenticated users

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| **I. Clean Architecture** | ✅ PASS | 4-layer structure: Api, Application, Domain, Infrastructure |
| **II. External API Resilience** | ✅ PASS | All API clients behind interfaces; Polly for retry/circuit-breaker; graceful degradation designed |
| **III. Configuration-Driven** | ✅ PASS | All API keys/URLs in `appsettings.json` + User Secrets; no hard-coded values |
| **IV. Validation & Error Handling** | ✅ PASS | FluentValidation for input; RFC 7807 Problem Details for errors; global exception middleware |
| **V. Observability & Logging** | ✅ PASS | Serilog structured logging; API call duration/status logged; health checks for all dependencies |
| **VI. Testing Discipline** | ✅ PASS | xUnit + Moq + FluentAssertions; unit tests for services; integration tests for API clients |
| **VII. Simplicity & YAGNI** | ✅ PASS | 4 projects (not more); accommodation API deferred; no CQRS/MediatR (unnecessary for MVP) |

## Project Structure

### Documentation (this feature)

```text
specs/001-travel-itinerary-generator/
├── plan.md              # This file
├── research.md          # Phase 0 output — API decisions
├── data-model.md        # Phase 1 output — entity design
├── quickstart.md        # Phase 1 output — local setup guide
├── contracts/           # Phase 1 output — API endpoint contracts
│   ├── auth.md
│   ├── itineraries.md
│   └── health.md
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (repository root)

```text
src/
├── SmartTravelPlanner.Api/               # Presentation Layer
│   ├── Controllers/
│   │   ├── AuthController.cs
│   │   ├── ItinerariesController.cs
│   │   └── HealthController.cs
│   ├── Middleware/
│   │   └── GlobalExceptionMiddleware.cs
│   ├── DTOs/
│   │   ├── Requests/
│   │   └── Responses/
│   ├── Extensions/
│   │   └── ServiceCollectionExtensions.cs
│   ├── Program.cs
│   ├── appsettings.json
│   └── SmartTravelPlanner.Api.csproj
│
├── SmartTravelPlanner.Application/       # Application/Service Layer
│   ├── Services/
│   │   ├── ItineraryGenerationService.cs
│   │   ├── WeatherEnrichmentService.cs
│   │   ├── RouteOptimizationService.cs
│   │   ├── CostCalculationService.cs
│   │   ├── AuthService.cs
│   │   └── CurrencyConversionService.cs
│   ├── Validators/
│   │   └── GenerateItineraryRequestValidator.cs
│   ├── Helpers/
│   │   └── HaversineCalculator.cs
│   ├── DTOs/
│   ├── Interfaces/
│   │   ├── IItineraryGenerationService.cs
│   │   ├── IWeatherEnrichmentService.cs
│   │   ├── IRouteOptimizationService.cs
│   │   ├── ICostCalculationService.cs
│   │   ├── ICurrencyConversionService.cs
│   │   └── IAuthService.cs
│   └── SmartTravelPlanner.Application.csproj
│
├── SmartTravelPlanner.Domain/            # Domain Layer
│   ├── Entities/
│   │   ├── User.cs
│   │   ├── Itinerary.cs
│   │   ├── DayPlan.cs
│   │   ├── ActivitySlot.cs
│   │   ├── CostBreakdown.cs
│   │   ├── Interest.cs
│   │   ├── Place.cs
│   │   └── RestaurantSuggestion.cs
│   ├── ValueObjects/
│   │   ├── Coordinates.cs
│   │   ├── Money.cs
│   │   └── TimeSlot.cs
│   ├── Enums/
│   │   ├── InterestCategory.cs
│   │   └── WeatherCondition.cs
│   ├── Interfaces/
│   │   ├── IWeatherClient.cs
│   │   ├── IPlacesClient.cs
│   │   ├── IRoutingClient.cs
│   │   ├── IGeocodingClient.cs
│   │   ├── ICurrencyClient.cs
│   │   ├── IAccommodationClient.cs
│   │   ├── IItineraryRepository.cs
│   │   ├── IUserRepository.cs
│   │   └── IPlacesCacheRepository.cs
│   └── SmartTravelPlanner.Domain.csproj
│
└── SmartTravelPlanner.Infrastructure/    # Infrastructure Layer
    ├── ExternalApis/
    │   ├── OpenMeteo/
    │   │   └── OpenMeteoWeatherClient.cs
    │   ├── OpenTripMap/
    │   │   └── OpenTripMapPlacesClient.cs
    │   ├── OpenRouteService/
    │   │   └── OpenRouteServiceRoutingClient.cs
    │   ├── Nominatim/
    │   │   └── NominatimGeocodingClient.cs
    │   └── Frankfurter/
    │       └── FrankfurterCurrencyClient.cs
    ├── Persistence/
    │   ├── AppDbContext.cs
    │   ├── Repositories/
    │   │   ├── ItineraryRepository.cs
    │   │   ├── UserRepository.cs
    │   │   └── PlacesCacheRepository.cs
    │   └── Migrations/
    ├── Caching/
    │   └── InMemoryCacheService.cs
    ├── Extensions/
    │   └── HttpClientExtensions.cs
    ├── Auth/
    │   └── JwtTokenService.cs
    └── SmartTravelPlanner.Infrastructure.csproj

tests/
├── SmartTravelPlanner.UnitTests/
│   ├── Application/
│   │   ├── ItineraryGenerationServiceTests.cs
│   │   ├── RouteOptimizationServiceTests.cs
│   │   ├── CostCalculationServiceTests.cs
│   │   └── WeatherEnrichmentServiceTests.cs
│   ├── Domain/
│   │   └── EntityTests.cs
│   └── SmartTravelPlanner.UnitTests.csproj
│
└── SmartTravelPlanner.IntegrationTests/
    ├── ExternalApis/
    │   ├── OpenMeteoClientTests.cs
    │   ├── OpenTripMapClientTests.cs
    │   ├── OpenRouteServiceClientTests.cs
    │   ├── NominatimClientTests.cs
    │   └── FrankfurterClientTests.cs
    └── SmartTravelPlanner.IntegrationTests.csproj
```

**Structure Decision**: Clean Architecture with 4 source projects + 2 test projects. Each layer maps 1:1 to a constitution-mandated concern (Presentation, Application, Domain, Infrastructure). The Domain layer has zero outward dependencies. All external API clients implement interfaces defined in Domain.

**Project References** (strictly enforced):
- `Api` → `Application`, `Domain`
- `Application` → `Domain`
- `Infrastructure` → `Domain` *(NOT Application — Clean Architecture boundary)*
- `UnitTests` → `Application`, `Domain`
- `IntegrationTests` → `Infrastructure`, `Domain`

## ItineraryGenerationService — Orchestration Algorithm

The full generation sequence within `ItineraryGenerationService` is:

1. **Geocode** city name → `Coordinates` + `CountryName` (via `IGeocodingClient`)
2. **Fetch weather** forecast for trip duration → `WeatherForecast[]` per day (via `IWeatherClient`; graceful degradation if unavailable)
3. **Fetch places** for all requested interest categories → raw `Place[]` list (via `IPlacesClient`; check `IPlacesCacheRepository` first)
4. **Classify places** as indoor/outdoor based on category and `Place.IsIndoor` flag
5. **Allocate places to days** (3–6 per day), respecting the weather classification: rainy/extreme days (WMO ≥ 51 or temp > 40°C) receive indoor-only places; clear days receive mixed allocation
6. **Optimize routes** per day → reorder each day's place list using nearest-neighbor heuristic (via `IRouteOptimizationService`; Haversine fallback if routing API unavailable)
7. **Assign time slots** — start at 09:00, add `VisitDurationMinutes` + travel time between each activity to compute `StartTime`/`EndTime`
8. **Fetch restaurants** (optional) — if `includeRestaurants`, query `IPlacesClient` with `foods` category near each day's activity centroid; attach to `DayPlan`
9. **Convert costs** — convert all `EstimatedCostLocal` values to user currency via `ICurrencyConversionService`; fallback to source currency + notice if unavailable
10. **Calculate cost breakdown** — sum per-day, per-category (activities / dining / transport); enforce `GrandTotal ≤ TotalBudget` (via `ICostCalculationService`)
11. **Persist as Draft** — save `Itinerary` with `Status = Draft` to database immediately (see Draft Persistence Model below)
12. **Return** assembled `ItineraryResponse`

**US1 build note**: In Phase 3 (US1), steps 2 and 6 are implemented as no-ops (weather → no adjustment, routing → original order). US2 replaces step 2's no-op; US3 replaces step 6's no-op. The service signature and object graph do not change between phases.

## Draft Persistence Model

**Decision**: Persist on generate, clean up stale drafts.

When `POST /api/itineraries/generate` is called, the itinerary is immediately persisted to the database with `Status = Draft`. This ensures the `itineraryId` in the response is always retrievable via `POST /itineraries/{id}/save`, even after restarts or under concurrent load.

**Stale Draft cleanup**: A background cleanup removes Draft itineraries older than 24 hours on startup and periodically (configurable via `Caching:DraftExpirationHours` in appsettings). This prevents unbounded database growth from unsaved drafts.

**Rationale**: In-memory Draft storage would make the `itineraryId` unreliable across restarts and under 50-concurrent-request load (SC-004). Persist-on-generate is the safer default for a stateless API deployment.

## Complexity Tracking

> No constitution violations. The 4-project structure directly satisfies Constitution I without exceeding the complexity threshold of Constitution VII.