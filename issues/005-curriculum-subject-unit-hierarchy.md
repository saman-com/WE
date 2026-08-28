## Type

AFK

## Parent PRD

`docs/product/SP-001-functional-specification.md`

## What to build

Implement Curriculum Management end-to-end per SP-001 Ch.6. Teachers or administrators can create a curriculum hierarchy: subject → unit → topic structure scoped to a school. The curriculum drives all downstream learning activities.

Includes: Curriculum Service with PostgreSQL schema, REST API, teacher/admin UI for browsing and editing curriculum tree, and tests.

## Acceptance criteria

- [ ] CRUD for curriculum, subject, and unit hierarchy via `/api/v1/curriculum/...`
- [ ] Curriculum scoped to a school/organisation
- [ ] UI displays navigable curriculum tree (subject → units)
- [ ] Curriculum records linked to organisation from issue 004
- [ ] API returns 403 for users without curriculum-management permission
- [ ] Unit tests cover hierarchy integrity (no orphan units)

## Blocked by

- `issues/004-organisation-classes-enrollment.md`

## User stories addressed

- SP-001 Ch.6 (Curriculum Management Module)
- SP-001 Ch.3 Workflow Stages 1–3 (Curriculum Design, Subject Structure, Unit Planning)
