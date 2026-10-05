# WE Platform feature test matrix

Coverage map for delivery feature checks. Paths are relative to the repo root (`edu_app/`).  
Status: `covered` | `partial` | `missing` | `linked` | `deferred` | `not required`.

Updated: 2026-10-05 (batch 19 NFR; batches 18 Arabic + 8–10 homes/interventions; 3–5; 14 leadership; 15 analytics; 16 federation/config).

---

## 1. Sign-in

| feature | check | layer | existing test file | status |
|---------|-------|-------|--------------------|--------|
| Sign-in | Login succeeds for valid credentials | backend | `we-platform/services/identity-service/tests/AuthEndpointTests.cs` (`Login_WithValidCredentials_ReturnsJwt`) | covered |
| Sign-in | Login fails for invalid credentials | backend | `we-platform/services/identity-service/tests/AuthEndpointTests.cs` (`Login_WithInvalidCredentials_ReturnsUnauthorized`) | covered |
| Sign-in | `/me` with valid token returns user and roles | backend | `we-platform/services/identity-service/tests/AuthEndpointTests.cs` (`Me_WithValidToken_ReturnsUserAndRoles`) | covered |
| Sign-in | `/me` without token returns 401 | backend | `we-platform/services/identity-service/tests/AuthEndpointTests.cs` (`Me_WithoutToken_ReturnsUnauthorized`) | covered |
| Sign-in | `/admin` requires Admin role (403 otherwise) | backend | `we-platform/services/identity-service/tests/AuthEndpointTests.cs` (`AdminEndpoint_WithoutAdminRole_ReturnsForbidden`, `AdminEndpoint_WithAdminRole_ReturnsOk`) | covered |
| Sign-in | Expired token rejected on protected endpoints | backend | `we-platform/services/identity-service/tests/AuthEndpointTests.cs` (`Me_WithExpiredToken_ReturnsUnauthorized`) | covered |
| Sign-in | Each role gets 403 on other roles' endpoints (matrix across portal routes / APIs) | e2e + backend | `we-platform/testing/e2e/tests/g-access-control.spec.ts` (role×path blocked cases); identity `AdminEndpoint_NonAdminRoles_ReturnForbidden` — not every API | partial |
| Sign-in | Token survives page refresh | e2e | `we-platform/testing/e2e/tests/signin.spec.ts` (`token survives page refresh`) | covered |
| Sign-in | Logout clears session and blocks protected pages | e2e | `we-platform/testing/e2e/tests/signin.spec.ts` (`logout clears session…`; identity has no logout API — UI clears `localStorage`) | covered |

## 2. School isolation

| feature | check | layer | existing test file | status |
|---------|-------|-------|--------------------|--------|
| School isolation | School B user with School A org/class/student/resource IDs gets 403/404 and never data — **every** ID-bearing endpoint, table-driven per service or cross-service suite | backend | Source-scanned catalog + coverage guard: `we-platform/testing/SchoolIsolation.Tests/` (`EndpointCoverageGuardTests`, `isolation-catalog.json`). Table-driven both-direction probes in each service’s `TenantIsolationEndpointTests.cs` (incl. federation). Consumer isolation covered in diagnostic/gaps/mastery/learning/edw/notification suites. e2e spot-checks remain in `testing/e2e/tests/g-access-control.spec.ts`. | covered |
| School isolation | `IgnoreQueryFilters()` bypasses are allow-listed with reason; new bypasses fail CI | backend | Audit: `docs/validation/ignore-query-filters-audit.md`. Guard: `SchoolIsolation.Tests/IgnoreQueryFiltersGuardTests` + `ignore-query-filters-allowlist.json`. | covered |
| School isolation | Tenant EF filter fail-closed when no tenant is set | backend | `TenantAwareDbContext.CurrentTenantId` filter (`tenant set ∧ TenantId match`); processors/consumers call `SetTenant`; shared `DimTime` joins use allow-listed `IgnoreQueryFilters`. | covered |
| School isolation | Federation admin A cannot list/create/assign/change policies or metrics for federation B (both directions) | backend | `federation-service/tests/TenantIsolationEndpointTests.cs` (+ existing `FederationEndpointTests` cross-federation cases). Federations are modelled via `FederationId` claim scoping. | covered |

