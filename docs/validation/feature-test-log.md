# Feature test delivery log

Running log for Step 2 batches. One line per completed batch.

| Batch | Commits | Tests added | Bugs found |
|-------|---------|-------------|------------|
| 2 School isolation | `cc4bcc6`…`0fa7229` | SchoolIsolation.Tests + per-service TenantIsolationEndpointTests | 5 cross-school leaks (org list, parent link, diagnostics, inbox, interventions) |
| 2b Isolation hardening | `273aa8d`…`8f73340` | IgnoreQueryFilters guard; federation A↔B; Admin assessment isolation | Assessment Admin mutate; ReportGenerator missing TenantAccess |
| 6 Evidence UI locked | `ddfea6a` | a-core-learning-loop Mark/Feedback/Draft lock assertions | none |
| 17 National small-count | `09dc90f` | CountCell suppression tests for 7 aggregate endpoints | none |
| 6 Evidence UI locked | `ddfea6a` | a-core-learning-loop lock assertions | none |
| 17 National small-count | `09dc90f` | CountCell suppression tests (7 endpoints) | none |
