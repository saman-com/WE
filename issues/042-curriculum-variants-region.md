## Type

AFK

## Parent PRD

`docs/product/SP-001-functional-specification.md`

## What to build

Implement curriculum variants per region/authority end-to-end per SP-001 Ch.6 and Ch.22. Different regions or education authorities can define their own curriculum frameworks while sharing the platform. Curriculum variants are tenant-scoped and can be inherited or overridden at the school level.

Includes: curriculum variant schema, variant management API, admin UI for defining regional curricula, and tests.

## Acceptance criteria

- [x] Education authority can define a curriculum variant (subjects, units, LOs)
- [x] Schools within a region inherit the regional curriculum variant
- [x] Schools can override specific units/LOs within their variant
- [x] Curriculum variants isolated per tenant/region
- [x] Assessments and evidence link to variant-specific LOs/micro-skills
- [x] Tests verify variant inheritance and override behavior

## Blocked by

- `issues/041-phase7-validation-gate.md`

## User stories addressed

- SP-001 Ch.6 (Curriculum Management — variants)
- SP-001 Ch.22 (Platform Integration — curriculum extensibility)
- EP-001 §18.12 (curriculum variants)
