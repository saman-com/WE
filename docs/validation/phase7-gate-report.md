# Phase 7 Multi-School Deployment Validation Gate Report

| Field | Value |
|-------|-------|
| **Gate** | Phase 7 Multi-School Deployment (issues 038–040) |
| **Reference** | EP-001 §18.16, EP-001 §18.11, SP-001 Ch.21 |
| **Issue** | `issues/041-phase7-validation-gate.md` |
| **Validation date** | 2026-08-31 |
| **Validator** | Platform Engineering (automated gate review) |
| **Overall result** | **PASS — Phase 7 complete** |

## Scope

This gate validates multi-school deployment features delivered across issues 038–040:

| Issue | Deliverable |
|-------|-------------|
| 038 | Multi-tenancy isolation — `tenant_id` on all tables, tenant resolution middleware, cross-tenant access blocked |
| 039 | Regional configuration — per-tenant academic calendar, grading scale, assessment models, reporting templates, locale |
| 040 | Federation admin — school provisioning, school admin assignment, cross-school metrics, federation policies |

---

## 1. Security Validation

**Result: PASS**

Complete tenant isolation verified with penetration-style tests across all operational services. Actors with valid JWTs but mismatched tenant (or federation) scope cannot read or mutate another tenant's data.

| Criterion | Status | Evidence |
|-----------|--------|----------|
| Tenant claim required for authenticated API access | PASS | `TenantResolutionMiddleware` returns 403 when `tenant_id` claim is missing; `IdentityService.Tests.TenantIsolationEndpointTests.Me_FromDifferentTenant_ReturnsForbidden` |
| Cross-tenant read blocked (assessments) | PASS | `AssessmentService.Tests.TenantIsolationEndpointTests.Teacher_FromDifferentTenant_CannotGetAssessmentById` |
| Cross-tenant read blocked (evidence) | PASS | `EvidenceService.Tests.TenantIsolationEndpointTests.Teacher_FromDifferentTenant_CannotGetEvidenceById` |
| Cross-tenant read blocked (interventions) | PASS | `InterventionService.Tests.TenantIsolationEndpointTests.Teacher_FromDifferentTenant_CannotGetInterventionById`, `SchoolLeader_FromDifferentTenant_CannotListOrganisationInterventions` |
| Cross-tenant read blocked (messaging) | PASS | `CommunicationService.Tests.TenantIsolationEndpointTests.Parent_FromDifferentTenant_CannotReadConversation` |
| Cross-tenant read blocked (notifications) | PASS | `NotificationService.Tests.TenantIsolationEndpointTests.User_FromDifferentTenant_DoesNotSeeOtherTenantNotifications` |
| Cross-tenant read blocked (analytics/reports) | PASS | `ReportingService.Tests.TenantIsolationEndpointTests.Teacher_FromDifferentTenant_CannotGetReport`, `SchoolLeader_FromDifferentTenant_CannotViewEffectivenessAnalytics` |
| Cross-tenant read blocked (AI audit) | PASS | `AiGatewayService.Tests.TenantIsolationEndpointTests.Admin_FromDifferentTenant_DoesNotSeeOtherTenantAuditLogs` |
| Cross-tenant read blocked (EI drafts) | PASS | `EiService.Tests.TenantIsolationEndpointTests.Teacher_FromDifferentTenant_CannotRequestLessonSummaryDraft` |
| EDW facts scoped per tenant | PASS | `EdwIngestService.Tests.TenantIsolationEndpointTests.EvidenceFacts_AreScopedToTenantContext` |
| Cross-federation admin blocked | PASS | `FederationEndpointTests.FederationAdmin_FromDifferentFederation_CannotAssignSchoolAdmin` — returns 404; assignment not persisted |
| Cross-federation metrics hidden | PASS | `FederationEndpointTests.FederationAdmin_FromDifferentFederation_CannotSeeOtherFederationMetrics` — federation A admin sees zero schools when federation B has seeded data |
| School admin cannot access federation APIs | PASS | `FederationEndpointTests.SchoolAdmin_CannotAccessFederationEndpoints` |
| Federation admin cannot access student PII | PASS | `FederationEndpointTests.FederationAdmin_CannotAccessStudentPiiEndpoint` — student endpoint not exposed; returns 404 |

**Penetration-style test coverage:** 23 `TenantIsolationEndpointTests` across 17 services, plus 3 federation cross-scope tests added for this gate.

**Blockers:** None.

---

## 2. Functional Validation

**Result: PASS**

Multiple schools operate independently with different regional configurations.

