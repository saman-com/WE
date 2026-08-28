# ADR-0006: API Design (REST)

## Status

Accepted

## Date

2026-08-28

## Approval

| Reviewer | Date | Decision |
|----------|------|----------|
| Saman | 2026-08-28 | Accepted |

## Context

All WE Platform clients (web portals, future mobile apps, ministry integrations) communicate with backend services via APIs. TD-001 and EP-001 specify REST as the primary API style for Version 1.0, with GraphQL and gRPC deferred. APIs must be versioned, documented, and backward-compatible where practical.

## Decision

### API style: REST over HTTPS

| Aspect | Standard |
|--------|----------|
| Base path | `/api/v1/` |
| Format | JSON (`application/json`) |
| Versioning | URL path (`/api/v1/`, future `/api/v2/`) |
| Documentation | OpenAPI 3.1 spec per service; aggregated at API Gateway |
| Error format | RFC 7807 Problem Details (`application/problem+json`) |

### URL conventions

```
GET    /api/v1/students/{id}/profile          # Read SLP
POST   /api/v1/assessments                    # Create assessment
PUT    /api/v1/assessments/{id}/publish       # Action as sub-resource
POST   /api/v1/assessments/{id}/submissions   # Nested resource
PATCH  /api/v1/interventions/{id}            # Partial update (status only)
```

- Resources: plural nouns, `kebab-case`
- IDs: UUID v7 (time-sortable) for all public identifiers
- Pagination: `?page=1&pageSize=20` with `X-Total-Count` header
- Filtering: `?status=active&classId={uuid}`
- Sorting: `?sort=createdAt:desc`

### HTTP status codes

| Code | Use |
|------|-----|
| 200 | Success with body |
| 201 | Created (include `Location` header) |
| 204 | Success, no body (delete) |
| 400 | Validation error |
| 401 | Unauthenticated |
| 403 | Forbidden (authenticated but insufficient permissions) |
| 404 | Resource not found |
| 409 | Conflict (e.g. duplicate enrollment) |
| 422 | Business rule violation (e.g. editing immutable evidence) |
| 429 | Rate limited |
| 500 | Internal server error (no stack trace in response) |

### API Gateway routing

| Path prefix | Target service |
|-------------|----------------|
| `/api/v1/auth/*` | Identity Service |
| `/api/v1/organisations/*` | Organisation Service |
| `/api/v1/curriculum/*` | Curriculum Service |
| `/api/v1/assessments/*` | Assessment Service |
| `/api/v1/students/*` | Student Learning Service |
| `/api/v1/evidence/*` | Evidence Service |
| `/api/v1/diagnostics/*` | Diagnostic Service |
| `/api/v1/gaps/*` | Learning Gap Service |
| `/api/v1/interventions/*` | Intervention Service |
| `/api/v1/ei/*` | Educational Intelligence Service |
| `/api/v1/ai/*` | AI Gateway (internal + teacher-facing) |
| `/api/v1/notifications/*` | Notification Service |
| `/api/v1/reports/*` | Reporting Service |
| `/api/v1/national/*` | National API (issue 044, API-key auth) |

Implementation: **YARP** (Yet Another Reverse Proxy) in `services/api-gateway/`.

### Cross-cutting headers

| Header | Purpose |
|--------|---------|
| `Authorization: Bearer <jwt>` | Authentication |
| `X-Correlation-Id` | Request tracing (generated if absent) |
| `X-Tenant-Id` | Tenant context (issue 038+) |
| `Accept-Language` | i18n preference (issue 043) |

### Deferred

| Style | When |
|-------|------|
| GraphQL | Phase 6+ for complex dashboard queries |
| gRPC | Service-to-service internal calls at scale |
| WebSockets | Real-time notifications (issue 030 may use SSE first) |

## Consequences

### Positive

- REST is well-understood, easy to test, and works with OpenAPI code generation
- URL versioning is explicit and cacheable
- Problem Details give consistent error handling across portals
- YARP integrates natively with .NET API Gateway

### Negative

- REST can require multiple round-trips for complex dashboard views; mitigated by aggregation endpoints
- No built-in subscription model; events handle async updates

### Neutral

- OpenAPI specs generated at build time and published to `/api/v1/docs` via API Gateway

## References

- TD-001 `docs/architecture/TD-001-technical-architecture.md` Ch.5 (API Gateway), integration sections
- EP-001 `docs/engineering/EP-001-engineering-package.md` Ch.8 (API standards)
- SP-001 Ch.22 (Platform Integration, APIs and Future Extensibility)
