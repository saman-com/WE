# WE Platform feature test matrix

Coverage map for delivery feature checks. Paths are relative to the repo root (`edu_app/`).  
Status: `covered` | `partial` | `missing` | `linked`.

Updated: 2026-10-05 (batch 2 school isolation complete).

---

## 1. Sign-in

| feature | check | layer | existing test file | status |
|---------|-------|-------|--------------------|--------|
| Sign-in | Login succeeds for valid credentials | backend | `we-platform/services/identity-service/tests/AuthEndpointTests.cs` (`Login_WithValidCredentials_ReturnsJwt`) | covered |
| Sign-in | Login fails for invalid credentials | backend | `we-platform/services/identity-service/tests/AuthEndpointTests.cs` (`Login_WithInvalidCredentials_ReturnsUnauthorized`) | covered |
| Sign-in | `/me` with valid token returns user and roles | backend | `we-platform/services/identity-service/tests/AuthEndpointTests.cs` (`Me_WithValidToken_ReturnsUserAndRoles`) | covered |
| Sign-in | `/me` without token returns 401 | backend | `we-platform/services/identity-service/tests/AuthEndpointTests.cs` (`Me_WithoutToken_ReturnsUnauthorized`) | covered |
| Sign-in | `/admin` requires Admin role (403 otherwise) | backend | `we-platform/services/identity-service/tests/AuthEndpointTests.cs` (`AdminEndpoint_WithoutAdminRole_ReturnsForbidden`, `AdminEndpoint_WithAdminRole_ReturnsOk`) | covered |
| Sign-in | Expired token rejected on protected endpoints | backend | missing | missing |
| Sign-in | Each role gets 403 on other roles' endpoints (matrix across portal routes / APIs) | e2e | `we-platform/testing/e2e/tests/g-access-control.spec.ts` (role×path blocked cases; not every API) | partial |
| Sign-in | Token survives page refresh | e2e | missing (auth stores token in localStorage via setup helpers only) | missing |
| Sign-in | Logout clears session and blocks protected pages | e2e | missing (no logout flow assertion; identity has no logout API test) | missing |

## 2. School isolation

| feature | check | layer | existing test file | status |
|---------|-------|-------|--------------------|--------|
| School isolation | School B user with School A org/class/student/resource IDs gets 403/404 and never data — **every** ID-bearing endpoint, table-driven per service or cross-service suite | backend | Source-scanned catalog + coverage guard: `we-platform/testing/SchoolIsolation.Tests/` (`EndpointCoverageGuardTests`, `isolation-catalog.json`). Table-driven both-direction probes in each service’s `TenantIsolationEndpointTests.cs` (incl. federation). Consumer isolation covered in diagnostic/gaps/mastery/learning/edw/notification suites. e2e spot-checks remain in `testing/e2e/tests/g-access-control.spec.ts`. | covered |

Services with at least one isolation test today: ai-gateway, assessment, communication, configuration, curriculum, diagnostic, edw-ingest, ei, evidence, identity, intervention, learning-gap, mastery, notification, organisation, reporting, student-learning.

## 3. Organisation setup

| feature | check | layer | existing test file | status |
|---------|-------|-------|--------------------|--------|
| Organisation setup | Create / edit / delete schools, year levels, classes | backend | `we-platform/services/organisation-service/tests/OrganisationEndpointTests.cs` (`Admin_CanCrudOrganisationYearLevelAndClass`) | covered |
| Organisation setup | Assign teachers | backend | `we-platform/services/organisation-service/tests/OrganisationEndpointTests.cs` (`Admin_CanAssignTeacherAndEnrollStudent`) | covered |
| Organisation setup | Assign school leaders | backend | `we-platform/services/organisation-service/tests/LeadershipDashboardEndpointTests.cs` (helper `AssignSchoolLeaderAsync` used by dashboard tests) | partial |
| Organisation setup | Enrol students | backend | `we-platform/services/organisation-service/tests/OrganisationEndpointTests.cs` (`Admin_CanAssignTeacherAndEnrollStudent`, `EnrollStudent_AutoCreatesStudentLearningProfile`) | covered |
| Organisation setup | Link parents to students | backend | `we-platform/services/organisation-service/tests/ParentWorkspaceEndpointTests.cs` (`Admin_LinksParentToStudent`) | covered |
| Organisation setup | Duplicate school / year / class / enrolment / parent-link rejected | backend | missing | missing |
| Organisation setup | Deleting a class that has students is rejected or handled safely | backend | missing (delete tested only on empty class in CRUD happy path) | missing |

## 4. Curriculum

