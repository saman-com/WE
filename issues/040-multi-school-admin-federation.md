## Type

AFK

## Parent PRD

`SP-001_WE_Platform_Functional_Specification.md`

## What to build

Implement multi-school admin portal and federation end-to-end per SP-001 Ch.21 and Ch.4. A district/regional administrator manages multiple schools (tenants): create schools, assign school admins, view cross-school aggregated metrics, and manage federation policies. Each school remains an isolated tenant.

Includes: federation admin portal, cross-tenant admin API (scoped to federation admin role), school provisioning workflow, and tests.

## Acceptance criteria

- [ ] Federation admin can create new school tenants and assign school admins
- [ ] Federation admin sees cross-school aggregated metrics (enrollment, progress)
- [ ] School admins manage only their own tenant
- [ ] Federation admin cannot access individual student PII across schools
- [ ] School provisioning creates tenant with default configuration
- [ ] Tests verify federation admin vs school admin scope

## Blocked by

- `issues/039-regional-configuration.md`

## User stories addressed

- SP-001 Ch.21 (System Administration)
- SP-001 Ch.4 (User Roles — Education Authority Officer)
- EP-001 §18.11 (organisation management, federation)
