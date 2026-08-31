# Phase 6 Advanced Analytics Validation Gate Report

| Field | Value |
|-------|-------|
| **Gate** | Phase 6 Advanced Analytics (issues 033–036) |
| **Reference** | EP-001 §18.16, EP-001 §18.10, SP-001 Ch.18 |
| **Issue** | `issues/037-phase6-validation-gate.md` |
| **Validation date** | 2026-08-31 |
| **Validator** | Platform Engineering (automated gate review) |
| **Overall result** | **PASS — Phase 6 complete** |

## Scope

This gate validates Advanced Analytics features delivered across issues 033–036:

| Issue | Deliverable |
|-------|-------------|
| 033 | Standard reporting module — class progress and school summary reports with PDF export |
| 034 | EDW ingest pipeline — async domain-event subscribers writing to analytics store |
| 035 | Longitudinal learning analysis — mastery trends, gap history, intervention outcomes |
| 036 | Curriculum and intervention effectiveness dashboards for school leaders |

---

## 1. Educational Validation

**Result: PASS**

Reports and analytics support real decision-making for teachers and school leaders.

| Capability | Evidence |
|------------|----------|
| Teacher class progress report (mastery, gaps, assessments) | `ReportingEndpointTests.Teacher_CanGenerateClassProgressReport_WithSeedData` — report includes mastery distribution, active learning gaps, and assessment summary; portal `/teacher/classes/[classId]` generates report and PDF export |
| School leader school summary report | `ReportingEndpointTests.SchoolLeader_CanGenerateSchoolSummaryReport` — KPIs, year-level summaries, class comparisons; portal `/leadership` generates on-demand school summary with PDF download |
| Student longitudinal mastery trend | `LongitudinalAnalyticsEndpointTests.Teacher_CanViewStudentLongitudinalAnalysis_WithMultiPeriodEdwData` — three-period cumulative mastery trend; portal `/students/[studentUserId]/profile` shows `MasteryTrendChart` for teacher viewers |
| Gap history timeline | Same test — gap opened/closed events derived from intervention facts; portal `GapHistoryTimeline` component |
| Intervention outcome tracking | Same test — intervention outcomes by status over time; portal `InterventionOutcomesTimeline` component |
| School-wide longitudinal view | `LongitudinalAnalyticsEndpointTests.SchoolLeader_CanViewOrganisationLongitudinalAnalysis_WithMultiPeriodEdwData` — per-student summaries and school mastery trend; portal `/leadership` longitudinal section |
| Curriculum effectiveness (underperforming areas) | `EffectivenessAnalyticsEndpointTests.SchoolLeader_CanViewEffectivenessAnalysis_WithKnownEdwSeedData` — mastery rates per subject/unit with `IsUnderperforming` flag; portal `CurriculumEffectivenessTable` |
| Intervention effectiveness by type | Same test — success rates by intervention type (`GuidedPractice`, `OneToOne`); portal `InterventionEffectivenessTable` |

**Web portal pages verified:**

- Teacher class report: `/teacher/classes/[classId]` (generate class progress report → PDF download)
- Student longitudinal profile: `/students/[studentUserId]/profile` (mastery trend, gap history, intervention outcomes)
- Leadership dashboard: `/leadership` (school summary report, longitudinal analysis, curriculum & intervention effectiveness)

**Blockers:** None.

---

## 2. Technical Validation

**Result: PASS**

EDW ingest is reliable; reports and analytics APIs generate correctly end-to-end.

| Criterion | Status | Evidence |
|-----------|--------|----------|
| EDW subscribes to key domain events | PASS | `EvidenceCreatedConsumer`, `AssessmentApprovedConsumer`, `InterventionCreatedConsumer` in `edw-ingest-service`; consumer tests verify MassTransit in-memory delivery |
| Events written to separate analytics store | PASS | `EdwDbContext` with `EvidenceFacts`, `AssessmentFacts`, `InterventionFacts`, `DimTimes`; PostgreSQL schema in `we-platform/databases/edw/` |
| Operational services unaffected (async) | PASS | Ingest is event-driven via MassTransit consumers; no synchronous coupling from operational services to EDW |
| Analytics schema supports fact tables | PASS | Star schema: `fact_evidence`, `fact_assessment`, `fact_intervention`, `dim_time`; V002 adds curriculum/effectiveness columns |
| Ingest is idempotent | PASS | `EdwIngestProcessorTests.ProcessingSameEventTwice_DoesNotDuplicateFacts`; processor checks `EventId` before insert |
| Batch ingest supported | PASS | `EdwIngestProcessorTests.ProcessEvidenceBatch_PersistsMultipleFactsInOneTransaction` |
| Teacher class report from EI (not duplicated) | PASS | `ReportEndpoints` calls `IEiInsightsClient` and `IClassDashboardClient`; test verifies EI mastery/gap data in report content |
| School summary from leadership EI data | PASS | `SchoolLeader_CanGenerateSchoolSummaryReport` uses `ISchoolSummaryClient` for KPIs and drill-down |
| PDF export on demand | PASS | `GeneratedReport_CanExportAsPdf` — valid `%PDF` response via QuestPDF exporter |
| Longitudinal queries from EDW | PASS | `LongitudinalAnalyticsQuery` reads `EdwAnalyticsDbContext`; no operational DB dependency |
| Effectiveness queries from EDW | PASS | `EffectivenessAnalyticsQuery` aggregates `EvidenceFacts` and `InterventionFacts` in analytics store |
| Role-based access on reports | PASS | Teacher/school-leader allowed paths tested; student and unassigned actors receive 403 |
| Leadership-only effectiveness dashboard | PASS | `Teacher_CannotViewEffectivenessAnalysis`; `SchoolLeader_CannotViewEffectivenessAnalysis_ForUnassignedOrganisation` |
| Backend build | PASS | `dotnet build WePlatform.sln` — 0 warnings, 0 errors |
| Backend tests | PASS | 242/242 tests passing (`dotnet test WePlatform.sln`) |

