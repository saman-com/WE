## Type

AFK

## Parent PRD

`SP-001_WE_Platform_Functional_Specification.md`

## What to build

Implement the Diagnostic Engine end-to-end per SP-001 Ch.9. When `EvidenceCreated` events arrive, the engine analyses performance per micro-skill and produces diagnostic insights on the SLP: what the student knows, partially understands, and struggles with. Diagnostics are deterministic and explainable — not AI-generated.

Includes: Diagnostic Service, schema, event subscriber, analysis rules engine, API for teacher to view diagnostics, SLP diagnostic section in UI, and tests.

## Acceptance criteria

- [ ] Diagnostic Service subscribes to `EvidenceCreated` events
- [ ] Diagnostics generated per micro-skill: mastered / developing / struggling
- [ ] Each diagnostic includes explainable reason (which evidence, which micro-skill)
- [ ] Diagnostics appear on student SLP view (teacher-facing)
- [ ] Diagnostics are deterministic — same evidence produces same diagnostic
- [ ] Tests cover diagnostic generation from sample evidence events

## Blocked by

- `issues/014-domain-event-bus.md`

## User stories addressed

- SP-001 Ch.9 (Diagnostic Engine Module)
- SP-001 Ch.3 Workflow Stage 11 (Diagnostic Analysis)
- SP-001 Ch.11 (Educational Intelligence Engine — diagnostic component)