Services with at least one isolation test today: ai-gateway, assessment, communication, configuration, curriculum, diagnostic, edw-ingest, ei, evidence, federation, identity, intervention, learning-gap, mastery, notification, organisation, reporting, student-learning.

## 3. Organisation setup

| feature | check | layer | existing test file | status |
|---------|-------|-------|--------------------|--------|
| Organisation setup | Create / edit / delete schools, year levels, classes | backend | `we-platform/services/organisation-service/tests/OrganisationEndpointTests.cs` (`Admin_CanCrudOrganisationYearLevelAndClass`) | covered |
| Organisation setup | Assign teachers | backend | `we-platform/services/organisation-service/tests/OrganisationEndpointTests.cs` (`Admin_CanAssignTeacherAndEnrollStudent`) | covered |
| Organisation setup | Assign school leaders | backend | `LeadershipDashboardEndpointTests.Admin_CanAssignSchoolLeader` | covered |
| Organisation setup | Enrol students | backend | `we-platform/services/organisation-service/tests/OrganisationEndpointTests.cs` (`Admin_CanAssignTeacherAndEnrollStudent`, `EnrollStudent_AutoCreatesStudentLearningProfile`) | covered |
| Organisation setup | Link parents to students | backend | `we-platform/services/organisation-service/tests/ParentWorkspaceEndpointTests.cs` (`Admin_LinksParentToStudent`) | covered |
| Organisation setup | Duplicate school / year / class / enrolment / parent-link rejected | backend | `OrganisationEndpointTests` (`DuplicateOrganisationCode_IsRejected`, `DuplicateYearLevelName_IsRejected`, `DuplicateClassCode_IsRejected`, `DuplicateEnrollment_IsRejected`); `ParentWorkspaceEndpointTests.DuplicateParentLink_IsRejected` | covered |
| Organisation setup | Deleting a class that has students is rejected or handled safely | backend | `OrganisationEndpointTests.DeleteClass_WithEnrolledStudents_CascadesSafely` (cascade delete is the product behaviour) | covered |

## 4. Curriculum

| feature | check | layer | existing test file | status |
|---------|-------|-------|--------------------|--------|
| Curriculum | Full hierarchy (subject → unit → LO → micro-skill) | backend | `we-platform/services/curriculum-service/tests/CurriculumEndpointTests.cs`, `LearningObjectiveEndpointTests.cs` | covered |
| Curriculum | `/tree` returns navigable hierarchy | backend | `CurriculumEndpointTests.Tree_ReturnsNavigableSubjectUnitHierarchy`; `LearningObjectiveEndpointTests.Tree_ReturnsLearningObjectiveMicroSkillHierarchyUnderUnit` | covered |
| Curriculum | Inherit regional variant into school | backend | `we-platform/services/curriculum-service/tests/CurriculumVariantEndpointTests.cs` (`School_InheritsRegionalCurriculumVariant_WithClonedTreeAndSourceLinks`) | covered |
| Curriculum | Variant-links endpoint / source links | backend | `CurriculumVariantEndpointTests.VariantLinks_ReturnsOnlyVariantSpecificLearningObjectiveAndMicroSkillIds`; also covered in assessment-link scenario | covered |
| Curriculum | School override must not change parent (regional) curriculum | backend | `CurriculumVariantEndpointTests.School_CanOverrideUnitAndLearningObjective_WithoutAffectingRegionalOrOtherSchools` | covered |

## 5. Assessments

