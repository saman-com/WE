# ADR-0004: Authentication and Authorisation

## Status

Accepted

## Date

2026-08-28

## Approval

| Reviewer | Date | Decision |
|----------|------|----------|
| Saman | 2026-08-28 | Accepted |

## Context

The WE Platform serves students, teachers, parents, school leaders, administrators, and education authority officers. SP-001 Ch.4 defines RBAC with least-privilege access. TD-001 Ch.8 specifies OAuth 2.0, OpenID Connect, JWT access tokens, and optional enterprise IdP federation (Entra ID, Google Workspace). EP-001 requires OAuth 2.0 / OIDC / JWT with RBAC and fine-grained permissions.

## Decision

### Authentication protocol

| Layer | Choice |
|-------|--------|
| Protocol | OAuth 2.0 + OpenID Connect |
| Token format | JWT (RS256 signed access tokens) |
| Token lifetime | Access token: 15 minutes; refresh token: 7 days (rotating) |
| Session store | Redis (refresh tokens, optional session metadata) |

### V1 identity provider strategy

**Phase 1 (issues 003–013):** Built-in Identity Service with username/password + JWT. This unblocks pilot development without external IdP dependencies.

**Phase 2+:** Add OIDC federation for:

- Microsoft Entra ID (schools on Microsoft 365)
- Google Workspace (schools on Google)
- SAML 2.0 bridge for enterprise (via Entra ID B2B or dedicated SAML SP)

### Authorisation model

| Concept | Implementation |
|---------|----------------|
| Model | RBAC + permission claims in JWT |
| Roles | Student, Parent, Teacher, LearningSupportTeacher, HeadOfDepartment, DeputyPrincipal, Principal, SchoolAdministrator, SystemAdministrator, EducationAuthorityOfficer |
| Scoping | Organisation/class/student context enforced at API layer, not just UI |
| Multi-role | One identity, multiple concurrent roles (SP-001 Ch.4) |
| Least privilege | Default deny; explicit permission grants per endpoint |

### API Gateway pattern

All external traffic routes through an API Gateway (`services/api-gateway/`) that:

1. Validates JWT signature and expiry
2. Extracts roles and permissions claims
3. Forwards requests to backend services with trusted internal headers
4. Service-to-service calls use client-credentials JWT or mTLS (later)

### Security requirements

- Passwords hashed with ASP.NET Core Identity defaults (PBKDF2; evaluate Argon2 later)
- MFA mandatory for Principal, SystemAdministrator, EducationAuthorityOfficer (issue 003 seeds; enforce in issue 038+)
- Secrets in environment variables locally; Azure Key Vault / AWS Secrets Manager in production
- No PII in JWT claims beyond `sub`, `email`, `name`, `roles`

### Login flow (V1 built-in)

```
User → POST /api/v1/auth/login → Identity Service
     → Validate credentials → Issue JWT + refresh token
     → Portal stores tokens (httpOnly cookie for refresh, memory for access)
     → Subsequent requests: Authorization: Bearer <access_token>
```

## Consequences

### Positive

- JWT enables stateless API validation across microservices
- Built-in auth unblocks pilot without IdP procurement
- OIDC path is clear for school SSO integration later
- RBAC aligns with SP-001 permission philosophy

### Negative

- Built-in password auth requires secure password reset flows (issue 003 scope)
- JWT revocation requires Redis blocklist on logout (not purely stateless)
- Federation adds complexity; deferred to post-pilot

### Neutral

- API Gateway is a separate service; can start as YARP reverse proxy in .NET

## References

- SP-001 Ch.4 (User Roles and Permission Model), Ch.20 (Security, Privacy and Identity)
- TD-001 `docs/architecture/TD-001-technical-architecture.md` Ch.8 (Identity, AuthN & AuthZ)
- EP-001 `docs/engineering/EP-001-engineering-package.md` Ch.8 (API standards), identity sections
