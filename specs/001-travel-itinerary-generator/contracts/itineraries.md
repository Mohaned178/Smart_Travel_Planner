# API Contract: Itineraries

**Base path**: `/api/itineraries`
**Authentication**: JWT Bearer token required on all endpoints unless marked Public

---

## POST `/api/itineraries/generate`

Generate a personalized travel itinerary. The itinerary is immediately persisted as `Draft` and the `itineraryId` can be used to save it later.

### Request

```json
{
  "cityName": "Paris",
  "totalBudget": 500.00,
  "currencyCode": "USD",
  "durationDays": 3,
  "tripStartDate": "2026-04-10",
  "interests": ["museums", "parks", "food"],
  "includeRestaurants": true,
  "includeAccommodations": false,
  "cuisinePreferences": ["Italian", "French"]
}
```

| Field | Type | Required | Validation |
|-------|------|----------|------------|
| `cityName` | string | ✅ | Max 200 chars; must resolve via geocoding |
| `totalBudget` | decimal | ✅ | > 0 |
| `currencyCode` | string | ✅ | Valid ISO 4217 code (3 uppercase letters) |
| `durationDays` | int | ✅ | 1–14 inclusive |
| `tripStartDate` | date | ✅ | Today or future date |
| `interests` | string[] | ✅ | Min 1 item; each must match predefined catalog |
| `includeRestaurants` | bool | ❌ | Default: `false` |
| `includeAccommodations` | bool | ❌ | Default: `false`. Always returns a notice — accommodation API is deferred |
| `cuisinePreferences` | string[] | ❌ | Only applied if `includeRestaurants` is `true` |

**Weather note**: If `tripStartDate` is more than 16 days in the future, weather data will be unavailable (Open-Meteo forecast limit). The itinerary is still generated without weather adjustments and a notice is included in the response.

### Response — 200 OK

```json
{
  "itineraryId": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
  "cityName": "Paris",
  "countryName": "France",
  "coordinates": { "latitude": 48.8566, "longitude": 2.3522 },
  "totalBudget": 500.00,
  "currencyCode": "USD",
  "durationDays": 3,
  "tripStartDate": "2026-04-10",
  "dayPlans": [
    {
      "dayNumber": 1,
      "date": "2026-04-10",
      "weather": {
        "summary": "Partly cloudy",
        "weatherCode": 2,
        "maxTemperatureC": 18.5,
        "minTemperatureC": 9.2,
        "precipitationMm": 0.0
      },
      "activities": [
        {
          "orderIndex": 1,
          "startTime": "09:00",
          "endTime": "11:00",
          "placeName": "Louvre Museum",
          "placeAddress": "Rue de Rivoli, 75001 Paris",
          "coordinates": { "latitude": 48.8606, "longitude": 2.3376 },
          "category": "museums",
          "isIndoor": true,
          "estimatedCost": 17.00,
          "visitDurationMinutes": 120,
          "travelTimeFromPrevMinutes": null,
          "travelDistanceFromPrevKm": null
        },
        {
          "orderIndex": 2,
          "startTime": "11:30",
          "endTime": "13:00",
          "placeName": "Jardin des Tuileries",
          "placeAddress": "Place de la Concorde, 75001 Paris",
          "coordinates": { "latitude": 48.8634, "longitude": 2.3275 },
          "category": "parks",
          "isIndoor": false,
          "estimatedCost": 0.00,
          "visitDurationMinutes": 90,
          "travelTimeFromPrevMinutes": 8.5,
          "travelDistanceFromPrevKm": 0.7
        }
      ],
      "restaurants": [
        {
          "mealSlot": "lunch",
          "name": "Le Petit Cler",
          "cuisineType": "French",
          "coordinates": { "latitude": 48.8580, "longitude": 2.3050 },
          "distanceFromActivityKm": 0.4,
          "estimatedMealCost": 25.00
        }
      ],
      "dailyCostTotal": 42.00
    }
  ],
  "costBreakdown": {
    "totalActivitiesCost": 180.00,
    "totalDiningCost": 120.00,
    "totalTransportCost": 30.00,
    "grandTotal": 330.00,
    "remainingBudget": 170.00,
    "currencyCode": "USD"
  },
  "notices": [
    "Accommodation suggestions are not yet available."
  ],
  "status": "Draft",
  "createdAt": "2026-03-06T17:32:00Z"
}
```

