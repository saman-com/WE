# IgnoreQueryFilters audit (batch 2b)

Inventory of every `IgnoreQueryFilters()` call under `we-platform/services/*/src` outside shared tenancy helpers.  
Startup backfill resolvers in `Program.cs` are included in the allow-list but marked as backfill.  
Shared helper: `shared/tenancy/TenantDbContextExtensions.BackfillTenantIdsAsync` (not under services).

Updated: 2026-10-05 (batch 2b).

Guard: `we-platform/testing/SchoolIsolation.Tests` + allow-list `ignore-query-filters-allowlist.json`.

| file:line | why bypassed | replacement check | verdict |
|-----------|--------------|-------------------|---------|
| `assessment-service/.../AssessmentEndpoints.cs` (`LoadAssessmentAsync`) | Load assessment by id across possibly-stale EF filter capture | `TenantAccess.ValidateEntityAccess` on every caller (incl. Admin mutators) | OK (after batch 2b fix) |
| `assessment-service/.../AiFeedbackDraftService.cs` | Finalize audit by id | Teacher match + `TenantAccess.ValidateEntityAccess` | OK (after batch 2b) |
| `communication-service/.../CommunicationEndpoints.cs:196` | Load conversation by participant triple | `TenantAccess.ValidateEntityAccess` + `TenantId == tenantContext.TenantId` | OK |
| `communication-service/.../CommunicationEndpoints.cs:252` | Teacher inbox with explicit tenant predicate | `m.TenantId == tenantId && m.TeacherUserId == userId` | OK |
| `communication-service/.../CommunicationEndpoints.cs:257` | Parent inbox with explicit tenant predicate | `m.TenantId == tenantId && m.ParentUserId == userId` | OK |
| `curriculum-service/.../CurriculumEndpoints.cs:183` | Inherit regional parent tree (cross-tenant by design) | Caller requires `request.OrganisationId == tenantContext.TenantId`; parent must be `Scope == Regional` | OK |
| `curriculum-service/.../CurriculumEndpoints.cs:343` | Get curriculum by id | `TenantAccess.ValidateEntityAccess` | OK |
| `curriculum-service/.../CurriculumEndpoints.cs:365` | Update curriculum by id | `TenantAccess.ValidateEntityAccess` | OK |
| `curriculum-service/.../CurriculumEndpoints.cs:402` | Delete curriculum by id | `TenantAccess.ValidateEntityAccess` | OK |
| `curriculum-service/.../CurriculumEndpoints.cs:431` | Get curriculum tree by id | `TenantAccess.ValidateEntityAccess` | OK |
| `curriculum-service/.../CurriculumEndpoints.cs:1309` | `FindSubjectAsync` | `TenantAccess.ValidateEntityAccess` | OK |
| `curriculum-service/.../CurriculumEndpoints.cs:1327` | `FindUnitAsync` | `TenantAccess.ValidateEntityAccess` | OK |
| `curriculum-service/.../CurriculumEndpoints.cs:1349` | `FindTopicAsync` | `TenantAccess.ValidateEntityAccess` | OK |
| `curriculum-service/.../CurriculumEndpoints.cs:1372` | `FindLearningObjectiveAsync` | `TenantAccess.ValidateEntityAccess` | OK |
| `curriculum-service/.../CurriculumEndpoints.cs:1396` | `FindMicroSkillAsync` | `TenantAccess.ValidateEntityAccess` | OK |
| `curriculum-service/.../CurriculumEndpoints.cs:1416` | `FindCurriculumAsync` | `TenantAccess.ValidateEntityAccess` | OK |
| `curriculum-service/.../CurriculumEndpoints.cs:1500` | School-variant scope check | Prior `Find*` helpers already applied `TenantAccess` | OK |
| `diagnostic-service/.../DiagnosticEndpoints.cs:50` | Distinguish empty vs wrong-tenant diagnostics | Forbid when all rows foreign; filter `TenantId == tenantId` | OK |
| `ei-service/.../AiSummaryDraftService.cs:127` | Finalize audit by id | `TeacherUserId` match + `TenantAccess.ValidateEntityAccess` | OK |
| `evidence-service/.../EvidenceEndpoints.cs:309` | `LoadEvidenceAsync` helper | Callers apply `TenantAccess.ValidateEntityAccess` | OK |
| `intervention-service/.../InterventionEndpoints.cs:129` | Student intervention list | Forbid when all foreign; filter `TenantId == tenantId` | OK |
| `intervention-service/.../InterventionEndpoints.cs:255` | Get intervention by id | `TenantAccess.ValidateEntityAccess` | OK |
| `intervention-service/.../InterventionEndpoints.cs:289` | Patch intervention by id | `TenantAccess.ValidateEntityAccess` | OK |
| `learning-gap-service/.../GapEndpoints.cs:49` | Student gaps list | Forbid when all foreign; filter `TenantId == tenantId` | OK |
| `mastery-service/.../MasteryEndpoints.cs:50` | Student mastery list | Forbid when all foreign; filter `TenantId == tenant` | OK |
| `mastery-service/.../MasteryEndpoints.cs:108` | Parent mastery summary | Forbid when all foreign; filter `TenantId == tenant` | OK |
| `notification-service/.../NotificationEndpoints.cs:47` | Mark-read by id | `TenantAccess.ValidateEntityAccess` + recipient match | OK |
| `organisation-service/.../OrganisationEndpoints.cs:131` | Get organisation by id | `TenantAccess.ValidateEntityAccess` | OK |
| `organisation-service/.../OrganisationEndpoints.cs:165` | Update organisation by id | `TenantAccess.ValidateEntityAccess` | OK |
| `organisation-service/.../OrganisationEndpoints.cs:206` | Delete organisation by id | `TenantAccess.ValidateEntityAccess` | OK |
| `organisation-service/.../OrganisationEndpoints.cs:891` | `FindOrganisationAsync` | `TenantAccess.ValidateEntityAccess` | OK |
| `organisation-service/.../OrganisationEndpoints.cs:910` | `FindYearLevelAsync` | `TenantAccess.ValidateEntityAccess` | OK |
| `organisation-service/.../OrganisationEndpoints.cs:930` | `FindClassAsync` | `TenantAccess.ValidateEntityAccess` | OK |
| `organisation-service/.../ParentWorkspaceEndpoints.cs:56` | Verify student enrolled in caller school | `enrollment.TenantId == tenantId` | OK |
| `reporting-service/.../ReportEndpoints.cs:150` | Get report gate before generator | `TenantAccess.ValidateEntityAccess` | OK |
| `reporting-service/.../ReportEndpoints.cs:199` | Export PDF gate before generator | `TenantAccess.ValidateEntityAccess` | OK |
| `reporting-service/.../DependencyInjection.cs:165` | `ReportGenerator.GetReportAsync` by id | `TenantAccess.ValidateEntityAccess` (batch 2b) | OK (after batch 2b fix) |
| `reporting-service/.../DependencyInjection.cs:179` | `ReportGenerator.ExportReportPdfAsync` by id | `TenantAccess.ValidateEntityAccess` (batch 2b) | OK (after batch 2b fix) |
| `student-learning-service/.../StudentLearningEndpoints.cs:52` | Get profile by student id | `TenantAccess.ValidateEntityAccess` | OK |
| `student-learning-service/.../StudentLearningEndpoints.cs:100` | Get profile summary | `TenantAccess.ValidateEntityAccess` | OK |
| `student-learning-service/.../StudentLearningEndpoints.cs:228` | Record evidence onto profile | `TenantAccess.ValidateEntityAccess` | OK |
| `reporting-service/.../LongitudinalAnalyticsQuery.cs` (DimTime joins) | Shared calendar dim uses `DefaultTenant.Id` | `IgnoreQueryFilters` on `DimTimes` only | OK (added batch 2b) |
| `edw-ingest-service/.../EdwIngestProcessor.cs` (DimTime) | Shared calendar dim under org `SetTenant` | `IgnoreQueryFilters` on `DimTimes` only | OK (added batch 2b) |