| feature | check | layer | existing test file | status |
|---------|-------|-------|--------------------|--------|
| Assessments | Drafts hidden from students | backend | `we-platform/services/assessment-service/tests/AssessmentEndpointTests.cs` (`Student_CannotSeeDraftAssessments`, `Student_CanListPublishedAssessments_ForEnrolledClassOnly`) | covered |
| Assessments | Editing after publishing rejected | backend | `AssessmentEndpointTests.Teacher_CannotEditPublishedAssessment` | covered |
| Assessments | Deleting after publishing rejected | backend | `AssessmentEndpointTests.Teacher_CannotDeletePublishedAssessment` | covered |
| Assessments | One submission per student | backend | `we-platform/services/assessment-service/tests/SubmissionEndpointTests.cs` (`Student_CannotSubmitTwice`) | covered |
| Assessments | Submitting after due date | backend | `SubmissionEndpointTests.Student_LateSubmission_IsAcceptedAndMarkedLate` (accepted + `IsLate=true`; documented product behaviour) | covered |

## 6. Evidence

| feature | check | layer | existing test file | status |
|---------|-------|-------|--------------------|--------|
| Evidence | Approved evidence rejects PUT | backend | `we-platform/services/evidence-service/tests/EvidenceEndpointTests.cs` (`ApprovedEvidence_PutReturnsForbidden`) | covered |
| Evidence | Approved evidence rejects DELETE | backend | `EvidenceEndpointTests.ApprovedEvidence_DeleteReturnsForbidden` | covered |
| Evidence | UI shows approved evidence as locked | e2e | `testing/e2e/tests/a-core-learning-loop.spec.ts` (after approve + reload: no Mark/Feedback/Draft/Approve; read-only mark text visible) | covered |

## 7. Updates after approval

| feature | check | layer | existing test file | status |
|---------|-------|-------|--------------------|--------|
| Updates after approval | Approval updates SLP / diagnostics / gaps / mastery | backend | `we-platform/testing/EvidenceApprovalFlow.Tests/EvidenceApprovalHappyPathTests.cs`; reliability in `EvidenceApprovalReliabilityTests.cs`; consumers across diagnostic/gap/mastery/event-subscriber | linked |

## 8–10. Student home, teacher home / insights, interventions

| feature | check | layer | existing test file | status |
|---------|-------|-------|--------------------|--------|
| Student home | Core progress / assessments / feedback surfaces | e2e | `we-platform/testing/e2e/tests/a-core-learning-loop.spec.ts`, `h-student-wording.spec.ts`, `j-smoke.spec.ts` | partial |
| Student home | Empty states | unit | `apps/web-portal/src/lib/student-focus.test.ts` (empty headline); `apps/web-portal/src/app/student/page.test.tsx` (Today quiet + Skills/Next/Notes/Growth empty copy) | covered |
| Teacher home | Assigned classes / empty no-class state | unit / e2e | `apps/web-portal/src/app/teacher/page.test.tsx`; smoke in `j-smoke.spec.ts` | covered |
| Teacher insights | Class EI insights RBAC + aggregation | backend / unit | `ei-service/tests/EiEndpointTests.cs`; `apps/web-portal/src/app/teacher/classes/[classId]/page.test.tsx` (empty EI sections + EI-request failure fallback) | covered |
| Interventions | Create / list / status lifecycle / filters | backend / e2e | `intervention-service/tests/InterventionEndpointTests.cs` (status transitions, org list filter); `testing/e2e/tests/b-interventions.spec.ts` | covered |
| Interventions | UI empty state + list status filter | unit / e2e | `apps/web-portal/src/app/teacher/interventions/page.test.tsx` (empty + status filter); `testing/e2e/tests/b-interventions.spec.ts` (Active/Closed/Planned/All filter) | covered |

## 11. AI