**Draft lifecycle**: The returned `itineraryId` is always retrievable via `POST /{id}/save` or `GET /{id}`. Draft itineraries are automatically purged after 24 hours if not saved (configurable via `Caching:DraftExpirationHours`).

### Error Responses

| Status | Condition |
|--------|-----------|
| 400 | Validation error (RFC 7807) |
| 401 | Missing or invalid JWT |
| 404 | City not found (geocoding failed) |
| 503 | All required APIs unavailable |

---

## POST `/api/itineraries/{id}/save`

Save a Draft itinerary to the user's account, transitioning its status to `Saved`.

### Path Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `id` | Guid | Itinerary ID from the generate response |

### Response — 200 OK

```json
{
  "itineraryId": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
  "status": "Saved",
  "message": "Itinerary saved successfully."
}
```

### Error Responses

| Status | Condition |
|--------|-----------|
| 401 | Missing or invalid JWT |
| 403 | Itinerary belongs to another user |
| 404 | Itinerary not found or Draft has expired (>24h) |

---

## GET `/api/itineraries`

List all **saved** itineraries for the authenticated user. Draft itineraries are not included in this list.

### Query Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `page` | int | ❌ | 1 | Page number |
| `pageSize` | int | ❌ | 10 | Items per page (max 50) |

### Response — 200 OK

```json
{
  "items": [
    {
      "itineraryId": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
      "cityName": "Paris",
      "durationDays": 3,
      "totalBudget": 500.00,
      "currencyCode": "USD",
      "tripStartDate": "2026-04-10",
      "status": "Saved",
      "createdAt": "2026-03-06T17:32:00Z"
    }
  ],
  "page": 1,
  "pageSize": 10,
  "totalCount": 1,
  "totalPages": 1
}
```

---

## GET `/api/itineraries/{id}`

Retrieve a saved or draft itinerary by ID.

### Path Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `id` | Guid | Itinerary ID |

### Response — 200 OK

Full itinerary response (same structure as generate response).

### Error Responses

| Status | Condition |
|--------|-----------|
| 401 | Missing or invalid JWT |
| 403 | Itinerary belongs to another user |
| 404 | Itinerary not found |

---

## DELETE `/api/itineraries/{id}`

Delete a saved itinerary.

### Response — 204 No Content

### Error Responses

| Status | Condition |
|--------|-----------|
| 401 | Missing or invalid JWT |
| 403 | Itinerary belongs to another user |
| 404 | Itinerary not found |

---

## GET `/api/itineraries/interests`

Get the predefined interest catalog.

**Authentication**: Public (no JWT required)

> **Routing note**: This endpoint is declared before `GET /api/itineraries/{id}` in the controller, and the `{id}` route uses a `[Route("{id:guid}")]` constraint. This ensures `"interests"` is never mistakenly matched as a Guid route parameter.

### Response — 200 OK

```json
{
  "interests": [
    { "name": "museums", "category": "cultural", "displayName": "Museums" },
    { "name": "parks", "category": "natural", "displayName": "Parks & Gardens" },
    { "name": "food", "category": "food", "displayName": "Food & Dining" },
    { "name": "nightlife", "category": "amusements", "displayName": "Nightlife" },
    { "name": "shopping", "category": "shops", "displayName": "Shopping" },
    { "name": "history", "category": "cultural", "displayName": "History & Heritage" },
    { "name": "landmarks", "category": "cultural", "displayName": "Landmarks" },
    { "name": "adventure", "category": "sport", "displayName": "Adventure & Sports" },
    { "name": "beaches", "category": "natural", "displayName": "Beaches" },
    { "name": "art", "category": "cultural", "displayName": "Art & Galleries" }
  ]
}
```