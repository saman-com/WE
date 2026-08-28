## Type

AFK

## Parent PRD

`docs/product/SP-001-functional-specification.md`

## What to build

Implement regional configuration per tenant end-to-end per SP-001 Ch.21. Each tenant (school/district) can configure: academic calendar, grading scale, assessment models, reporting templates, and locale settings. Configuration stored per tenant and applied platform-wide for that tenant's users.

Includes: Configuration Service (or admin module), tenant-scoped settings schema, admin UI for regional config, and tests.

## Acceptance criteria

- [ ] Tenant admin can configure academic calendar (term dates, holidays)
- [ ] Tenant admin can configure grading scale and assessment models
- [ ] Configuration changes apply to all users within the tenant
- [ ] Different tenants can have different configurations simultaneously
- [ ] Configuration API scoped to tenant admin role
- [ ] Tests verify config isolation between tenants

## Blocked by

- `issues/038-multi-tenancy-isolation.md`

## User stories addressed

- SP-001 Ch.21 (System Administration — platform configuration)
- EP-001 §18.11 (regional configuration)
- SP-001 Ch.6 (Curriculum — school-specific structures)
