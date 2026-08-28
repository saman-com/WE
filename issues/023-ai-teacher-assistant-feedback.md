## Type

AFK

## Parent PRD

`docs/product/SP-001-functional-specification.md`

## What to build

Implement the AI teacher assistant for draft feedback end-to-end per SP-001 Ch.19. When a teacher reviews a student submission, they can request AI-drafted feedback per micro-skill. The draft is presented for human review and editing — the teacher must approve before feedback becomes official. AI never assigns grades or approves evidence alone.

Includes: AI assistant integration in teacher review UI, prompt for feedback drafting, human-review workflow, audit log entry, and tests.

## Acceptance criteria

- [ ] Teacher can request AI-drafted feedback on a student submission
- [ ] Draft displayed for teacher review; teacher can edit before saving
- [ ] AI draft is never auto-applied — teacher must explicitly accept
- [ ] Audit log records: prompt used, AI response, teacher edits, final approval
- [ ] AI cannot approve evidence or assign official grades
- [ ] Tests verify human-review gate and audit trail

## Blocked by

- `issues/022-ai-gateway-prompt-management.md`

## User stories addressed

- SP-001 Ch.19 (AI Assistant — feedback drafting)
- SP-001 Ch.3 Workflow Stage 15 (AI Educational Support)
- SP-001 §1.6 (Responsible Artificial Intelligence)
