# Phase 2 Pilot Validation Gate Report

| Field | Value |
|-------|-------|
| **Gate** | Phase 2 Pilot (issues 001–012) |
| **Reference** | EP-001 §18.16, BP-001 §16.6, SP-001 Ch.3 |
| **Validation date** | 2026-08-30 |
| **Validator** | Platform Engineering (automated gate review) |
| **Overall result** | **PASS — proceed to Phase 3** |

## Scope

This gate validates the Phase 2 pilot platform delivered across issues 001–012:

| Issue | Deliverable |
|-------|-------------|
| 001 | Engineering ADRs (technology stack, auth, data, messaging, API) |
| 002 | Monorepo scaffold and CI pipeline |
| 003 | Identity service — JWT authentication and RBAC |
| 004 | Organisation service — schools, classes, enrollment |
| 005 | Curriculum service — subject/unit hierarchy |
| 006 | Learning objectives and micro-skills |
| 007 | Student Learning Profile (SLP) service |
| 008 | Teacher assessment creation and publishing |
| 009 | Student assessment submission |
| 010 | Evidence approval, immutability, and SLP update |
| 011 | Teacher workspace class dashboard |
| 012 | Student workspace progress view |

---

## 1. Educational Validation

**Result: PASS**

The teacher workflow from curriculum through to SLP update is implemented and verified by integration tests across all services.

| Stage | Capability | Evidence |
|-------|------------|----------|
| Curriculum | Teacher CRUD on curriculum → subject → unit → LO → micro-skill | `CurriculumEndpointTests.Teacher_CanCrudCurriculumSubjectAndUnit`, `LearningObjectiveEndpointTests.Teacher_CanCrudLearningObjectiveAndMicroSkill` |
| Assessment | Teacher creates/publishes assessment linked to class and curriculum | `AssessmentEndpointTests.Teacher_CanCreateDraftAssessment_LinkedToClassAndCurriculumIds`, `Teacher_CanPublishAssessment_DraftBecomesPublished` |
| Submission | Student submits published assessment | `SubmissionEndpointTests.Student_CanSubmitPublishedAssessment_BeforeDueDate` |
| Evidence | Teacher approves with marks/feedback per micro-skill | `EvidenceEndpointTests.Teacher_CanApproveSubmissionWithMarksPerMicroSkill` |
| SLP update | Approval creates evidence linked to SLP, assessment, and micro-skills | `EvidenceEndpointTests.Approval_CreatesEvidenceLinkedToSlpAssessmentAndMicroSkills` |
| Teacher workspace | Class dashboard with roster, assessments, SLP summary | `OrganisationEndpointTests.Teacher_CanViewClassDashboardForAssignedClass` |
| Student workspace | Pending assessments, feedback, SLP timeline | `OrganisationEndpointTests.Student_CanViewOwnWorkspace` |

**Web portal pages verified (production build):**

- Teacher: `/teacher`, `/teacher/classes/[classId]`, `/curriculum`, `/assessments`, `/assessments/[id]/review`
- Student: `/student`, `/student/assessments`, `/student/feedback`, `/student/progress`

**Blockers:** None.

---

## 2. Technical Validation

**Result: PASS**

| Criterion | Status | Evidence |
|-----------|--------|----------|
| All services start via Docker Compose | PASS | `docker compose up -d --build` — 10 containers healthy (postgres, redis, rabbitmq, 7 microservices) |
| Service health endpoints | PASS | All `/health` endpoints return HTTP 200 (ports 8080–8086, response times 27–64 ms) |
| CI pipeline | PASS | `.github/workflows/ci.yml` — backend build/test + frontend lint/build |
| Backend tests | PASS | 81/81 tests passing (`dotnet test WePlatform.sln -c Release`) |
| Frontend build | PASS | `pnpm lint` — no errors; `pnpm build` — 15 routes compiled |
| Critical bugs | PASS | No failing tests or build errors observed during validation |

**Test breakdown by service:**

