## Type

AFK

## Parent PRD

`docs/product/SP-001-functional-specification.md`

## What to build

Implement school organisation structure end-to-end: school, year levels, classes, and student/teacher enrollment. An administrator can create a school, define classes, assign teachers to classes, and enroll students. Teachers see only their assigned classes; students see only their enrollments.

Includes: Organisation Service schema + API, admin UI for setup, RBAC scoping by organisation, and tests verifying least-privilege access per SP-001 Ch.4.

## Acceptance criteria

- [ ] CRUD for school, year level, class via `/api/v1/organisations/...`
- [ ] Teachers can be assigned to classes; students enrolled in classes
- [ ] Teacher role sees only their classes; student role sees only their enrollment
- [ ] Admin UI allows creating a school with at least one class and enrolling a student
- [ ] Database migrations versioned in `databases/`
- [ ] Integration tests verify cross-class access is denied

## Blocked by

- `issues/003-identity-auth.md`

## User stories addressed

- SP-001 Ch.4 (User Roles and Permission Model)
- SP-001 Ch.3 Workflow Stage 8 (Student Participation)
- SP-001 §1.11 (Teachers, Students, System Administrators)
