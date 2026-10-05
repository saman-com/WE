# Feature test delivery log

Running log for Step 2 batches. One line per completed batch.

| Batch | Commits | Tests added | Bugs found |
|-------|---------|-------------|------------|
| 2 School isolation | `cc4bcc6`…`0fa7229` | SchoolIsolation.Tests + per-service TenantIsolationEndpointTests | 5 cross-school leaks (org list, parent link, diagnostics, inbox, interventions) |
| 2b Isolation hardening | `273aa8d`…`8f73340` | IgnoreQueryFilters guard; federation A↔B; Admin assessment isolation | Assessment Admin mutate; ReportGenerator missing TenantAccess |
| 6 Evidence UI locked | `ddfea6a` | a-core-learning-loop Mark/Feedback/Draft lock assertions | none |
| 17 National small-count | `09dc90f` | CountCell suppression tests for 7 aggregate endpoints | none |
| 1 Sign-in | `2b56d2b` | Expired JWT; refresh; logout e2e; extra role×route 403s | none |
| 11 AI draft UI | `54c2128` | Student feedback absent before approve in d-ai-feedback | none |
| 3–5 Org/curriculum/assessments | `006bf7c`, `e7ccc2b` | Duplicates, class delete, leader assign, variant-links, edit/delete after publish | Duplicate year/class Conflict; org-code uniqueness ignored tenant filter |
| 12–13 Parent/messages | — | already covered; unread count not required | none |
| 14–16 Leadership/analytics/federation | `36143f7` | KPI vs seeded aggregator; empty charts; school-admin policies | none |
| 18 + 8–10 Arabic/homes | `259ef6f` | Parent RTL; i18n scans; empty homes; intervention filters | none |
| 19 Non-functional | — (no commit) | `docs/validation/nfr-report.md`; PlatformSlaLoadTests p95 out; vuln/CORS/secrets/health/backup probes | Next.js 15.1.0 critical advisories; OWASP headers missing; no k6 |