**Phase 6 test breakdown:**

| Service | Phase 6 tests | Result |
|---------|---------------|--------|
| reporting-service (reports) | 6 | Pass |
| reporting-service (longitudinal) | 5 | Pass |
| reporting-service (effectiveness) | 3 | Pass |
| edw-ingest-service (processor + consumers) | 8 | Pass |

**Blockers:** None.

---

## 3. Performance Validation

**Result: PASS**

Analytics queries complete within acceptable time for integration-test workloads.

| Criterion | Status | Evidence |
|-----------|--------|----------|
| Report generation completes in tests | PASS | `ReportingEndpointTests` — class progress and school summary generation return 200 without timeout |
| Longitudinal queries complete in tests | PASS | `LongitudinalAnalyticsEndpointTests` — multi-period EDW seed data queried for student and organisation views in &lt;1 s |
| Effectiveness aggregation completes in tests | PASS | `EffectivenessAnalyticsEndpointTests` — curriculum and intervention aggregation over seeded facts in &lt;1 s |
| EDW ingest processing completes in tests | PASS | Processor and consumer tests persist facts without timeout |

**Notes (non-blocking):**

- No dedicated load or latency SLA test exists for analytics endpoints under production-scale data volumes. Integration tests exercise realistic multi-period seed data without performance regression.
- Report generation is on-demand (not pre-cached) per issue 033; acceptable for current pilot scale.

**Blockers:** None.

---

## 4. Data Validation

**Result: PASS**

EDW fact records faithfully reflect operational domain events; analytics queries produce correct aggregations from known seed data.

| Criterion | Status | Evidence |
|-----------|--------|----------|
| Evidence event → fact field mapping | PASS | `EdwIngestProcessorTests.ProcessEvidenceCreated_PersistsEvidenceFact` — `EventId`, `EvidenceId`, `StudentUserId`, `MicroSkillCount` match source event |
| Assessment event → fact field mapping | PASS | `ProcessAssessmentApproved_PersistsAssessmentFact` — `AssessmentId`, `MicroSkillCount` match source event |
| Intervention event → fact field mapping | PASS | `ProcessInterventionCreated_PersistsInterventionFact` — `InterventionId`, `Status` match source event |
| Consumer pipeline end-to-end | PASS | `EdwIngestConsumerTests` — published domain events result in persisted facts via MassTransit consumers |
| Longitudinal aggregation correctness | PASS | `Teacher_CanViewStudentLongitudinalAnalysis_WithMultiPeriodEdwData` — cumulative micro-skill counts (3→8→10) and gap/intervention timelines match seeded EDW facts |
| Organisation-level aggregation correctness | PASS | `SchoolLeader_CanViewOrganisationLongitudinalAnalysis_WithMultiPeriodEdwData` — school trend aggregates two students correctly (12 at period 202602, 14 at 202603) |
| Effectiveness aggregation correctness | PASS | `SchoolLeader_CanViewEffectivenessAnalysis_WithKnownEdwSeedData` — mastery rates (75%, 40%, 50%) and intervention success rates (67%, 50%) match seeded counts |
| Underperforming flag logic | PASS | Algebra (40%) and Fractions (50%) flagged `IsUnderperforming`; Forces (75%) not flagged |
| Reports source operational truth via EI | PASS | Class progress report pulls live EI insights and class dashboard data; school summary pulls leadership aggregation — not stale EDW copies |

**Blockers:** None.

---

## 5. Stakeholder Approval

| Field | Value |
|-------|-------|
| **Decision** | Approved — Phase 6 complete |
| **Approver** | Platform Engineering |
| **Date** | 2026-08-31 |
| **Rationale** | All Phase 6 validation criteria pass. Teachers generate on-demand class progress reports with PDF export sourced from EI. School leaders generate school summary reports and access longitudinal, curriculum effectiveness, and intervention effectiveness dashboards aggregated from the EDW. The EDW ingest pipeline reliably consumes domain events with idempotent writes to a separate analytics store. Analytics queries produce correct results from multi-period seed data. Role-based access is enforced across teacher, leader, and student boundaries. No blockers identified. |

---

## Summary

| Validation area | Result |
|-----------------|--------|
| Educational validation (reports and analytics support decision-making) | PASS |
| Technical validation (EDW ingest reliable; reports generate correctly) | PASS |
| Performance validation (analytics queries complete within acceptable time) | PASS |
| Data validation (EDW data matches operational source of truth) | PASS |
| Stakeholder approval | APPROVED |

**Gate decision: PASS — Phase 6 (issues 033–036) is complete.**