| feature | check | layer | existing test file | status |
|---------|-------|-------|--------------------|--------|
| AI | Unsafe / PII / prohibited content blocked | backend | `ai-gateway-service/tests/GovernanceSafetyFilterTests.cs`, `AiGovernanceEndpointTests.cs` (`Complete_BlocksPiiInPromptVariables`, `Complete_BlocksProhibitedActions`) | covered |
| AI | Every call written to audit log under correct school/tenant | backend | `AiGovernanceEndpointTests.Complete_LogsSuccessfulRequestWithAuditMetadata`; `assessment-service/tests/AiFeedbackDraftEndpointTests.cs` (`SchoolBTeacher_AiFeedbackDraft_IsLoggedUnderSchoolBTenant`); `ai-gateway TenantIsolationEndpointTests` | covered |
| AI | Student never sees a draft | backend / e2e | `AiFeedbackDraftEndpointTests.Student_CannotRequestAiFeedbackDraft`; e2e `d-ai-feedback.spec.ts` (teacher draft → student after approve; draft text absent on student UI before approve) | covered |
| AI | Real provider retest (non-Mock end-to-end) | deferred | Mock adapter only until a real AI provider is integrated; Production refuses Mock unless `AiProvider:AllowMockInProduction=true`; portal shows “AI preview (sample text)” on Mock drafts | deferred |

## 12–13. Parent, messages, notifications

| feature | check | layer | existing test file | status |
|---------|-------|-------|--------------------|--------|
| Parent | Parent blocked from child not linked to them | backend | `organisation-service/tests/ParentWorkspaceEndpointTests.cs` (`Parent_CannotViewUnlinkedChildProgress`, `Parent_CannotViewOtherStudentsLinkedChildProgress`); intervention parent unlinked cases | covered |
| Messages | Parent ↔ teacher messaging scoped to student / class | backend / e2e | `communication-service/tests/CommunicationEndpointTests.cs`; `testing/e2e/tests/c-messages.spec.ts` | covered |
| Notifications | Mark as read | backend / e2e | `notification-service/tests/NotificationEndpointTests.cs` (`User_CanMarkNotificationAsRead`); e2e `c-messages.spec.ts` | covered |
| Notifications | Unread count | — | not required (absent from UX-001 / SP-001) | not required |
| Notifications | Email delivery (SMTP / real provider) | deferred | `MockEmailNotifier` only until a real email provider is integrated; Production refuses Mock unless `Email:AllowMockInProduction=true` | deferred |

## 14. Leadership

| feature | check | layer | existing test file | status |
|---------|-------|-------|--------------------|--------|
| Leadership | Dashboard loads / report generation | e2e | `we-platform/testing/e2e/tests/e-leadership.spec.ts` | partial |
| Leadership | Dashboard numbers match seeded source data (compute expected from seed) | backend | `organisation-service/tests/LeadershipDashboardEndpointTests.cs` (`SchoolLeader_DashboardNumbers_MatchComputedExpectationsFromSeededOrgClassData` — multi-class seed + `LeadershipDashboardAggregator` expected KPIs/year/class comparisons) | covered |

## 15. Analytics

| feature | check | layer | existing test file | status |
|---------|-------|-------|--------------------|--------|
| Analytics | `/api/v1/analytics` aggregates match known seeded events | backend | `reporting-service/tests/EffectivenessAnalyticsEndpointTests.cs`, `LongitudinalAnalyticsEndpointTests.cs` (seeded EDW facts; effectiveness asserts seed sums + school-average underperforming rule) | covered |
| Analytics | Charts handle empty data | e2e / unit | `apps/web-portal/src/components/effectiveness-charts.test.tsx`, `longitudinal-charts.test.tsx` (empty i18n copy); backend empty EDW: `SchoolLeader_EffectivenessAnalysis_ReturnsEmptyCollections_WhenNoEdwFacts`, `SchoolLeader_OrganisationLongitudinalAnalysis_ReturnsEmptyCollections_WhenNoEdwFacts` | covered |

## 16. Regional configuration and federation

