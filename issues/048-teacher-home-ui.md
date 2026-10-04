## Type

AFK

## Parent PRD

`docs/product/UX-001-ui-design.md` (edition 1, sections 5, 12, 13, and 15)

Supporting reference: `docs/product/SP-001-functional-specification.md` (Ch.13 Teacher Workspace)

## What to build

Bring the teacher home in the web portal in line with UX-001 §12. A signed-in teacher sees the classes they teach, in the same type, spacing, and frame as the student home. One class is the focus. One primary action opens that class. Further classes are a simple list. Assessments, Curriculum, Interventions, and Parent messages stay as muted links.

Use the visual system in UX-001 §5. Do not redesign the class detail page. Do not show student-facing skill words in place of the teacher’s own terms on later teacher screens. This issue is the home only.

If a draft of this home already exists in `we-platform/apps/web-portal`, finish that draft against this issue. Do not add a second teacher home.

## Acceptance criteria

- [ ] Teacher sign-in lands on this home
- [ ] The home matches UX-001 §12 on a phone and on a wide screen
- [ ] The primary action opens that class
- [ ] A teacher with no classes sees the empty sentence in UX-001 §12
- [ ] A teacher does not see classes outside their assignments
- [ ] The frame matches the student home (wordmark, type, paper, one filled action)
- [ ] Visible strings are in the English and Arabic catalogues

## Blocked by

- `issues/011-teacher-workspace-dashboard.md`
- `issues/047-student-home-ui.md`

## User stories addressed

- UX-001 §12 (teacher home)
- SP-001 Ch.13 (Teacher Workspace — assigned classes)
