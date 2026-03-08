# API Contract: Authentication

**Base path**: `/api/auth`
**Authentication**: Public (no JWT required for these endpoints)

---

## POST `/api/auth/register`

Register a new user account.

### Request

```json
{
  "email": "user@example.com",
  "password": "SecureP@ssw0rd!",
  "displayName": "John Doe"
}
```

| Field | Type | Required | Validation |
|-------|------|----------|------------|
| `email` | string | ✅ | Valid email format, max 256 chars, must be unique |
| `password` | string | ✅ | Min 8 chars, at least 1 uppercase, 1 lowercase, 1 digit, 1 special char |
| `displayName` | string | ❌ | Max 100 chars |

### Response — 201 Created

```json
{
  "userId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "email": "user@example.com",
  "displayName": "John Doe"
}
```

### Error Responses

| Status | Condition |
|--------|-----------|
| 400 | Validation error (RFC 7807 Problem Details) |
| 409 | Email already registered |

---

## POST `/api/auth/login`

Authenticate and receive a JWT access token.

### Request

```json
{
  "email": "user@example.com",
  "password": "SecureP@ssw0rd!"
}
```

### Response — 200 OK

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "expiresAt": "2026-03-06T19:32:00Z",
  "tokenType": "Bearer"
}
```

**Token lifetime**: Tokens expire after 60 minutes (`Jwt:ExpirationMinutes` in appsettings). Re-authenticate after expiry. Refresh tokens are not supported in this version.

### Error Responses

| Status | Condition |
|--------|-----------|
| 400 | Validation error |
| 401 | Invalid credentials |

---

## Notes

**FR-017 carve-out**: The `/api/auth/register` and `/api/auth/login` endpoints, along with `GET /api/itineraries/interests` and `GET /api/health`, are explicitly excluded from JWT authentication requirements. All other endpoints require a valid Bearer token. This is the intended design — these public endpoints enable onboarding and catalog discovery without requiring prior authentication.