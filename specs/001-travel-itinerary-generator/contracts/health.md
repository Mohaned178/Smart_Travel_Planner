# API Contract: Health Checks

**Base path**: `/api/health`
**Authentication**: Public (no JWT required)

---

## GET `/api/health`

Overall service health status.

### Response — 200 OK

```json
{
  "status": "Healthy",
  "timestamp": "2026-03-06T17:32:00Z",
  "checks": {
    "database": "Healthy",
    "openMeteo": "Healthy",
    "openTripMap": "Healthy",
    "openRouteService": "Healthy",
    "nominatim": "Healthy",
    "frankfurter": "Healthy"
  }
}
```

### Response — 503 Service Unavailable

```json
{
  "status": "Unhealthy",
  "timestamp": "2026-03-06T17:32:00Z",
  "checks": {
    "database": "Healthy",
    "openMeteo": "Unhealthy",
    "openTripMap": "Healthy",
    "openRouteService": "Degraded",
    "nominatim": "Healthy",
    "frankfurter": "Healthy"
  }
}
```

**Status values**: `Healthy`, `Degraded`, `Unhealthy`

**Implementation**: Uses ASP.NET Core `Microsoft.Extensions.Diagnostics.HealthChecks` with custom health checks for each external dependency.