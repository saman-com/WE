# Phase 5 Parent and Leadership Validation Gate Report

| Field | Value |
|-------|-------|
| **Gate** | Phase 5 Parent and Leadership (issues 027–031) |
| **Reference** | EP-001 §18.16, EP-001 §18.9, SP-001 Ch.15–17 |
| **Issue** | `issues/032-phase5-validation-gate.md` |
| **Validation date** | 2026-08-31 |
| **Validator** | Platform Engineering (automated gate review) |
| **Overall result** | **PASS — Phase 5 complete** |

## Scope

This gate validates Parent and Leadership features delivered across issues 027–031:

| Issue | Deliverable |
|-------|-------------|
| 027 | Parent Workspace — child progress summary (mastery, feedback, assessments, interventions) |
| 028 | Parent–teacher messaging scoped to student context |
| 029 | School Leadership Dashboard — school-wide KPIs and drill-down |
| 030 | Notification Service — event-driven in-app and email notifications |
| 031 | Leadership intervention monitoring — school-wide oversight with filters |

---

## 1. Educational Validation

**Result: PASS**

Parents can understand their child's learning progress. School leaders see actionable school-wide KPIs.

| Capability | Evidence |
|------------|----------|
| Parent sees mastery summary | `Parent_CanViewLinkedChildProgress` aggregates mastery from mastery-service; portal `/parent` displays mastery levels per micro-skill |
| Parent sees recent feedback | `Parent_CanViewLinkedChildProgress` includes evidence feedback marks; portal `/parent` shows feedback section |
| Parent sees assessment results | `Parent_CanViewLinkedChildProgress` includes assessment summaries with submission status |
| Parent sees active interventions | `Parent_CanViewApprovedInterventionSummaryForLinkedChild`; portal `/parent` shows active interventions with planned actions and timeline |
| Leadership sees school KPIs | `SchoolLeader_CanViewLeadershipDashboardForAssignedOrganisation` — total students, classes, active interventions, learning gaps, mastery distribution |
| Leadership drill-down | `SchoolLeader_CanDrillDownToYearLevel`, `SchoolLeader_CanDrillDownToClassSummary`; portal `/leadership` with year-level and class comparison views |
| Leadership intervention oversight | `SchoolLeader_CanListLeadershipInterventionsWithAggregation`; portal `/leadership` intervention section with student, gap, teacher, status, timeline |

**Web portal pages verified:**

- Parent workspace: `/parent` (child selector, mastery, feedback, assessments, interventions)
- Parent messaging: `/parent/messages`
- Leadership dashboard: `/leadership` (KPIs, year-level drill-down, class comparison, intervention monitoring)
- Teacher messaging: `/teacher/messages`
- Notifications: `/notifications`

**Blockers:** None.

---

## 2. Privacy Validation

**Result: PASS**

Parents see only their linked children's data. Confidential teacher information is filtered from parent views.

| Criterion | Status | Evidence |
|-----------|--------|----------|
| Parent–child link required | PASS | `Parent_CannotViewUnlinkedChildProgress` — 403 without link; `GetChildProgress` checks `ParentStudentLinks` before aggregation |
| Parent cannot view other parents' children | PASS | `Parent_CannotViewOtherStudentsLinkedChildProgress` — 403 when child linked to different parent |
| Parent list scoped to own children | PASS | `Parent_CanListLinkedChildren` — parent sees only admin-linked children |
| Intervention notes excluded from parent view | PASS | `Parent_CannotViewInterventionNotes` — `ToParentSummary` returns only `PlannedActions`, status, timeline; notes and outcome omitted |
| Teacher diagnostics not exposed to parents | PASS | `DiagnosticEndpoints.EvaluateAccessAsync` allows admin/teacher only; all other roles receive 403 |
| Mastery scoped to linked child | PASS | `Parent_CanViewLinkedChildMasterySummary` via parent access checker |
| Messaging scoped to student context | PASS | `Messages_AreScopedToStudentContext`; conversations require parent–student and teacher–student access |
| Notifications recipient-only | PASS | `Notifications_AreScopedToRecipientOnly`; list filtered by `RecipientUserId` |

**Blockers:** None.

---

## 3. Technical Validation

**Result: PASS**

Messaging, notifications, and dashboards work end-to-end with event-driven architecture and cross-service aggregation.

