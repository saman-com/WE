# ADR-0001: Technology Stack

## Status

Accepted

## Date

2026-08-28

## Approval

| Reviewer | Date | Decision |
|----------|------|----------|
| Saman | 2026-08-28 | Accepted |

## Context

The WE Platform is an Educational Intelligence Platform requiring long-term maintainability, strong security, multi-portal web clients, and eventual AI/EI services. TD-001 §2.8 recommends a specific stack for Version 1.0. EP-001 requires technology-independent standards but defers concrete vendor choices to TD-001 and ADRs.

We need a single, documented stack before scaffolding the monorepo (issue 002) so all services and portals share consistent tooling.

## Decision

Adopt the TD-001 Version 1.0 recommended stack:

### Frontend

| Component | Choice |
|-----------|--------|
| Framework | React 19 |
| Meta-framework | Next.js 15 (App Router) |
| Language | TypeScript 5.x |
| Styling | Tailwind CSS 4.x |
| Component library | shadcn/ui (Tailwind-based; use Material UI only where a complex data component warrants it) |

Portals under `we-platform/apps/`:

- `teacher-portal`
- `student-portal`
- `parent-portal`
- `leadership-portal`
- `admin-portal`

### Backend

| Component | Choice |
|-----------|--------|
| Runtime | .NET 9 |
| Framework | ASP.NET Core (Minimal APIs + controllers where appropriate) |
| Language | C# 13 |
| ORM | Entity Framework Core 9 |
| API documentation | OpenAPI (Swashbuckle) |

Each bounded context is a separate service under `we-platform/services/`.

### Supporting languages

| Use | Language |
|-----|----------|
| AI services / scripts | Python 3.12 (in `we-platform/ai/`) |
| Infrastructure | YAML (Kubernetes, GitHub Actions), HCL (Terraform) |
| Automation | Bash / PowerShell in `we-platform/scripts/` |

### Local development

| Component | Choice |
|-----------|--------|
| Container runtime | Docker + Docker Compose |
| Package managers | npm/pnpm (frontend), NuGet (.NET) |

## Consequences

### Positive

- Aligns with TD-001 enterprise recommendations and EP-001 layered service architecture
- .NET 9 provides strong security, performance, and long-term Microsoft support
- Next.js enables SSR/SSG for portals, good DX, and shared TypeScript types with API clients
- Single stack reduces onboarding friction for contributors

### Negative

- Team must maintain both C# and TypeScript expertise
- Python AI services add a third runtime (isolated to `ai/` directory)
- Material UI deferred to shadcn/ui may require custom work for complex data grids later

### Neutral

- Java/Spring Boot remains a documented alternative in TD-001 but is not pursued for V1
- Mobile apps (iOS/Android) are out of scope until post-pilot; web-first responsive design applies

## References

- TD-001 `docs/architecture/TD-001-technical-architecture.md` §2.8 Recommended Technology Stack
- EP-001 `docs/engineering/EP-001-engineering-package.md` Ch.4 (Backend), Ch.5 (Frontend)
- SP-001 Ch.2 (Overall Platform Architecture)