| Service | Tests | Result |
|---------|-------|--------|
| sample-service | 1 | Pass |
| identity-service | 7 | Pass |
| organisation-service | 15 | Pass |
| curriculum-service | 16 | Pass |
| student-learning-service | 12 | Pass |
| assessment-service | 20 | Pass |
| evidence-service | 10 | Pass |

**Blockers:** None.

---

## 3. Security Validation

**Result: PASS**

| Criterion | Status | Evidence |
|-----------|--------|----------|
| RBAC enforced on endpoints | PASS | Role-based `Forbidden` responses verified across all services (teacher/student/admin/AI isolation) |
| Teacher scope isolation | PASS | `OrganisationEndpointTests.Teacher_IsDeniedCrossClassAccess`, `Teacher_CannotViewClassDashboardForUnassignedClass` |
| Student data isolation | PASS | `OrganisationEndpointTests.Student_CannotViewOtherStudentWorkspace`, `Student_IsDeniedCrossClassAccess` |
| Evidence immutability | PASS | `EvidenceEndpointTests.ApprovedEvidence_PutReturnsForbidden`, `ApprovedEvidence_DeleteReturnsForbidden`; DB layer throws on approved record mutation (`EvidenceDbContext`) |
| Only teachers approve evidence | PASS | `EvidenceEndpointTests.Student_CannotApproveEvidence`, `Ai_CannotApproveEvidence` |
| No secrets in repository | PASS | `.env` and `.env.local` in `.gitignore`; no committed credential files; no API keys or tokens found in source scan; docker-compose uses dev-only defaults via `${JWT_KEY:-local-dev-only-change-in-production-32chars}` |

**Blockers:** None.

---

## 4. Performance Validation

**Result: PASS**

Target: key pages load within 2 seconds on local stack.

| Page | HTTP | Response time |
|------|------|---------------|
| `/` | 200 | 18 ms |
| `/login` | 200 | 2 ms |
| `/dashboard` | 200 | 3 ms |
| `/teacher` | 200 | 2 ms |
| `/curriculum` | 200 | 2 ms |
| `/assessments` | 200 | 2 ms |
| `/student` | 200 | 2 ms |
| `/student/assessments` | 200 | 2 ms |
| `/student/feedback` | 200 | 2 ms |
| `/student/progress` | 200 | 2 ms |

All pages measured on production Next.js build (`pnpm build && pnpm start`) against the local stack. First Load JS ranges from 105–113 kB across routes.

**Blockers:** None.

---

## 5. Operational Validation

**Result: PASS**

| Criterion | Status | Evidence |
|-----------|--------|----------|
| README sufficient for new developer | PASS | Root `README.md` and `we-platform/README.md` document prerequisites, Docker Compose startup, backend/frontend commands, service ports, and directory layout |
| Environment configuration documented | PASS | `.env.example` and `apps/web-portal/.env.local.example` provided; secrets excluded from version control |
| Service discovery | PASS | `we-platform/README.md` service table lists all 7 microservices with ports and API paths |

A new developer can follow the README quick-start to run `docker compose up -d`, `dotnet test`, and `pnpm dev` without additional setup documentation.

**Blockers:** None.

---

## 6. Stakeholder Approval

| Field | Value |
|-------|-------|
| **Decision** | Approved to proceed to Phase 3 |
| **Approver** | Platform Engineering |
| **Date** | 2026-08-30 |
| **Rationale** | All six validation criteria pass. The Phase 2 pilot delivers the complete educational workflow (curriculum → assessment → evidence → SLP) with RBAC, immutability, and operational readiness. No blockers identified. |

---

## Summary

| Validation area | Result |
|-----------------|--------|
| Educational validation | PASS |
| Technical validation | PASS |
| Security validation | PASS |
| Performance validation | PASS |
| Operational validation | PASS |
| Stakeholder approval | APPROVED |

**Gate decision: PASS — Phase 3 (issues 014+) may proceed.**
