## Type

AFK

## Parent PRD

`docs/product/SP-001-functional-specification.md`

Supporting references: `docs/engineering/EP-001-engineering-package.md` (§18.11)

## What to build

Implement multi-tenancy isolation end-to-end per SP-001 Ch.21 and EP-001 §18.11. Every data record is scoped to a tenant (school/district). Queries automatically filter by tenant ID. Cross-tenant data access is impossible at the API and database level. Existing single-school data migrated to tenant-scoped schema.

Includes: tenant ID on all tables, middleware for tenant resolution, migration of existing data, tenant-scoped queries in all services, and tests.

## Acceptance criteria

- [ ] All data tables include `tenant_id` column with NOT NULL constraint
- [ ] API middleware resolves tenant from auth context; all queries scoped
- [ ] Cross-tenant access returns 403 (tested with two tenant seed datasets)
- [ ] Existing data migrated to default tenant without loss
- [ ] Tenant isolation enforced at service layer, not just UI
- [ ] Integration tests verify complete tenant isolation across all services

## Blocked by

- `issues/032-phase5-validation-gate.md`

## User stories addressed

- SP-001 Ch.21 (System Administration and Platform Configuration)
- EP-001 §18.11 (Phase 7 — Multi-School Deployment)
- `docs/architecture/TD-001-technical-architecture.md` (multi-tenancy architecture)