| feature | check | layer | existing test file | status |
|---------|-------|-------|--------------------|--------|
| Federation | Only FederationAdmin can use federation endpoints | backend | `federation-service/tests/FederationEndpointTests.cs` (`SchoolAdmin_CannotAccessFederationEndpoints` incl. policies get/put; FederationAdmin happy paths) | covered |
| Regional configuration | School admins / leaders create schools and set policies | backend | School create: org `Admin_CanCrudOrganisationYearLevelAndClass` + federation `FederationAdmin_CreatesSchoolTenant_AndAssignsSchoolAdmin`; leaders/admins denied org create (`SchoolLeader_CannotCreateOrganisation`); policies: `SchoolLeader_ConfiguresAndTeacher_ReadsTenantConfiguration`, `SchoolAdmin_ConfiguresRegionalPolicies_ForOwnTenant`; federation policies FederationAdmin-only | covered |

## 17. National API

| feature | check | layer | existing test file | status |
|---------|-------|-------|--------------------|--------|
| National API | Missing API key → unauthorized | backend | `national-reporting-service/tests/NationalReportingEndpointTests.cs` (`MissingApiKey_IsUnauthorized`) | covered |
| National API | Invalid API key → unauthorized | backend | `NationalReportingEndpointTests.InvalidApiKey_IsUnauthorized` | covered |
| National API | Revoked / inactive API key → unauthorized | backend | `NationalReportingEndpointTests.InactiveApiKey_IsUnauthorized` | covered |
| National API | Rate limit returns 429 | backend | `NationalReportingEndpointTests.NationalEndpoints_AreRateLimited` | covered |
| National API | All seven aggregate endpoints | backend | 3 national (`enrollment`, `mastery-benchmarks`, `curriculum-coverage`) + 4 policy (`trends`, `equity`, `curriculum-effectiveness`, `intervention-impact`) in `NationalReportingEndpointTests` / `PolicyDashboardEndpointTests` | covered |
| National API | Small counts suppressed (min group size &lt; 5) | backend | `NationalReporting:MinimumGroupSize` (default 5); `CountCell` `{ value, suppressed }`; totals suppress when any child suppressed; tests in `NationalReportingEndpointTests` / `PolicyDashboardEndpointTests`; SP-001 Ch.22.10 | covered |
| National API | OpenAPI document matches the endpoints | backend | `NationalReportingEndpointTests.OpenApiDocumentation_IsPublishedAtApiV1Docs` (presence only; does not assert all seven paths) | partial |

## 18. Arabic

| feature | check | layer | existing test file | status |
|---------|-------|-------|--------------------|--------|
| Arabic | Pages render RTL | e2e / unit | `testing/e2e/tests/i-arabic.spec.ts` (student + teacher + parent); `apps/web-portal/src/components/language-switcher.test.tsx`; `i18n/i18n.test.ts` | partial (homes covered; not every admin page) |
| Arabic | No hard-coded English in source (locale scan) | unit | `i18n/i18n.test.ts` (en↔ar key lockstep; empty-state key coverage; scan for hard-coded English catalogue literals in `app/` + `components/`, allow-listed metadata) | covered |
| Arabic | API error codes map to translated messages | unit / backend | `i18n/i18n.test.ts` (all `errors.*` catalogue entries + `errorMessageKey`); identity `Login_WithInvalidCredentials_ReturnsTranslatableErrorCode`; assessment validation error code | covered |

## 19. Non-functional

