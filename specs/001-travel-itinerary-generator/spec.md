# Feature Specification: Travel Itinerary Generator

**Feature Branch**: `001-travel-itinerary-generator`
**Created**: 2026-03-06
**Status**: Draft
**Input**: User description: "A backend service that generates personalized travel itineraries for users based on their selected city, budget and interests. The service integrates multiple external APIs to provide weather forecasts, recommended places to visit, distances between locations, and optionally nearby restaurants or accommodations."

## Clarifications

### Session 2026-03-06

- Q: What authentication model should the system use — anonymous, JWT-based, or hybrid? → A: Authenticated users only — JWT-based auth required for all endpoints; itineraries are tied to user accounts.
- Q: How should the system handle budget currency — single currency, user-selected, or destination-local? → A: User-selected currency — user specifies their currency at request time; all costs returned in that currency using a forex API.
- Q: Should interests be a predefined catalog, free-text, or hybrid? → A: Predefined catalog — system offers a fixed list of interests; user selects from the list.
- Q: What storage mechanism should back saved itineraries and user data? → A: PostgreSQL — open-source relational database with EF Core support.

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Generate a Basic Itinerary (Priority: P1)

A user wants to plan a weekend trip. They provide the destination city, their total budget, trip duration (number of days), and a list of interests (e.g., museums, parks, food). The system generates a day-by-day itinerary with time slots, recommended places, estimated costs, and travel times between locations — all within the user's budget.

**Why this priority**: This is the core value proposition. Without itinerary generation, the service has no purpose. Everything else is supplementary.

**Independent Test**: Can be fully tested by sending a single API request with city, budget, duration, and interests, and verifying the response contains a structured multi-day plan with places, costs, and time slots.

**Acceptance Scenarios**:

1. **Given** a valid city name, a budget of $500, a 3-day trip, and interests ["museums", "parks"], **When** the user requests an itinerary, **Then** the system returns a 3-day plan where each day contains 3–6 activities matching the interests, the total estimated cost does not exceed $500, and each activity includes a name, address, time slot, and estimated cost.
2. **Given** a valid city name and a 1-day trip, **When** the user requests an itinerary, **Then** the system returns a single-day plan with activities ordered to minimize travel distance between locations.
3. **Given** a very low budget (e.g., $20 for 3 days), **When** the user requests an itinerary, **Then** the system returns a plan prioritizing free or low-cost activities (parks, free museums, walking tours) and includes a warning that the budget is very restrictive. A warning is included when budget utilization exceeds 90% of the total budget.
4. **Given** interests that have no matching places in the city, **When** the user requests an itinerary, **Then** the system returns the best available alternatives and notifies the user that some interests could not be matched.

---

### User Story 2 — Weather-Aware Itinerary (Priority: P2)

A user plans a 5-day trip and wants to ensure they are not scheduled for outdoor activities during rain or extreme weather. The system fetches the weather forecast for the destination city and adjusts the daily plan accordingly — placing outdoor activities on clear days and indoor activities on rainy days.

**Why this priority**: Weather integration significantly improves the quality and realism of the itinerary. It is the first "intelligence layer" on top of the basic plan.

**Independent Test**: Can be tested by requesting itineraries for cities with known forecast data (mocked) and verifying that rainy days contain predominantly indoor activities.

**Acceptance Scenarios**:

1. **Given** a 5-day trip where day 3 has a rainy forecast, **When** the user requests an itinerary, **Then** day 3 contains only indoor activities (museums, shopping, indoor dining) and outdoor activities are scheduled on clear days.
2. **Given** extreme heat (>40°C) forecasted for day 2, **When** the user requests an itinerary, **Then** day 2 prioritizes shaded/indoor activities during peak hours (11:00–16:00) and schedules outdoor activities in early morning or evening.
3. **Given** the weather API is unavailable, **When** the user requests an itinerary, **Then** the system generates the itinerary without weather adjustments and includes a notice that weather data was unavailable.
4. **Given** a trip start date more than 16 days in the future, **When** the user requests an itinerary, **Then** the system generates the itinerary without weather adjustments and includes a notice that the trip date is outside the available forecast window.

