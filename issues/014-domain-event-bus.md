## Type

AFK

## Parent PRD

`SP-001_WE_Platform_Functional_Specification.md`

Supporting references: `ABC_EP001.md` (Ch.10), `ABC.md` (§2.8)

## What to build

Implement the domain event bus end-to-end per SP-001 Ch.2 §2.8 and EP-001 Ch.10. When evidence is approved (issue 010), publish `EvidenceCreated` and `AssessmentApproved` events to RabbitMQ. Downstream services (EI modules) subscribe without tight coupling.

Includes: shared event contracts in `shared/events/`, publisher in Evidence/Assessment Service, subscriber skeleton with logging, RabbitMQ integration, and tests verifying events are published on approval.

## Acceptance criteria

- [ ] `EvidenceCreated` event published to RabbitMQ when teacher approves evidence
- [ ] `AssessmentApproved` event published with assessment ID, student ID, micro-skill results
- [ ] Event contracts versioned in `shared/events/` with JSON schema
- [ ] Subscriber service logs received events (proof of delivery)
- [ ] Events are immutable once published (append-only)
- [ ] Integration tests verify publish on approval and subscriber receipt

## Blocked by

- `issues/010-evidence-immutability-slp-update.md`

## User stories addressed

- SP-001 Ch.2 §2.8 (Platform Events)
- SP-001 Ch.2 §2.5 (Educational Information Flow)
- EP-001 Ch.10 (Event-Driven Development Guide)
