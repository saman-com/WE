## Type

AFK

## Parent PRD

`SP-001_WE_Platform_Functional_Specification.md`

## What to build

Implement AI educational summaries end-to-end per SP-001 Ch.19. Teachers can request AI-generated drafts for lesson summaries and student progress report narratives. All outputs require human review before sharing with parents or publishing. Summaries use SLP/evidence context from the AI Gateway context builder.

Includes: summary prompt templates, teacher UI for requesting/editing summaries, human-review workflow, and tests.

## Acceptance criteria

- [ ] Teacher can request AI draft lesson summary for a class/unit
- [ ] Teacher can request AI draft progress report narrative for a student
- [ ] Drafts presented for editing; teacher must approve before export/share
- [ ] Context builder includes relevant SLP/evidence data (not full student record)
- [ ] Summaries clearly marked as AI-assisted drafts until teacher approves
- [ ] Tests verify review gate and context scoping

## Blocked by

- `issues/023-ai-teacher-assistant-feedback.md`

## User stories addressed

- SP-001 Ch.19 (AI Assistant — lesson support, educational summaries)
- SP-001 Ch.13 (Teacher Workspace — generate reports)