| feature | check | layer | existing test file | status |
|---------|-------|-------|--------------------|--------|
| Curriculum | Full hierarchy (subject → unit → LO → micro-skill) | backend | `we-platform/services/curriculum-service/tests/CurriculumEndpointTests.cs`, `LearningObjectiveEndpointTests.cs` | covered |
| Curriculum | `/tree` returns navigable hierarchy | backend | `CurriculumEndpointTests.Tree_ReturnsNavigableSubjectUnitHierarchy`; `LearningObjectiveEndpointTests.Tree_ReturnsLearningObjectiveMicroSkillHierarchyUnderUnit` | covered |
| Curriculum | Inherit regional variant into school | backend | `we-platform/services/curriculum-service/tests/CurriculumVariantEndpointTests.cs` (`School_InheritsRegionalCurriculumVariant_WithClonedTreeAndSourceLinks`) | covered |
| Curriculum | Variant-links endpoint / source links | backend | `CurriculumVariantEndpointTests` (calls `/variant-links` in assessment-link scenario; inherit asserts source IDs) | partial |
| Curriculum | School override must not change parent (regional) curriculum | backend | `CurriculumVariantEndpointTests.School_CanOverrideUnitAndLearningObjective_WithoutAffectingRegionalOrOtherSchools` | covered |

## 5. Assessments

| feature | check | layer | existing test file | status |
|---------|-------|-------|--------------------|--------|
| Assessments | Drafts hidden from students | backend | `we-platform/services/assessment-service/tests/AssessmentEndpointTests.cs` (`Student_CannotSeeDraftAssessments`, `Student_CanListPublishedAssessments_ForEnrolledClassOnly`) | covered |
| Assessments | Editing after publishing rejected | backend | missing (API returns Conflict for non-draft; no dedicated test) | missing |
| Assessments | Deleting after publishing rejected | backend | missing (API returns Conflict for non-draft; no dedicated test) | missing |
| Assessments | One submission per student | backend | `we-platform/services/assessment-service/tests/SubmissionEndpointTests.cs` (`Student_CannotSubmitTwice`) | covered |
| Assessments | Submitting after due date | backend | `SubmissionEndpointTests.Student_LateSubmission_IsAcceptedAndMarkedLate` (late accepted + flagged; confirm product intent if reject expected) | partial |

## 6. Evidence

| feature | check | layer | existing test file | status |
|---------|-------|-------|--------------------|--------|
| Evidence | Approved evidence rejects PUT | backend | `we-platform/services/evidence-service/tests/EvidenceEndpointTests.cs` (`ApprovedEvidence_PutReturnsForbidden`) | covered |
| Evidence | Approved evidence rejects DELETE | backend | `EvidenceEndpointTests.ApprovedEvidence_DeleteReturnsForbidden` | covered |
| Evidence | UI shows approved evidence as locked | e2e | missing | missing |

## 7. Updates after approval

| feature | check | layer | existing test file | status |
|---------|-------|-------|--------------------|--------|
| Updates after approval | Approval updates SLP / diagnostics / gaps / mastery | backend | `we-platform/testing/EvidenceApprovalFlow.Tests/EvidenceApprovalHappyPathTests.cs`; reliability in `EvidenceApprovalReliabilityTests.cs`; consumers across diagnostic/gap/mastery/event-subscriber | linked |

## 8–10. Student home, teacher home / insights, interventions

| feature | check | layer | existing test file | status |
|---------|-------|-------|--------------------|--------|
| Student home | Core progress / assessments / feedback surfaces | e2e | `we-platform/testing/e2e/tests/a-core-learning-loop.spec.ts`, `h-student-wording.spec.ts`, `j-smoke.spec.ts` | partial |
| Student home | Empty states | e2e / unit | `apps/web-portal/src/lib/student-focus.test.ts` (empty headline); no browser empty-state coverage | partial |
| Teacher home | Assigned classes / empty no-class state | unit / e2e | `apps/web-portal/src/app/teacher/page.test.tsx`; smoke in `j-smoke.spec.ts` | partial |
| Teacher insights | Class EI insights RBAC + aggregation | backend / e2e | `ei-service/tests/EiEndpointTests.cs`; class UI empty copy exists but no dedicated e2e empty/filter cases | partial |
| Interventions | Create / list / status lifecycle / filters | backend / e2e | `intervention-service/tests/InterventionEndpointTests.cs` (status transitions, org list filter); `testing/e2e/tests/b-interventions.spec.ts` | partial |
| Interventions | UI empty state + list status filter | e2e | missing (UI has filters/empty copy; no automated browser check) | missing |

