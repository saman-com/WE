## Type

AFK

## Parent PRD

`docs/product/SP-001-functional-specification.md`

## What to build

Implement the Identity Service end-to-end: user registration/login, JWT issuance, role assignment (Student, Teacher, Parent, School Leader, System Administrator), and RBAC enforcement per SP-001 Ch.4 and Ch.20.

Includes: PostgreSQL schema for users/roles/permissions, ASP.NET Core Identity Service with REST API (`/api/v1/auth/login`, `/api/v1/auth/me`), middleware for JWT validation, seed data for test roles, minimal login UI in the web app, and integration tests proving unauthorized requests are rejected.

## Acceptance criteria

- [x] Users can log in and receive a JWT; `/api/v1/auth/me` returns user + roles
- [x] RBAC enforced: endpoints reject requests without required role
- [x] At least 5 platform roles seeded (Student, Teacher, Parent, School Leader, System Administrator)
- [x] Passwords hashed; secrets not committed to git
- [x] Login UI works end-to-end against local Docker stack
- [x] Unit and integration tests cover auth success and failure paths

## Blocked by

- `issues/002-monorepo-scaffold-ci.md`

## User stories addressed

- SP-001 Ch.4 (User Roles and Permission Model)
- SP-001 Ch.20 (Security, Privacy and Identity Management)
- SP-001 §1.11 (Platform Users)