| feature | check | layer | existing test file | status |
|---------|-------|-------|--------------------|--------|
| Load | k6 (or similar) against compose main endpoints; report p95 vs 500 ms | manual / script | Report: `docs/validation/nfr-report.md` §1. Compose-wide k6 still missing. Unit SLA executed: `national-reporting-service/tests/PlatformSlaLoadTests.cs` (enrollment p95=12ms, trends p95=1ms, max≤500ms); portal `policy-dashboards.sla.test.ts` | partial |
| Security | Dependency vulnerability scan (`dotnet list package --vulnerable`, `pnpm audit`) | manual / script | `docs/validation/nfr-report.md` §2 — .NET clean except test-only SSH.NET High; portal `next@15.5.27` + postcss `8.5.28` via `.pnpmfile.cjs`; `pnpm audit --prod` 0 critical/high | covered |
| Security | Basic OWASP header and CORS checks | manual / script | `docs/validation/nfr-report.md` §3 — CORS `WebPortal` → `localhost:3000`; OWASP security headers absent (no product change this batch) | covered |
| Security | Confirm no secrets committed | manual / script | `docs/validation/nfr-report.md` §4 — no committed `.env`/keys; local-dev `Password=we_dev` + JWT placeholders only | covered |
| Backup / restore | Dump + restore all service DBs then `pnpm e2e` | manual / script | Checklist + dump probe in `docs/validation/nfr-report.md` §5 — 19/19 `pg_dump` OK; restore + e2e not executed | partial |
| Health | Every compose service `/health` returns OK | manual / script | Live sample 21/21 OK (`nfr-report.md` §6); unit: `examples/sample-service/tests/HealthEndpointTests.cs`; no compose-wide health script / app Docker healthchecks | covered |

---

## Gap summary (for Step 2 ordering)

Planned fill order: **2 → 6 → 17 → 1 → 11 → 12–13 → 3 → 4 → 5 → 14 → 15 → 16 → 18 → 8–10 → 19**.

| Priority batch | Highest-impact gaps |
|----------------|---------------------|
| 2 School isolation | Exhaustive table-driven isolation (batch 2 + 2b complete: endpoint guard, IgnoreQueryFilters allow-list, fail-closed filter, federation A↔B) |
| 6 Evidence | e2e UI locked; re-verify PUT/DELETE (fix product if editable) |
| 17 National | Small-count suppression + OpenAPI path completeness; note SP-001 missing min group size |
| 1 Sign-in | Role×API matrix remains partial (portal + identity admin covered) |
| 11 AI | Student never sees draft; real provider retest deferred |
| 12–13 | Unread count not required; email delivery deferred |
| 3 Org | Duplicates; delete class with students; explicit assign-leader assertion | ✓ filled |
| 4 Curriculum | Dedicated variant-links assertion if incomplete | ✓ filled |
| 5 Assessments | Edit/delete after publish tests; clarify late-submit product rule | ✓ filled |
| 14 Leadership | Seeded expected values vs dashboard | ✓ filled |
| 15 Analytics | Empty chart behaviour; tighten seed aggregate assertions | ✓ filled |
| 16 Federation / config | School-admin policy/school create completeness | ✓ filled |
| 18 Arabic | Full-page RTL + source literal scan + error catalogue | ✓ filled (RTL homes; catalogue scan; errors.*) |
| 8–10 Homes / interventions | Empty states, filters, status changes in e2e | ✓ filled |
| 19 Non-functional | NFR report written; unit SLA + audits + health sample done; k6 + restore/e2e + header middleware still open |

## Existing suite anchors (baseline)

| Suite | Location | Role |
|-------|----------|------|
| Backend unit/integration | `we-platform/WePlatform.sln` (`dotnet test`) | Per-service API + engine tests |
| Cross-service Testcontainers | `we-platform/testing/EvidenceApprovalFlow.Tests` | Approval → SLP/EI pipeline |
| Web portal unit | `we-platform/apps/web-portal` (`pnpm test`) | i18n, teacher home, focus helpers |
| Playwright | `we-platform/testing/e2e` (`pnpm e2e`) | Core loop, interventions, messages, AI, leadership, authority, access, wording, Arabic, smoke |
| Demo users | Identity `IdentityDataSeeder` (School A / School B, all roles) | Seeded for e2e and isolation scenarios |