## 11. AI

| feature | check | layer | existing test file | status |
|---------|-------|-------|--------------------|--------|
| AI | Unsafe / PII / prohibited content blocked | backend | `ai-gateway-service/tests/GovernanceSafetyFilterTests.cs`, `AiGovernanceEndpointTests.cs` (`Complete_BlocksPiiInPromptVariables`, `Complete_BlocksProhibitedActions`) | covered |
| AI | Every call written to audit log under correct school/tenant | backend | `AiGovernanceEndpointTests.Complete_LogsSuccessfulRequestWithAuditMetadata`; `assessment-service/tests/AiFeedbackDraftEndpointTests.cs` (`SchoolBTeacher_AiFeedbackDraft_IsLoggedUnderSchoolBTenant`); `ai-gateway TenantIsolationEndpointTests` | covered |
| AI | Student never sees a draft | backend / e2e | `AiFeedbackDraftEndpointTests.Student_CannotRequestAiFeedbackDraft`; e2e `d-ai-feedback.spec.ts` (teacher draft → student after approve). No explicit student-UI assertion that draft text is absent before finalise | partial |

## 12–13. Parent, messages, notifications

| feature | check | layer | existing test file | status |
|---------|-------|-------|--------------------|--------|
| Parent | Parent blocked from child not linked to them | backend | `organisation-service/tests/ParentWorkspaceEndpointTests.cs` (`Parent_CannotViewUnlinkedChildProgress`, `Parent_CannotViewOtherStudentsLinkedChildProgress`); intervention parent unlinked cases | covered |
| Messages | Parent ↔ teacher messaging scoped to student / class | backend / e2e | `communication-service/tests/CommunicationEndpointTests.cs`; `testing/e2e/tests/c-messages.spec.ts` | covered |
| Notifications | Mark as read | backend / e2e | `notification-service/tests/NotificationEndpointTests.cs` (`User_CanMarkNotificationAsRead`); e2e `c-messages.spec.ts` | covered |
| Notifications | Unread count | — | not required (absent from UX-001 / SP-001) | not required |

## 14. Leadership

| feature | check | layer | existing test file | status |
|---------|-------|-------|--------------------|--------|
| Leadership | Dashboard loads / report generation | e2e | `we-platform/testing/e2e/tests/e-leadership.spec.ts` | partial |
| Leadership | Dashboard numbers match seeded source data (compute expected from seed) | backend | missing (`LeadershipDashboardEndpointTests` uses fakes/mocks, not Identity/demo seed arithmetic) | missing |

## 15. Analytics

| feature | check | layer | existing test file | status |
|---------|-------|-------|--------------------|--------|
| Analytics | `/api/v1/analytics` aggregates match known seeded events | backend | `reporting-service/tests/EffectivenessAnalyticsEndpointTests.cs`, `LongitudinalAnalyticsEndpointTests.cs` (seeded EDW facts) | partial |
| Analytics | Charts handle empty data | e2e / unit | missing (UI empty strings exist; no automated empty-chart assertion) | missing |

## 16. Regional configuration and federation

| feature | check | layer | existing test file | status |
|---------|-------|-------|--------------------|--------|
| Federation | Only FederationAdmin can use federation endpoints | backend | `federation-service/tests/FederationEndpointTests.cs` (`SchoolAdmin_CannotAccessFederationEndpoints`, FederationAdmin happy paths) | covered |
| Regional configuration | School admins / leaders create schools and set policies | backend | `configuration-service/tests/ConfigurationEndpointTests.cs`; federation provisioning + `FederationAdmin_ManagesFederationPolicies`; school create via organisation/federation tests | partial |

## 17. National API

| feature | check | layer | existing test file | status |
|---------|-------|-------|--------------------|--------|
| National API | Missing API key → unauthorized | backend | `national-reporting-service/tests/NationalReportingEndpointTests.cs` (`MissingApiKey_IsUnauthorized`) | covered |
| National API | Invalid API key → unauthorized | backend | `NationalReportingEndpointTests.InvalidApiKey_IsUnauthorized` | covered |
| National API | Revoked / inactive API key → unauthorized | backend | `NationalReportingEndpointTests.InactiveApiKey_IsUnauthorized` | covered |
| National API | Rate limit returns 429 | backend | `NationalReportingEndpointTests.NationalEndpoints_AreRateLimited` | covered |
| National API | All seven aggregate endpoints | backend | 3 national (`enrollment`, `mastery-benchmarks`, `curriculum-coverage`) + 4 policy (`trends`, `equity`, `curriculum-effectiveness`, `intervention-impact`) in `NationalReportingEndpointTests` / `PolicyDashboardEndpointTests` | covered |
| National API | Small counts suppressed (min group size &lt; 5) | backend | planned batch 17: `NationalReporting:MinimumGroupSize` (default 5); hidden cells `{ value: null, suppressed: true }`; totals must not allow subtraction recovery; add rule to SP-001 Ch.22 | missing |
| National API | OpenAPI document matches the endpoints | backend | `NationalReportingEndpointTests.OpenApiDocumentation_IsPublishedAtApiV1Docs` (presence only; does not assert all seven paths) | partial |

