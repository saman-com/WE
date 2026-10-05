# Evidence approval flow

Teacher approval of educational evidence fans out to the Student Learning Profile, EI engines (diagnostics, gaps, mastery), EDW, and notifications.

## Sequence

```
Teacher POST /api/v1/evidence
        │
        ▼
 evidence-service
   ├── INSERT educational_evidence + evidence_micro_skill_marks
   ├── INSERT MassTransit outbox rows (same DB transaction)
   └── 201 Created (after commit)
        │
        ├── (outbox delivery) EvidenceCreated → we.platform.events / EvidenceCreated
        │      ├── student-learning-service-evidence-created → profile_evidence_entries (+ micro_skills)
        │      ├── diagnostic-service-evidence-created → micro_skill_diagnostics
        │      ├── learning-gap-service-evidence-created → learning_gaps
        │      ├── mastery-service-evidence-created → mastery_evidence_marks, mastery_records
        │      ├── edw-ingest-service → fact_evidence
        │      └── platform-events-subscriber → log only (bus smoke)
        │
        └── (outbox delivery) AssessmentApproved → we.platform.events / AssessmentApproved
               ├── edw-ingest-service → fact_assessment
               ├── notification-service-events → notifications
               └── platform-events-subscriber → log only
```

| Step | Publisher / consumer | Exchange / routing / queue | Tables written |
|------|----------------------|---------------------------|----------------|
| Approve | evidence-service | — | `educational_evidence`, `evidence_micro_skill_marks`, MassTransit outbox |
| SLP | student-learning `EvidenceCreated` consumer | `we.platform.events` / `EvidenceCreated` → `student-learning-service-evidence-created` | `profile_evidence_entries`, `profile_evidence_micro_skills` |
| Diagnostics | diagnostic-service | same exchange/RK → `diagnostic-service-evidence-created` | `micro_skill_diagnostics` |
| Gaps | learning-gap-service | same → `learning-gap-service-evidence-created` | `learning_gaps` |
| Mastery | mastery-service | same → `mastery-service-evidence-created` | `mastery_evidence_marks`, `mastery_records` |
| EDW evidence | edw-ingest-service | same → `edw-ingest-service` | `fact_evidence`, `dim_time` |
| EDW assessment | edw-ingest-service | `AssessmentApproved` → `edw-ingest-service` | `fact_assessment` |
| Notify | notification-service | `AssessmentApproved` → `notification-service-events` | `notifications` |
| Smoke | event-subscriber-service | both RKs → `platform-events-subscriber` | none (logs only) |

## Design notes

- **Outbox:** evidence-service uses MassTransit EF Core bus outbox so events are committed with the evidence row and delivered when RabbitMQ is available.
- **Inbox:** student-learning, diagnostic, learning-gap, and mastery consumers use the EF consumer outbox/inbox for duplicate delivery handling, plus unique-constraint fallbacks.
- **Tenant:** each of those consumers calls `ITenantContext.SetTenant(message.OrganisationId)` before processing so query filters apply.
- **SLP:** updated asynchronously from `EvidenceCreated` (optional `Title` on the event). `POST /api/v1/students/{id}/profile/evidence` remains for backfill (`scripts/backfill-profile-evidence.sh`).
- **AssessmentApproved:** consumed by EDW and notifications only. EI engines correctly subscribe to `EvidenceCreated` (issues 015–017).
- **event-subscriber-service:** kept as a documented bus smoke subscriber (issue 014 proof of delivery). No domain side effects.

## Local upgrade note

Services call `EnsureCreated` on startup, then run idempotent SQL (`WePlatform.Messaging.MassTransitOutboxInboxSchema`, mirrored under `databases/<service>/V00*__masstransit_outbox_inbox.sql`) so existing Postgres volumes gain MassTransit `InboxState` / `OutboxState` / `OutboxMessage` tables. See `deployment/runbook.md`.

Consumers use delayed redelivery (1/5/15 minutes) after immediate retries. Alert on `*_error` queue depth (see `monitoring/README.md`).

## Follow-ups (not in this change)

- Add MassTransit EF outbox to assessment-service and communication-service publishers.
- Fail closed when `ITenantContext` has no tenant (`shared/tenancy/TenantModelBuilderExtensions.cs` currently allows unfiltered reads).
- Mastery supersede/correction rule (deferred: approved evidence is immutable; new evidence adds marks).
