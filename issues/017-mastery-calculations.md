## Type

AFK

## Parent PRD

`docs/product/SP-001-functional-specification.md`

## What to build

Implement mastery calculations end-to-end per SP-001 Ch.7 and Ch.11. Aggregate evidence across multiple sources to compute mastery level per micro-skill on the SLP. Mastery uses configurable thresholds and supports multi-source evidence weighting. Results are deterministic.

Includes: mastery calculation in Educational Intelligence Service (or dedicated module), schema for mastery records, API, SLP mastery view, and tests with known evidence sets.

## Acceptance criteria

- [ ] Mastery level computed per micro-skill from all approved evidence
- [ ] Multi-source evidence aggregated (not just latest assessment)
- [ ] Mastery levels: not started / developing / proficient / mastered (or equivalent)
- [ ] Mastery recalculates when new evidence approved (event-driven)
- [ ] Mastery visible on SLP alongside diagnostics and gaps
- [ ] Tests verify mastery calculation with predefined evidence scenarios

## Blocked by

- `issues/015-diagnostic-engine.md`

## User stories addressed

- SP-001 Ch.7 (Micro-Skill mastery measurement)
- SP-001 Ch.11 (Educational Intelligence Engine — mastery calculations)
- SP-001 Ch.5 (SLP — continuous growth)
