## Type

AFK

## Parent PRD

`docs/product/SP-001-functional-specification.md`

Supporting references: `docs/engineering/EP-001-engineering-package.md` (Ch.10), `docs/architecture/TD-001-technical-architecture.md` (§2.8)

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