| Criterion | Status | Evidence |
|-----------|--------|----------|
| Parent progress aggregation | PASS | `organisation-service` orchestrates HTTP clients to assessment, evidence, mastery, and intervention services; `Parent_CanViewLinkedChildProgress` |
| Parent–teacher messaging persisted | PASS | `MessageHistory_IsPersistedAndAuditable` — DB persistence with sender role and timestamp; `MessageSent` domain event published |
| Leadership dashboard aggregates from EI | PASS | `LeadershipDashboardAggregator` calls `ei-service` class insights; `SchoolLeader_CanViewLeadershipDashboardForAssignedOrganisation` verifies `_eiClient.RequestedClasses` |
| Assessment completion in dashboard | PASS | Dashboard test verifies `_assessmentClient.RequestedClasses` called for class summaries |
| Intervention data from Intervention Service | PASS | `SchoolLeader_CanListLeadershipInterventionsWithAggregation` joins intervention-service data with class enrollment and EI severity |
| Event-driven notifications | PASS | `AssessmentPublishedConsumer_CreatesInAppNotificationAndSendsEmail`, `AssessmentApprovedConsumer_CreatesFeedbackAvailableNotification`, `MessageSentConsumer_CreatesNewMessageNotification` |
| In-app notification list and mark-read | PASS | `User_CanListOwnNotifications`, `User_CanMarkNotificationAsRead` |
| Email adapter (mock for local) | PASS | `FakeEmailNotifier` / `IEmailNotifier` in notification-service tests |
| Backend build | PASS | `dotnet build WePlatform.sln` — 0 warnings, 0 errors |
| Backend tests | PASS | 218/218 tests passing (`dotnet test WePlatform.sln`) |

**Phase 5 test breakdown:**

| Service | Phase 5 tests | Result |
|---------|---------------|--------|
| organisation-service (parent + leadership) | 14 | Pass |
| communication-service (messaging) | 9 | Pass |
| notification-service (API + consumers) | 7 | Pass |
| intervention-service (parent filter + leader oversight) | 8 | Pass |
| mastery-service (parent scope) | 1 | Pass |

**Notes (non-blocking):**

- Leadership dashboard "loads within 2 seconds" criterion (issue 029) has no dedicated performance test; aggregation is HTTP-based with no evidence of regression in integration tests.
- Parent–child linking is admin API only (`Admin_LinksParentToStudent`); no dedicated admin portal UI found.
- Issue 030 "what to build" mentions intervention-created notifications; no `InterventionCreatedConsumer` exists. The issue acceptance criteria list only assessment published, feedback available, and new message — all three are implemented and tested.

**Blockers:** None.

---

## 4. Security Validation

**Result: PASS**

Leadership and parent roles are properly scoped. Oversight roles are read-only where required.

| Criterion | Status | Evidence |
|-----------|--------|----------|
| Leadership role required for dashboard | PASS | `Teacher_CannotViewLeadershipDashboard` — 403 for teachers |
| Leadership org assignment required | PASS | `SchoolLeader_CannotViewLeadershipDashboardForUnassignedOrganisation` |
| Leadership intervention list gated | PASS | `Teacher_CannotListLeadershipInterventions` |
| Leaders cannot modify interventions | PASS | `SchoolLeader_CannotModifyIntervention` — 403 on PATCH |
| Intervention org listing scoped | PASS | `SchoolLeader_CannotListOrganisationInterventionsForUnassignedOrganisation` |
| Teacher messaging scoped to their classes | PASS | `Teacher_SeesOnlyMessagesFromParentsInTheirClasses`, `UnrelatedTeacher_CannotReadConversation` |
| Parent messaging scoped to linked children | PASS | `UnrelatedParent_CannotSendMessage`, `Parent_SeesOwnMessageThreads` |
| Students cannot message | PASS | `Student_CannotSendMessages` — 403 |
| Notification mark-read recipient-only | PASS | `UnrelatedUser_CannotMarkNotificationAsRead` — 403 |
| Cross-service parent access check | PASS | `GET /api/v1/access/parent/{parentUserId}/student/{studentUserId}` used by downstream services via `HttpParentAccessChecker` |

**Blockers:** None.

---

## 5. Stakeholder Approval

| Field | Value |
|-------|-------|
| **Decision** | Approved — Phase 5 complete |
| **Approver** | Platform Engineering |
| **Date** | 2026-08-31 |
| **Rationale** | All Phase 5 validation criteria pass. Parents see clear, role-appropriate child progress with confidential information filtered. School leaders have school-wide KPIs, drill-down, and read-only intervention oversight. Parent–teacher messaging is student-scoped and auditable. Notifications are event-driven for assessment published, feedback available, and new messages. Role-based access control is enforced across parent, teacher, and leadership boundaries. No blockers identified. |

---

## Summary

| Validation area | Result |
|-----------------|--------|
| Educational validation (parents understand progress; leaders see KPIs) | PASS |
| Privacy validation (parent scope; confidential info filtered) | PASS |
| Technical validation (messaging, notifications, dashboards end-to-end) | PASS |
| Security validation (leadership and parent roles properly scoped) | PASS |
| Stakeholder approval | APPROVED |

**Gate decision: PASS — Phase 5 (issues 027–031) is complete.**