---

### User Story 3 — Optimized Route Planning (Priority: P2)

A user wants their daily activities arranged in an efficient order so they spend minimal time traveling between locations. The system uses distance/routing data to sequence the day's activities geographically, reducing backtracking and unnecessary transit.

**Why this priority**: Route optimization directly impacts user experience — a poorly routed day wastes time and money. It is tightly coupled with the core itinerary generation.

**Independent Test**: Can be tested by providing a set of locations and verifying the returned order minimizes total travel distance compared to a random ordering.

**Acceptance Scenarios**:

1. **Given** 5 activities planned for a single day, **When** the itinerary is generated, **Then** the activities are ordered so that total travel distance is within 20% of the optimal shortest route.
2. **Given** activities spread across different neighborhoods, **When** the itinerary is generated, **Then** adjacent activities are geographically clustered and travel time between consecutive activities does not exceed 45 minutes (where possible).
3. **Given** the distance API is unavailable, **When** the itinerary is generated, **Then** the system falls back to straight-line distance estimates and notes that routing data was unavailable.

---

### User Story 4 — Restaurant Recommendations (Priority: P3)

A user wants the itinerary to include meal suggestions. For each meal slot (breakfast, lunch, dinner), the system suggests nearby restaurants matching the user's cuisine preferences and remaining budget. This is an optional enrichment — the itinerary MUST still work without it.

**Why this priority**: Dining recommendations add significant value but are not essential. The core itinerary functions without them.

**Independent Test**: Can be tested by requesting an itinerary with restaurant suggestions enabled, and verifying each day includes 1–3 restaurant suggestions with name, cuisine type, price range, and proximity to nearby planned activities.

**Acceptance Scenarios**:

1. **Given** the user enables restaurant suggestions with cuisine preference "Italian", **When** the itinerary is generated, **Then** each day includes at least one Italian restaurant suggestion near the day's activities, with estimated meal cost deducted from the budget.
2. **Given** the user does not enable restaurant suggestions, **When** the itinerary is generated, **Then** no restaurant data is included and the response is unchanged.
3. **Given** the restaurant API is unavailable, **When** the itinerary is generated, **Then** the itinerary is returned without restaurant suggestions and a notice is included.

---

### User Story 5 — Accommodation Suggestions (Priority: P3)

A user wants the system to suggest nearby accommodations (hotels, hostels, guesthouses) within their budget. The system identifies lodging options close to the geographic center of the planned activities and filters by price.

**Why this priority**: Accommodation suggestions are a convenience feature. Many users book lodging independently, so this is optional enrichment.

**Independent Test**: Can be tested by requesting an itinerary with accommodation suggestions enabled and verifying the response includes a notice that the feature is not yet available.

**Acceptance Scenarios**:

1. **Given** the user enables accommodation suggestions with a nightly budget of $100, **When** the itinerary is generated, **Then** the response includes 1–5 accommodation options under $100/night located within 3 km of the daily activity cluster. *(Deferred — returns "not available" notice in MVP)*
2. **Given** the user does not enable accommodation suggestions, **When** the itinerary is generated, **Then** no accommodation data is included.
3. **Given** the accommodation API is unavailable, **When** the itinerary is generated, **Then** the itinerary is returned without accommodation suggestions and a notice is included.

---

### User Story 6 — Save and Retrieve Itineraries (Priority: P3)

An authenticated user wants to save a generated itinerary so they can retrieve it later, share it, or modify it. The system persists the itinerary under the user's account and provides a unique identifier for retrieval. Only the owning user can access their saved itineraries.

**Why this priority**: Persistence is a standard feature but not critical for the core generation engine. The system is useful even as a stateless generator.