## 18. Arabic

| feature | check | layer | existing test file | status |
|---------|-------|-------|--------------------|--------|
| Arabic | Pages render RTL | e2e / unit | `testing/e2e/tests/i-arabic.spec.ts` (student + teacher); `apps/web-portal/src/components/language-switcher.test.tsx`; `i18n/i18n.test.ts` | partial (not every page) |
| Arabic | No hard-coded English in source (locale scan) | unit / script | missing (no source scan for untranslated literals; i18n tests cover keys/helpers only) | missing |
| Arabic | API error codes map to translated messages | unit / backend | `i18n/i18n.test.ts` (`API error code translation`); identity `Login_WithInvalidCredentials_ReturnsTranslatableErrorCode`; assessment validation error code — not a full code↔locale catalogue | partial |

## 19. Non-functional

| feature | check | layer | existing test file | status |
|---------|-------|-------|--------------------|--------|
| Load | k6 (or similar) against compose main endpoints; report p95 vs 500 ms | manual / script | missing as compose-wide k6; unit-style concurrent SLA only in `national-reporting-service/tests/PlatformSlaLoadTests.cs` | missing |
| Security | Dependency vulnerability scan (`dotnet list package --vulnerable`, `pnpm audit`) | manual / script | missing | missing |
| Security | Basic OWASP header and CORS checks | manual / script | missing | missing |
| Security | Confirm no secrets committed | manual / script | missing | missing |
| Backup / restore | Dump + restore all service DBs then `pnpm e2e` | manual / script | missing | missing |
| Health | Every compose service `/health` returns OK | manual / script | sample only: `examples/sample-service/tests/HealthEndpointTests.cs`; no compose-wide health script | missing |

---

## Gap summary (for Step 2 ordering)

Planned fill order: **2 → 6 → 17 → 1 → 11 → 12–13 → 3 → 4 → 5 → 14 → 15 → 16 → 18 → 8–10 → 19**.

| Priority batch | Highest-impact gaps |
|----------------|---------------------|
| 2 School isolation | Exhaustive table-driven isolation (or cross-service suite); fix any cross-school leaks immediately |
| 6 Evidence | e2e UI locked; re-verify PUT/DELETE (fix product if editable) |
| 17 National | Small-count suppression + OpenAPI path completeness; note SP-001 missing min group size |
| 1 Sign-in | Expired token, refresh persistence, logout |
| 11 AI | Student never sees draft text in UI before approve |
| 12–13 | Unread count (API + UI/e2e) |
| 3 Org | Duplicates; delete class with students; explicit assign-leader assertion |
| 4 Curriculum | Dedicated variant-links assertion if incomplete |
| 5 Assessments | Edit/delete after publish tests; clarify late-submit product rule |
| 14 Leadership | Seeded expected values vs dashboard |
| 15 Analytics | Empty chart behaviour; tighten seed aggregate assertions |
| 16 Federation / config | School-admin policy/school create completeness |
| 18 Arabic | Full-page RTL + source literal scan + error catalogue |
| 8–10 Homes / interventions | Empty states, filters, status changes in e2e |
| 19 Non-functional | k6, vuln scan, headers/CORS/secrets, backup/restore, compose health — scripts + report |

## Existing suite anchors (baseline)

| Suite | Location | Role |
|-------|----------|------|
| Backend unit/integration | `we-platform/WePlatform.sln` (`dotnet test`) | Per-service API + engine tests |
| Cross-service Testcontainers | `we-platform/testing/EvidenceApprovalFlow.Tests` | Approval → SLP/EI pipeline |
| Web portal unit | `we-platform/apps/web-portal` (`pnpm test`) | i18n, teacher home, focus helpers |
| Playwright | `we-platform/testing/e2e` (`pnpm e2e`) | Core loop, interventions, messages, AI, leadership, authority, access, wording, Arabic, smoke |
| Demo users | Identity `IdentityDataSeeder` (School A / School B, all roles) | Seeded for e2e and isolation scenarios |
