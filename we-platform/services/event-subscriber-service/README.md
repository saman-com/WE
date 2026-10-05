# event-subscriber-service

Bus smoke subscriber for `we.platform.events`.

Consumes `EvidenceCreated` and `AssessmentApproved` and logs them only (no database writes). Kept as a documented proof-of-delivery / topology check per issue 014 — not a domain worker.

See [evidence-approval-flow.md](../../../docs/architecture/evidence-approval-flow.md).
