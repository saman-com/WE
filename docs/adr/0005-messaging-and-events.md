# ADR-0005: Messaging and Domain Events

## Status

Accepted

## Date

2026-08-28

## Approval

| Reviewer | Date | Decision |
|----------|------|----------|
| Saman | 2026-08-28 | Accepted |

## Context

The WE Platform educational workflow is event-driven: assessment approval creates evidence, evidence triggers diagnostics, diagnostics feed learning gaps, and gaps drive interventions. TD-001 §2.6 and EP-001 Ch.10 require an event bus for asynchronous domain events. TD-001 recommends RabbitMQ for Version 1.0 (Kafka as enterprise scale-out option).

## Decision

### Message broker: RabbitMQ 3.13

- Local/dev: RabbitMQ via Docker Compose (management UI on port 15672)
- Production: Managed RabbitMQ (CloudAMQP, Amazon MQ, or self-hosted cluster)
- Protocol: AMQP 0.9.1 via .NET client (`RabbitMQ.Client` or MassTransit)

### Event design

| Principle | Rule |
|-----------|------|
| Naming | PascalCase past tense: `EvidenceCreated`, `AssessmentApproved`, `InterventionCreated` |
| Contracts | Versioned JSON schemas in `we-platform/shared/contracts/events/` |
| Immutability | Events are append-only; never modify published events |
| Idempotency | Consumers must handle duplicate delivery (idempotency keys) |
| Payload | Event ID, correlation ID, timestamp, tenant ID, aggregate ID, minimal data |

### Initial domain events (Phase 2–3)

| Event | Publisher | Subscribers |
|-------|-----------|-------------|
| `AssessmentPublished` | Assessment Service | Notification Service |
| `AssessmentSubmitted` | Assessment Service | Notification Service |
| `AssessmentApproved` | Assessment Service | Evidence Service, Notification Service |
| `EvidenceCreated` | Evidence Service | Diagnostic Engine, EDW Ingest |
| `DiagnosticCompleted` | Diagnostic Service | Learning Gap Engine |
| `LearningGapIdentified` | Learning Gap Service | EI Engine, Notification Service |
| `InterventionCreated` | Intervention Service | Notification Service, EDW Ingest |
| `MasteryUpdated` | EI Service | SLP Service, Notification Service |

### Publishing pattern

Services publish to a **topic exchange** (`we.platform.events`) with routing keys matching event type names. Each subscriber declares its own queue bound to relevant routing keys.

```
Assessment Service → [we.platform.events] → evidence.queue → Evidence Service
                                          → notification.queue → Notification Service
```

### Outbox pattern

Use the **transactional outbox** pattern: domain events written to an `outbox` table in the same DB transaction as the aggregate change, then relayed to RabbitMQ by a background worker. This prevents lost events on failure.

### Library choice

**MassTransit** over raw RabbitMQ.Client:

- Built-in outbox, retry, dead-letter queues
- Consumer registration and DI integration with ASP.NET Core
- Saga support for future multi-step workflows

## Consequences

### Positive

- Decouples services; EI modules can evolve independently
- RabbitMQ is simpler to operate than Kafka for pilot scale
- Outbox pattern ensures reliable event delivery
- MassTransit reduces boilerplate

### Negative

- RabbitMQ is not ideal for event replay at national scale; Kafka migration path documented in TD-001 Phase 2
- Outbox adds latency (milliseconds) between write and publish
- Event schema versioning requires discipline

### Neutral

- Synchronous REST remains primary for user-facing request/response; events for side effects and EI pipeline only

## References

- TD-001 `docs/architecture/TD-001-technical-architecture.md` §2.8 (RabbitMQ), Ch.5 (Event-Driven Architecture)
- EP-001 `docs/engineering/EP-001-engineering-package.md` Ch.10 (Event-Driven Development Guide)
- SP-001 Ch.2 §2.8 (Platform Events), Ch.3 (Educational Workflow)
