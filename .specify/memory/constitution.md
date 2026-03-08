<!--
Sync Impact Report
- Version change: 0.0.0 → 1.0.0 (initial ratification)
- Added sections:
  - Core Principles (7 principles)
  - Project Objectives
  - Core Features & Functionality
  - External APIs
  - Potential Extensions & Enhancements
  - Technology Stack & Constraints
  - Development Workflow & Quality Gates
  - Governance
- Templates requiring updates:
  - plan-template.md ✅ No updates needed (generic template, constitution-compatible)
  - spec-template.md ✅ No updates needed (generic template, constitution-compatible)
  - tasks-template.md ✅ No updates needed (generic template, constitution-compatible)
- Follow-up TODOs: None
-->

# Smart Travel Planner Constitution

## Project Objectives

The Smart Travel Planner is a backend service that generates **personalized travel itineraries** for users based on their selected city, budget, and interests. The service integrates multiple external APIs to provide weather forecasts, recommended places to visit, distances between locations, and optionally nearby restaurants or accommodations.

### Primary Objectives

1. **Intelligent Itinerary Generation** — Produce day-by-day travel plans tailored to user preferences (city, budget, interests, trip duration).
2. **Real-Time Data Enrichment** — Integrate live weather forecasts, points-of-interest, distance/routing data, and dining/lodging options from external APIs.
3. **Budget-Aware Planning** — Ensure generated itineraries respect the user's stated budget by estimating costs for activities, meals, and transport.
4. **Scalable & Maintainable Architecture** — Follow clean architecture principles so the service can grow with new data sources, features, and consumers.
5. **Professional API Surface** — Expose a well-documented RESTful API that can be consumed by web, mobile, or third-party clients.

## Core Principles

### I. Clean Architecture (NON-NEGOTIABLE)

The project MUST follow a layered architecture separating concerns into distinct layers:

- **API Layer** — Controllers, middleware, request/response DTOs.
- **Application/Service Layer** — Business logic, itinerary generation, orchestration.
- **Domain Layer** — Core entities, value objects, domain rules.
- **Infrastructure Layer** — External API clients, data access, caching.

Each layer MUST depend only on inner layers (Dependency Inversion). No business logic in controllers; no HTTP concerns in services.

### II. External API Resilience

All external API integrations MUST be wrapped behind abstractions (interfaces) and MUST implement:

- **Retry policies** with exponential back-off.
- **Circuit-breaker patterns** for fault tolerance.
- **Graceful degradation** — if a non-critical API (e.g., restaurants) is unavailable, the itinerary MUST still be generated with available data.
- **Caching** of API responses where appropriate to reduce cost and latency.

### III. Configuration-Driven Design

All API keys, base URLs, rate limits, and feature toggles MUST be stored in configuration (appsettings / environment variables), never hard-coded. Sensitive values MUST use .NET User Secrets in development and secure vault/environment variables in production.

### IV. Validation & Error Handling

- All incoming requests MUST be validated (FluentValidation or Data Annotations).
- The API MUST return consistent, structured error responses (RFC 7807 Problem Details).
- Unhandled exceptions MUST be caught by global middleware and logged — never leaked to the client.

### V. Observability & Logging

- Structured logging (Serilog or equivalent) MUST be used throughout the service.
- Every external API call MUST log request duration, status, and any errors.
- Health-check endpoints MUST be exposed for all critical external dependencies.

### VI. Testing Discipline

- Unit tests MUST cover all service/business-logic classes.
- Integration tests SHOULD cover external API client wrappers using mocked HTTP handlers.
- The itinerary generation algorithm MUST have comprehensive test coverage with various budget/interest combinations.

### VII. Simplicity & YAGNI

- Start with the minimal viable feature set; avoid premature abstraction.
- Every added layer of complexity MUST be justified by a concrete requirement.
- Prefer composition over inheritance; prefer simple DTOs over deep object hierarchies.

## Core Features & Functionality

### Itinerary Generation (MVP)

| Feature | Description |
|---------|-------------|
| **City Selection** | User selects a destination city; the system resolves it to coordinates and metadata. |
| **Budget Input** | User provides a total budget; the engine distributes costs across days and activity types. |
| **Interest Matching** | User selects interests (e.g., museums, parks, food, nightlife); places are filtered and ranked accordingly. |
| **Day-by-Day Plan** | The engine produces a structured itinerary with time slots, locations, estimated costs, and travel times. |
| **Weather Integration** | Each day's plan is annotated with the weather forecast; outdoor activities are deprioritized on rainy days. |
| **Distance & Routing** | The planner orders daily activities to minimize travel time using distance/matrix APIs. |

### Supporting Features

| Feature | Description |
|---------|-------------|
| **Restaurant Suggestions** | Optionally include nearby restaurant recommendations for each meal slot based on cuisine preferences and budget. |
| **Accommodation Lookup** | Optionally suggest hotels/hostels near the planned activity cluster, filtered by budget. |
| **Cost Breakdown** | Provide a per-day and per-category cost summary so the user can see budget utilization. |
| **Multi-Day Support** | Support trips from 1 to 14 days with balanced activity distribution. |
| **Saved Itineraries** | Allow users to save, retrieve, and share generated itineraries. |

## External APIs

