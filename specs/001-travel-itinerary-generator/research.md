# Research: Travel Itinerary Generator

**Feature Branch**: `001-travel-itinerary-generator`
**Date**: 2026-03-06

---

## 1. Weather Forecast API

**Decision**: Open-Meteo

**Rationale**: Completely free and open-source, no API key required, no usage caps. Provides hourly and daily forecasts up to 16 days. High-resolution data from multiple national weather services. Supports global coverage, which aligns with SC-009 (cities > 100k population). JSON response format integrates easily with `System.Text.Json`.

**Forecast range note**: Open-Meteo supports forecasts up to 16 days. For trips with `tripStartDate` more than 16 days in the future, weather data will be unavailable; the system falls back to generating the itinerary without weather adjustments and includes a notice. This is a known limitation documented in the generate endpoint contract.

**Alternatives considered**:

| API | Pros | Cons | Why rejected |
|-----|------|------|--------------|
| OpenWeatherMap | Mature, large community | Free tier requires credit card for One Call 3.0; 1,000 calls/day cap | Credit card requirement + daily cap creates friction |
| WeatherAPI.com | Good free tier (3-day hourly) | Limited free forecast range; less documentation | 3-day forecast insufficient for 14-day trips |
| Visual Crossing | 1,000 records/day free, 50yr history | Daily cap; commercial use requires attribution | History not needed; daily cap could restrict usage |

**Integration notes**:

- Base URL: `https://api.open-meteo.com/v1/forecast`
- Parameters: `latitude`, `longitude`, `daily` (weather_code, temperature_2m_max/min, precipitation_sum)
- No authentication required — simplifies configuration
- Rate limit: fair-use policy (no hard cap); cache responses per city+date for 6 hours to minimize calls
- Fallback: return itinerary without weather adjustments + notice (per FR-015)

---

## 2. Places / Points of Interest API

**Decision**: OpenTripMap

**Rationale**: Tourism-focused POI database with 10M+ attractions globally. Built on OpenStreetMap + Wikidata + Wikipedia. Generous free tier; allows caching and storing data. Categories map well to the predefined interest catalog (museums, parks, landmarks, etc.). Free for commercial use.

**Rate limit note**: OpenTripMap's free tier is rate-limited to approximately 1 request/second. For a multi-day itinerary with multiple interest categories, 10–20 sequential API calls are possible. To stay within SC-001 (<10s), place results are fetched by category in a single radius call where possible, and cached aggressively in `IPlacesCacheRepository` (7-day TTL). Cold-cache first-time generation for a new city with many categories may approach the 10s limit; this is acknowledged as an acceptable cold-start trade-off.

**Alternatives considered**:

| API | Pros | Cons | Why rejected |
|-----|------|------|--------------|
| Foursquare Places | 100M+ POIs, rich UGC data | Free tier limited to 10k calls; pay-as-you-go after | Cost concern for a portfolio project |
| Google Places | Extremely rich data | Expensive; $200/month credit only | Budget constraint; not truly free |

**Integration notes**:

- Base URL: `https://api.opentripmap.com/0.1/en/places`
- Endpoints: `/radius` (find places near coords), `/xid/{id}` (place details)
- API key required (free registration)
- Category mapping: `cultural`, `natural`, `sport`, `amusements`, `accomodations`, `shops`, `foods`
- Rate limit: ~1 req/sec; use `SemaphoreSlim(1,1)` throttle in client + aggressive place caching
- Fallback: if unavailable, return service-unavailable error (required API per FR-003)

---

## 3. Distance / Routing API

**Decision**: OpenRouteService

**Rationale**: Free hosted tier with 500 matrix requests/day and 2,000 directions requests/day. Supports distance matrix (up to 3,500 locations per request) which is essential for route optimization. Built on OpenStreetMap data. Well-documented API with .NET-friendly JSON responses.

**Alternatives considered**:

| API | Pros | Cons | Why rejected |
|-----|------|------|--------------|
| OSRM | Unlimited (self-hosted), very fast | Requires self-hosting infrastructure | Adds deployment complexity; YAGNI |
| Google Distance Matrix | Accurate, real-time traffic | Expensive; $200/month credit only | Cost; overkill for initial version |
| MapBox | Good free tier | 100k requests/month free then paid | More complex auth; ORS sufficient |

**Integration notes**:

- Base URL: `https://api.openrouteservice.org`
- Endpoints: `/v2/matrix/{profile}` (distance matrix), `/v2/directions/{profile}` (directions)
- API key required (free registration)
- Profile: `driving-car` or `foot-walking` (configurable via `ExternalApis:OpenRouteService:Profile`)
- Rate limit: 40 req/min, 500 matrix req/day (free tier)
- Fallback: straight-line (Haversine) distance estimation via `HaversineCalculator` in Application layer (per FR-015, US3 acceptance scenario 3)

---

## 4. Geocoding API

**Decision**: Nominatim (OpenStreetMap)

**Rationale**: Free, open-source, no API key required. Dedicated `Nominatim.API` NuGet package for .NET. Global coverage. Returns structured address data + coordinates. Excellent for resolving city names to lat/lon (FR-002).

**Alternatives considered**:

| API | Pros | Cons | Why rejected |
|-----|------|------|--------------|
| OpenCage | 2,500 req/day free; allows caching | Free tier is for testing only; production requires paid | Not truly free for production |
| Google Geocoding | Most accurate | Expensive | Cost |

**Integration notes**:

- Base URL: `https://nominatim.openstreetmap.org`
- Endpoints: `/search` (forward geocoding), `/reverse` (reverse geocoding)
- Must include `User-Agent` header with app name (configured via `ExternalApis:Nominatim:UserAgent`)
- Rate limit: 1 request/second (public instance)
- Cache results aggressively — city geocoding data is effectively static
- Use `Nominatim.API` NuGet package for typed .NET integration

---

## 5. Currency Exchange Rate API

**Decision**: Frankfurter

**Rationale**: Completely free, open-source, no API key required, no usage caps. Sourced from European Central Bank. Supports 30+ currencies. Updated daily. Has been operational for 10+ years. Perfect for converting costs to user's selected currency (FR-018).

**Alternatives considered**:

| API | Pros | Cons | Why rejected |
|-----|------|------|--------------|
| ExchangeRate-API | 1,500 req/month free | Requires API key; attribution mandatory | Usage cap + attribution adds complexity |
| Fixer | 100 req/month free | Very limited free tier | Insufficient for any real usage |
| Open Exchange Rates | 1,000 req/month free | Free tier limited to USD base | Base currency constraint doesn't match user-selected currency |

**Integration notes**:

- Base URL: `https://api.frankfurter.dev`
- Endpoints: `/latest?from={base}&to={target}` (current rates), `/currencies` (list)
- No authentication required
- Updated daily ~16:00 CET — cache exchange rates for 24 hours
- Fallback: return costs in source currency + notice (per FR-018)

---

## 6. Restaurant API (Optional)

**Decision**: OpenTripMap (reuse Places API with food category filter)

**Rationale**: OpenTripMap already provides a `foods` category that includes restaurants. Reusing the same API avoids adding a new external dependency. The data includes name, coordinates, and categories. While less rich than Yelp/Foursquare for restaurant-specific data (no menus, no price ranges), it satisfies the basic requirement (FR-009) and keeps the system simpler per YAGNI (Constitution VII).

**Limitation**: OpenTripMap does not provide price range data for restaurants. Meal cost estimates are approximated using a configurable city cost-of-living tier (stored as `TransportAndMealRates` in appsettings) rather than actual venue pricing. This is noted as a known limitation in the API response notices.

**Alternatives considered**:

| API | Pros | Cons | Why rejected |
|-----|------|------|--------------|
| Yelp Fusion | Rich data (reviews, prices, menus) | 5,000 req/day free; US-centric | Not global enough; adds dependency |
| Foursquare | Global restaurant data | Pay-as-you-go | Cost |

**Integration notes**:

- Same OpenTripMap client, filter by `foods` category
- Fallback: itinerary returned without restaurant suggestions + notice (per FR-014)

---

## 7. Accommodation API (Optional)

**Decision**: Defer to future phase

**Rationale**: No reliable free accommodation API exists that provides pricing data. Booking.com API requires partnership approval. Amadeus has a free tier but limited. Per Constitution VII (YAGNI), this optional feature (FR-010) can be implemented later when a suitable API is identified. The itinerary MUST still work without it (FR-014).

**Alternatives considered**:

| API | Pros | Cons | Why rejected |
|-----|------|------|--------------|
| Amadeus Hotel | Official travel API | Free tier very limited; complex auth | Over-engineered for optional feature |
| RapidAPI Hotels | Aggregated data | Unreliable; many wrappers deprecated | Stability concern |

**Integration notes**:

- FR-010 will return a "not available" notice when accommodation suggestions are requested
- `IAccommodationClient` interface defined in Domain so a future implementation can be plugged in without breaking changes

---

## 8. Authentication (JWT)

**Decision**: ASP.NET Core Identity + JWT Bearer tokens (no refresh tokens in MVP)

**Rationale**: Built into the framework; no external dependency. Integrates with EF Core and PostgreSQL. Provides user registration, login, password hashing, and role management out of the box. JWT Bearer authentication is the standard for stateless API auth (FR-017).

**Refresh token decision**: Refresh tokens are deferred from MVP. Tokens expire after 60 minutes (`Jwt:ExpirationMinutes`). Users re-authenticate on expiry. A refresh endpoint can be added in a future iteration without breaking the existing auth flow.

**User entity decision**: `User` extends `IdentityUser<Guid>` directly. The custom fields `DisplayName`, `CreatedAt`, and `LastLoginAt` are added as properties on the derived class. `AppDbContext` extends `IdentityDbContext<User, IdentityRole<Guid>, Guid>`. This avoids duplicating Identity's built-in fields (NormalizedEmail, SecurityStamp, etc.) while keeping the data model clean.

**Integration notes**:

- Use `Microsoft.AspNetCore.Identity.EntityFrameworkCore` for user store
- Use `Microsoft.AspNetCore.Authentication.JwtBearer` for token validation
- Token generation: custom `JwtTokenService` using `System.IdentityModel.Tokens.Jwt`
- Configuration: JWT secret, issuer, audience, expiration in `appsettings.json` / User Secrets
- Endpoints: `/api/auth/register`, `/api/auth/login` (refresh deferred)

---

## 9. Database & ORM

**Decision**: PostgreSQL + Entity Framework Core

**Rationale**: Specified in clarifications (Q5). EF Core is the standard ORM for .NET with excellent PostgreSQL support via `Npgsql.EntityFrameworkCore.PostgreSQL`. Code-first migrations simplify schema evolution.

**Migration strategy**: Migrations are applied at startup via `context.Database.Migrate()` in `Program.cs` for development and Docker deployments. For production, migrations are applied as a pre-deploy step via `dotnet ef database update`. `IDesignTimeDbContextFactory` is implemented to support EF tooling in the multi-project structure.

**Integration notes**:

- Use `Npgsql.EntityFrameworkCore.PostgreSQL` NuGet package
- Connection string in User Secrets (dev) / environment variables (prod)
- Code-first migrations with `dotnet ef migrations add` / `dotnet ef database update`
- Use `IDesignTimeDbContextFactory` in Infrastructure project for migration tooling

---

## 10. Project Structure

**Decision**: Clean Architecture with 4 .NET projects

**Rationale**: Aligns with Constitution I (Clean Architecture — NON-NEGOTIABLE). Four projects map to the four architectural layers specified in the constitution.

| Project | Layer | Purpose |
|---------|-------|---------|
| `SmartTravelPlanner.Api` | Presentation | Controllers, middleware, request/response DTOs, DI setup |
| `SmartTravelPlanner.Application` | Application | Services, itinerary generation logic, validators, helpers, DTOs |
| `SmartTravelPlanner.Domain` | Domain | Entities, value objects, interfaces, domain rules |
| `SmartTravelPlanner.Infrastructure` | Infrastructure | API clients, EF Core DbContext, repositories, caching |

**Project references** (strictly enforced — Clean Architecture):

| From | To | Reason |
|------|----|--------|
| Api | Application, Domain | Controllers call Application services; DTOs map to Domain entities |
| Application | Domain | Services implement Domain interfaces |
| Infrastructure | Domain | Clients/repositories implement Domain interfaces |
| Infrastructure | ~~Application~~ | **NOT allowed** — Infrastructure must not depend on Application |
| UnitTests | Application, Domain | Test Application services and Domain logic |
| IntegrationTests | Infrastructure, Domain | Test Infrastructure clients against real/mocked HTTP |

**Testing projects**:

| Project | Scope |
|---------|-------|
| `SmartTravelPlanner.UnitTests` | Domain + Application layer unit tests |
| `SmartTravelPlanner.IntegrationTests` | Infrastructure layer integration tests (mocked HTTP via RichardSzalay.MockHttp) |

**Why not more/fewer projects?**

- Fewer (2): Would violate Constitution I — no clear layer separation
- More (6+): Would violate Constitution VII (YAGNI) — unnecessary granularity
- 4 layers + 2 test projects is the sweet spot