## Startup backfill (Program.cs)

All `Program.cs` `IgnoreQueryFilters` usages resolve parent entities while `BackfillTenantIdsAsync` runs with no request tenant. Allow-listed as `startup-backfill`.

## Leaks found and fixed in batch 2b

| leak | fix |
|------|-----|
| Assessment Admin mutators used `LoadAssessmentAsync` + `EvaluateTeacherClassAccessAsync` Admin bypass without `TenantAccess` | Add `TenantAccess` on every mutator/reader after load; Admin A↔B isolation test |
| Unused `LoadSubmissionAsync` bypassed filters with no check | Removed |
| `ReportGenerator` loaded reports by id with no tenant check | Inject `ITenantContext` + `TenantAccess.ValidateEntityAccess` |

## Fail-closed follow-ups (batch 2b)

Changing the global filter to require a tenant exposed code that relied on the open filter:

| breakage | fix |
|----------|-----|
| EF model cached a closed-over `ITenantContext` so filters did not track the request tenant | Bind `TenantAwareDbContext.CurrentTenantId` in `HasQueryFilter` |
| Mastery/gap/diagnostic/SLP/EDW processors queried before `SetTenant` | `SetTenant(evidence.OrganisationId)` inside each processor |
| Notification creator dedup with no tenant | `SetTenant(tenantId)` in `NotificationCreator` |
| Shared `DimTime` rows under `DefaultTenant` invisible under school tenant | Allow-listed `IgnoreQueryFilters` on DimTime lookups/joins |
| Parent/student JWTs in tests using `DefaultTenant` against school-scoped rows | Tests pass the school `organisationId` as JWT tenant |