| API | Purpose | Required | Provider Examples |
|-----|---------|----------|-------------------|
| **Weather Forecast** | Retrieve multi-day forecast for the destination city | ✅ Yes | OpenWeatherMap, WeatherAPI, Visual Crossing |
| **Places / Points of Interest** | Discover attractions, landmarks, parks, museums matching user interests | ✅ Yes | Foursquare Places, Google Places, OpenTripMap, scraping |
| **Distance / Routing** | Calculate travel times and distances between locations to optimize daily routes | ✅ Yes | OpenRouteService, Google Distance Matrix, MapBox |
| **Restaurants** | Find nearby dining options by cuisine, rating, and price range | ⬜ Optional | Yelp Fusion, Foursquare, Google Places or free apis |
| **Accommodations** | Search for lodging near activity areas within budget | ⬜ Optional | Booking.com API, RapidAPI Hotels, Amadeus, hotels-api |
| **Geocoding** | Resolve city names to coordinates and vice versa | ✅ Yes | OpenCage, Nominatim, Google Geocoding |

## Potential Extensions & Enhancements

### High-Value Extensions

1. **User Authentication & Profiles** — JWT-based auth so users can save preferences, view history, and manage itineraries.
2. **AI-Powered Recommendations** — Use ML or LLM-based scoring to rank places based on aggregated user reviews and sentiment.
3. **Real-Time Flight/Transport Integration** — Suggest flights, trains, or buses to/from the destination city with pricing.
4. **Collaborative Trip Planning** — Allow multiple users to contribute interests and vote on activities within a shared itinerary.
5. **PDF/Calendar Export** — Generate downloadable PDF itineraries or `.ics` calendar files for easy import.

### Professional Enhancements

1. **Rate Limiting & API Key Management** — Protect the service with per-client rate limiting and API key authentication for third-party consumers.
2. **Response Caching with Redis** — Cache popular city itineraries and API responses to reduce latency and external API costs.
3. **Webhook Notifications** — Notify users of weather changes or updated recommendations before their trip.
4. **Admin Dashboard** — A lightweight admin panel (Blazor or Razor Pages) to monitor API usage, view analytics, and manage cached data.
5. **Multi-Language Support** — Localize itinerary descriptions, place names, and UI labels using resource files or a localization API.
6. **Currency Conversion** — Auto-convert budget and cost estimates to the destination country's currency using a forex API.
7. **Accessibility Filters** — Allow users to filter for wheelchair-accessible venues, family-friendly places, or pet-friendly options.
8. **Social Sharing** — Generate shareable links or social-media-friendly summaries of itineraries.
9. **Offline Mode Support** — Provide a downloadable JSON payload so mobile clients can access the itinerary without connectivity.

## Technology Stack & Constraints

| Dimension | Choice |
|-----------|--------|
| **Runtime** | .NET 10 (LTS) |
| **Framework** | ASP.NET Core Web API |
| **Language** | C# LTS |
| **HTTP Client** | `HttpClientFactory` with Polly for resilience |
| **Validation** | FluentValidation |
| **Logging** | Serilog with structured JSON sinks |
| **Serialization** | System.Text.Json |
| **Documentation** | Swagger / Swashbuckle (OpenAPI latest) |
| **Testing** | xUnit + Moq + FluentAssertions |
| **Configuration** | `appsettings.json` + User Secrets + Environment Variables |
| **Caching** | In-memory (IMemoryCache); Redis for production scale (extension) |
| **CI/CD** | GitHub Actions (recommended) |
| **Deployment** | Azure App Service / Docker container (recommended) |

### Constraints

- All API keys MUST be stored outside source control.
- The service MUST respond to itinerary requests within **5 seconds** under normal load with warm caches.
- The service MUST handle at least **50 concurrent itinerary requests** without degradation.
- External API failures MUST NOT result in 500 errors to the client; graceful fallback MUST be provided.

## Development Workflow & Quality Gates

### Workflow

1. **Feature branches** — All work MUST happen on a feature branch off `main`.
2. **Pull requests** — Every merge to `main` MUST go through a pull request with at least one review.
3. **Commit conventions** — Use conventional commits (`feat:`, `fix:`, `docs:`, `refactor:`, `test:`).
4. **Build verification** — All PRs MUST pass `dotnet build` and `dotnet test` before merge.

### Quality Gates

- **Build** — Zero warnings policy (treat warnings as errors in CI).
- **Tests** — All unit and integration tests MUST pass.
- **Code Coverage** — Minimum 70% line coverage on service/business-logic projects.
- **Linting** — Code MUST pass configured analyzers (e.g., StyleCop, .editorconfig rules).
- **API Docs** — Swagger documentation MUST be kept in sync with all endpoint changes.

## Governance

- This constitution is the authoritative reference for all architectural and process decisions in the Smart Travel Planner project.
- Amendments require: (1) a documented proposal, (2) review and approval, and (3) a migration plan for any breaking changes.
- All pull requests and code reviews MUST verify compliance with this constitution.
- Complexity beyond what is described here MUST be justified in writing.
- Version follows **MAJOR.MINOR.PATCH** semantic versioning: MAJOR for principle removals/redefinitions, MINOR for new sections/principles, PATCH for wording/clarification fixes.

**Version**: 1.0.0 | **Ratified**: 2026-03-06 | **Last Amended**: 2026-03-06
