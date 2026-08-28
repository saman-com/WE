## Type

AFK

## Parent PRD

`docs/product/SP-001-functional-specification.md`

## What to build

Implement Learning Objectives and Micro-Skills management end-to-end per SP-001 Ch.7. Teachers attach learning objectives to curriculum units and break each objective into measurable micro-skills. This is the precision layer that all assessments and diagnostics reference.

Includes: Learning Objective Service (or module within Curriculum Service), schema, API, teacher UI for defining LOs and micro-skills under a unit, and tests.

## Acceptance criteria

- [ ] CRUD for learning objectives linked to curriculum units
- [ ] CRUD for micro-skills linked to learning objectives
- [ ] UI shows LO → micro-skill hierarchy under a curriculum unit
- [ ] Micro-skills are the smallest measurable unit (referenced by ID in later slices)
- [ ] Deleting a unit with LOs requires explicit cascade or block (documented behavior)
- [ ] Tests verify LO/micro-skill hierarchy integrity

## Blocked by

- `issues/005-curriculum-subject-unit-hierarchy.md`

## User stories addressed

- SP-001 Ch.7 (Learning Objective and Micro-Skill Management)
- SP-001 Ch.3 Workflow Stages 4–5 (Learning Objectives, Micro-Skills)
