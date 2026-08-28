## Type

AFK

## Parent PRD

`SP-001_WE_Platform_Functional_Specification.md`

## What to build

Implement curriculum variants per region/authority end-to-end per SP-001 Ch.6 and Ch.22. Different regions or education authorities can define their own curriculum frameworks while sharing the platform. Curriculum variants are tenant-scoped and can be inherited or overridden at the school level.

Includes: curriculum variant schema, variant management API, admin UI for defining regional curricula, and tests.

## Acceptance criteria

- [ ] Education authority can define a curriculum variant (subjects, units, LOs)
- [ ] Schools within a region inherit the regional curriculum variant
- [ ] Schools can override specific units/LOs within their variant
- [ ] Curriculum variants isolated per tenant/region
- [ ] Assessments and evidence link to variant-specific LOs/micro-skills
- [ ] Tests verify variant inheritance and override behavior

## Blocked by

- `issues/041-phase7-validation-gate.md`

## User stories addressed

- SP-001 Ch.6 (Curriculum Management — variants)
- SP-001 Ch.22 (Platform Integration — curriculum extensibility)
- EP-001 §18.12 (curriculum variants)
