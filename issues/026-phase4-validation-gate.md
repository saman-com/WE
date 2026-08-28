## Type

HITL

## Parent PRD

`docs/product/SP-001-functional-specification.md`

Supporting references: `docs/engineering/EP-001-engineering-package.md` (§18.16)

## What to build

Conduct the Phase 4 validation gate. Human review checkpoint for AI features (issues 022–025). Verify AI assists but never replaces teacher judgement, all AI outputs require human review, governance audit trail is complete, and safety filters work. Produce `docs/validation/phase4-gate-report.md`.

## Acceptance criteria

- [ ] Educational validation: AI drafts feedback/summaries; teacher always approves before use
- [ ] AI never assigns official grades or approves evidence autonomously
- [ ] Technical validation: AI Gateway is sole entry point; audit logs complete
- [ ] Security validation: safety filters block prohibited actions; no PII in AI logs
- [ ] Governance validation: prompt versions tracked; provider documented in ADR
- [ ] Stakeholder approval recorded in phase 4 gate report

## Blocked by

- `issues/025-ai-governance-audit-safety.md`

## User stories addressed

- EP-001 §18.16 (Validation Gates)
- EP-001 §18.8 (Phase 4 — Artificial Intelligence)
- SP-001 §1.6 (Responsible Artificial Intelligence)