**Independent Test**: Can be tested by authenticating, generating an itinerary, saving it, then retrieving it by ID and verifying the data matches and ownership is enforced.

**Acceptance Scenarios**:

1. **Given** a generated itinerary, **When** the user requests to save it, **Then** the system returns a unique itinerary ID and confirms the save.
2. **Given** a valid itinerary ID, **When** the user requests to retrieve it, **Then** the system returns the full itinerary data exactly as it was saved.
3. **Given** an invalid or non-existent itinerary ID, **When** the user requests to retrieve it, **Then** the system returns a clear error message indicating the itinerary was not found.

---

### User Story 7 — Cost Breakdown Report (Priority: P2)

A user wants to see a detailed cost breakdown of their itinerary — per day, per category (activities, meals, transport), and a grand total — so they can understand how their budget is being utilized.

**Why this priority**: Budget visibility builds trust in the system's recommendations and helps users make informed decisions.

**Independent Test**: Can be tested by generating an itinerary and verifying the cost breakdown sums match the grand total and do not exceed the stated budget.

**Acceptance Scenarios**:

1. **Given** a generated itinerary with budget $500, **When** the cost breakdown is requested, **Then** the response includes per-day costs, per-category costs (activities, dining, transport), and a grand total that does not exceed $500.
2. **Given** a budget that is not fully utilized, **When** the cost breakdown is requested, **Then** the remaining budget is clearly shown.
3. **Given** budget utilization exceeds 90% of the total budget, **When** the itinerary is generated, **Then** the response includes a notice warning that the budget is nearly exhausted and suggesting the user consider increasing it or reducing activities.

---

### Edge Cases

- What happens when the user provides an unrecognized or misspelled city name?
  - The system MUST return a helpful error suggesting similar valid city names or asking for clarification.
- What happens when all external APIs fail simultaneously?
  - The system MUST return a meaningful error with a retry suggestion; it MUST NOT return a partial or corrupted itinerary.
- What happens when the user requests a 0-day or negative-duration trip?
  - The system MUST validate input and reject with a clear validation error.
- What happens when the user provides a budget of $0?
  - The system MUST generate an itinerary with only free activities, or return a message explaining that no activities are available within the budget.
- What happens when the trip duration exceeds 14 days?
  - The system MUST reject the request with a message stating the maximum supported duration.
- What happens when the user provides an empty interests list?
  - The system MUST either use a default set of general interests (sightseeing, landmarks) or prompt the user to select at least one interest.
- What happens when the trip start date is more than 16 days in the future?
  - The system MUST still generate the itinerary without weather adjustments and include a notice that weather data is unavailable for the requested dates (Open-Meteo forecast limit). This is not an error condition.
- What happens when budget utilization exceeds 90%?
  - The system MUST include a warning notice in the response that the budget is nearly fully utilized, without blocking itinerary generation.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST accept a destination city name, total budget (numeric), preferred currency code (ISO 4217, e.g., USD, EUR, EGP), trip duration (1–14 days), and a list of interests selected from a predefined catalog as input.
