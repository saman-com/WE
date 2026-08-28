## Type

HITL

## Parent PRD

`docs/product/SP-001-functional-specification.md`

Supporting references: `docs/engineering/EP-001-engineering-package.md` (§18.16), `docs/product/BP-001-business-package.md` (§16.6)

## What to build

Conduct the Phase 2 pilot validation gate. This is a human review checkpoint — not code. Walk through EP-001 §18.16 validation criteria against the working platform from issues 001–012: educational validation, technical validation, security validation, performance validation, operational validation, and stakeholder approval.

Produce a `docs/validation/pilot-gate-report.md` documenting pass/fail for each criterion, any blockers found, and sign-off to proceed to Phase 3.

## Acceptance criteria

- [ ] Educational validation: teacher can manage curriculum → assessment → evidence → SLP workflow end-to-end
- [ ] Technical validation: all services start via Docker Compose; CI green; no critical bugs
- [ ] Security validation: RBAC enforced; evidence immutability verified; no secrets in repo
- [ ] Performance validation: key pages load within 2s on local stack
- [ ] Operational validation: README sufficient for new developer to run locally
- [ ] Stakeholder approval recorded in pilot gate report with date and approver

## Blocked by

- `issues/011-teacher-workspace-dashboard.md`
- `issues/012-student-workspace-progress.md`

## User stories addressed

- EP-001 §18.16 (Validation Gates)
- `docs/product/BP-001-business-package.md` §16.6 (Starting with a Pilot Programme)
- SP-001 Ch.3 (Complete Educational Workflow — Stages 1–12)
