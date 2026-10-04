## Type

AFK

## Parent PRD

`docs/product/UX-001-ui-design.md` (edition 1, sections 3–11 and 13–15)

Supporting reference: `docs/product/SP-001-functional-specification.md` (Ch.14 Student Workspace)

## What to build

Bring the student home in the web portal in line with UX-001. A signed-in student lands on Today and can move through Skills, Next, Notes, and Growth. The home answers “what should I focus on today?” using that student’s assessments and teacher-approved evidence.

Phone is one column with the tabs fixed along the bottom. A wide screen uses the same blocks, with the tabs under the header and a second column as specified in UX-001 §5 and §§7–11.

If a draft of these screens already exists in `we-platform/apps/web-portal`, finish that draft against this issue. Do not add a second home. Do not design or build the task-writing page, the teacher class page, parent, leadership, messages, calendar, or portfolio. Those are outside UX-001 edition 1.

Copy goes through the existing English and Arabic catalogues. Students see Strong, Solid, Getting there, and Not yet. They do not see gap, failure, weakness, severity, or another student’s results.

## Acceptance criteria

- [ ] Student sign-in lands on Today
- [ ] Today, Skills, Next, Notes, and Growth match UX-001 §§7–11 on a phone and on a wide screen
- [ ] Today’s primary action opens the due assessment, not a generic list
- [ ] Skill rows and growth points come from that student’s approved evidence only
- [ ] Empty states use the sentences in UX-001
- [ ] Student copy uses Strong, Solid, Getting there, and Not yet, in English and Arabic
- [ ] Student cannot see other students’ data or teacher-only fields
- [ ] Tests cover how a mark out of 5 becomes those four words, and which task and skill the home selects

## Blocked by

- `issues/012-student-workspace-progress.md`
- `issues/043-multilingual-ui-i18n.md`

## User stories addressed

- UX-001 §§7–11 (student home)
- SP-001 Ch.14 (Student Workspace — understand learning, monitor progress, review feedback)