- **FR-002**: The system MUST resolve city names to geographic coordinates using a geocoding service.
- **FR-003**: The system MUST discover places of interest matching the user's selected interests within the destination city.
- **FR-004**: The system MUST generate a day-by-day itinerary with time-slotted activities, each including: place name, address/coordinates, category, estimated cost, and visit duration.
- **FR-005**: The system MUST ensure the total estimated cost of all activities does not exceed the user's stated budget.
- **FR-006**: The system MUST fetch the weather forecast for the destination city for the trip duration and adjust the itinerary so that outdoor activities are scheduled on clear days. If the trip start date is beyond the 16-day forecast window, the system MUST proceed without weather adjustments and include a notice.
- **FR-007**: The system MUST calculate distances between consecutive activities and order them to minimize total daily travel time.
- **FR-008**: The system MUST include estimated travel time and distance between consecutive activities in the itinerary.
- **FR-009**: The system MUST optionally include restaurant recommendations for each meal slot when enabled by the user.
- **FR-010**: The system MUST optionally include accommodation suggestions within the user's nightly budget when enabled by the user. In the MVP, this returns a "not available" notice.
- **FR-011**: The system MUST provide a cost breakdown (per day, per category, grand total) with every generated itinerary.
- **FR-012**: The system MUST allow users to save generated itineraries and retrieve them by a unique identifier.
- **FR-013**: The system MUST validate all input fields and return structured error responses (RFC 7807 Problem Details) for invalid requests.
- **FR-014**: The system MUST gracefully degrade when optional APIs (restaurants, accommodations) are unavailable, still returning the core itinerary with a notice.
- **FR-015**: The system MUST handle required API failures (weather, places, distance) by applying fallback strategies (cached data, default behavior) or returning a clear service-unavailable error.
- **FR-016**: The system MUST expose all functionality through a RESTful API with OpenAPI documentation.
- **FR-017**: The system MUST require JWT-based authentication for all endpoints except: `POST /api/auth/register`, `POST /api/auth/login`, `GET /api/itineraries/interests`, and `GET /api/health`. Users MUST register and log in to generate, save, or retrieve itineraries. Saved itineraries MUST be scoped to the owning user's account.
- **FR-018**: The system MUST convert all cost estimates to the user's selected currency using a forex/exchange-rate API. If the forex API is unavailable, the system MUST return costs in the source currency and include a notice.
- **FR-019**: The system MUST persist user accounts and saved itineraries in a PostgreSQL relational database.

### Key Entities

- **User**: A registered account — unique ID, email, hashed password, registration date, preferences.
- **City**: Represents the travel destination — name, country, coordinates, timezone.
- **Interest**: A user-selected category from a predefined catalog (e.g., museums, parks, food, nightlife, shopping, history, landmarks, adventure, beaches, art). The catalog is maintained by the system and can be expanded over time.
- **Place**: A recommended location — name, address, coordinates, category, estimated cost, visit duration, indoor/outdoor classification, rating.
- **Itinerary**: The complete trip plan — city, budget, selected currency, duration, list of daily plans, cost breakdown, metadata, owning user ID. Persisted as Draft immediately on generation; transitions to Saved on user request.
- **DayPlan**: A single day's schedule — date, weather summary, ordered list of activity slots, daily cost total.
- **ActivitySlot**: A time-bound activity — start time, end time, place, estimated cost, travel time from previous activity.
- **CostBreakdown**: Budget utilization summary — per-day costs, per-category costs (activities, dining, transport), grand total, remaining budget, currency code.
- **Restaurant**: A dining suggestion — name, cuisine type, price range, rating, address, distance from nearest activity.
- **Accommodation**: A lodging option — name, type (hotel/hostel/guesthouse), price per night, rating, address, distance from activity cluster center.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users receive a complete, personalized itinerary within 10 seconds of making a request (warm cache). Cold-cache first-time generation for a new city may take up to 15 seconds due to external API calls.
- **SC-002**: 95% of generated itineraries have a total estimated cost within the user's stated budget.
- **SC-003**: Daily activity routes are within 25% of the optimal shortest path between all scheduled locations.
- **SC-004**: The system handles at least 50 concurrent itinerary generation requests without degradation.
- **SC-005**: When any single optional API is unavailable, the system still returns a valid itinerary with appropriate notices within 15 seconds.
- **SC-006**: 100% of invalid input requests receive a descriptive, structured error response (not a raw server error).
- **SC-007**: Saved itineraries can be retrieved with 100% data fidelity — no data loss or corruption between save and retrieval.
- **SC-008**: Weather-adjusted itineraries schedule 90%+ of outdoor activities on days with favorable weather conditions.
- **SC-009**: The system supports all cities with a population > 100,000 worldwide.
- **SC-010**: API documentation covers all endpoints with request/response examples, making integration possible without support.