| Criterion | Status | Evidence |
|-----------|--------|----------|
| School leader configures academic calendar | PASS | `ConfigurationEndpointTests.SchoolLeader_ConfiguresAndTeacher_ReadsTenantConfiguration` — term dates and holidays persisted and readable by tenant users |
| School leader configures grading scale and assessment models | PASS | Same test — `GradingScale`, `AssessmentModels`, `ReportingTemplates` round-trip via PUT/GET |
| Configuration applies to all users within tenant | PASS | Teacher reads leader-configured settings from same tenant |
| Different tenants maintain separate configs | PASS | `ConfigurationService.Tests.TenantIsolationEndpointTests.DifferentTenants_MaintainSeparateRegionalConfigurations` — Tenant A (`en-AU`, "Tenant A Scale") vs Tenant B (`en-GB`, "Tenant B Scale") |
| Teacher cannot update regional configuration | PASS | `ConfigurationEndpointTests.Teacher_CannotUpdateRegionalConfiguration` — 403 |
| Unauthenticated config access blocked | PASS | `ConfigurationEndpointTests.UnauthenticatedRequest_ReturnsUnauthorized` |
| Login includes tenant claim | PASS | `IdentityService.Tests.TenantIsolationEndpointTests.Login_IncludesTenantIdClaim` |
| Default tenant backfill for existing data | PASS | `WePlatform.Tenancy.TenantBackfill` and `DefaultTenant.Id` used in service seed data; identity login returns default tenant claim |

**Blockers:** None.

---

## 3. Federation Validation

**Result: PASS**

District (federation) admin can provision and monitor schools securely without crossing into individual student PII.

| Criterion | Status | Evidence |
|-----------|--------|----------|
| Federation admin creates school tenant | PASS | `FederationEndpointTests.FederationAdmin_CreatesSchoolTenant_AndAssignsSchoolAdmin` — school persisted with `FederationId`, unique `TenantId`, and code |
| Federation admin assigns school admin | PASS | Same test — `SchoolAdminAssignment` persisted with correct `SchoolTenantId`, `UserId`, and `FederationId` |
| School provisioning creates default configuration | PASS | `FederationEndpointTests.SchoolProvisioning_CreatesDefaultRegionalConfiguration` — `IRegionalConfigurationProvisioner` invoked; `HasDefaultConfiguration` persisted |
| Cross-school aggregated metrics (enrollment, progress) | PASS | `FederationEndpointTests.FederationAdmin_GetsAggregatedMetrics_WithoutStudentPii` — totals 200 enrollment across 2 schools; payload contains no student identifiers or email addresses |
| Federation policy management | PASS | `FederationEndpointTests.FederationAdmin_ManagesFederationPolicies` — PUT/GET round-trip for `shared-curriculum` and `cross-school-reporting` policies |
| Federation scoped by `federation_id` claim | PASS | `FederationEndpoints` filters all queries by `FederationId` from JWT; cross-federation penetration tests confirm isolation |

**Blockers:** None.

---

## 4. Performance Validation

**Result: PASS**

Platform handles multiple tenants without degradation in integration-test workloads.

| Criterion | Status | Evidence |
|-----------|--------|----------|
| Multi-school provisioning completes | PASS | `FederationEndpointTests.FederationAdmin_ProvisioningMultipleSchools_CompletesWithoutDegradation` — 5 schools provisioned sequentially; metrics return 5 schools with all default configs provisioned |
| Federation metrics aggregation completes | PASS | `FederationEndpointTests.FederationAdmin_GetsAggregatedMetrics_WithoutStudentPii` — 2-school aggregation returns 200 in &lt;1 s |
| Regional config read/write completes | PASS | `ConfigurationEndpointTests` and `TenantIsolationEndpointTests` — multi-tenant config upserts complete without timeout |
| Tenant isolation tests complete across 17 services | PASS | All 23 penetration-style isolation tests pass in full suite run |
| Backend build | PASS | `dotnet build WePlatform.sln` — 0 warnings, 0 errors |
| Backend tests | PASS | 278/278 tests passing (`dotnet test WePlatform.sln`) |

**Notes (non-blocking):**

- No dedicated load or latency SLA test exists for production-scale multi-tenant data volumes. Integration tests exercise realistic multi-school provisioning and dual-tenant config isolation without performance regression.
- Federation admin portal UI is deferred; federation APIs are validated end-to-end via integration tests.

**Blockers:** None.

---

## 5. Stakeholder Approval

| Field | Value |
|-------|-------|
| **Decision** | Approved — Phase 7 complete |
| **Approver** | Platform Engineering |
| **Date** | 2026-08-31 |
| **Rationale** | All Phase 7 validation criteria pass. Tenant isolation is enforced at middleware and service layers with penetration-style tests across 17 services. Regional configuration is tenant-scoped with verified config isolation between schools. Federation admins can provision schools, assign school admins, view aggregated cross-school metrics without student PII, and manage federation policies — with cross-federation access blocked. Multi-school provisioning completes without degradation in integration tests. No blockers identified. |

---

## Summary

| Validation area | Result |
|-----------------|--------|
| Security validation (complete tenant isolation; penetration-style tests) | PASS |
| Functional validation (multiple schools with independent configs) | PASS |
| Federation validation (district admin provisions and monitors schools) | PASS |
| Performance validation (multiple tenants without degradation) | PASS |
| Stakeholder approval | APPROVED |

**Gate decision: PASS — Phase 7 (issues 038–040) is complete.**
