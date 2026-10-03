# WE Platform Incident Response

| Field | Value |
|-------|-------|
| **Audience** | Platform Operations, Security, Engineering |
| **Related gate** | Issue 046 / EP-001 §18.16 Operational Validation |

## Severity

| Severity | Definition | Initial response |
|----------|------------|------------------|
| SEV-1 | National/ministry API down, widespread auth failure, confirmed data exposure | Immediate page; executive notify |
| SEV-2 | Single critical service down, multi-tenant degradation, queue backlog | Engage on-call within 15 minutes |
| SEV-3 | Partial feature impairment, elevated latency above SLA | Business-hours fix, next release if needed |
| SEV-4 | Cosmetic / docs / non-user-facing defect | Backlog |

## Response process

1. **Detect** — health check failure, alert, user report, or CI regression on `main`
2. **Triage** — assign severity; capture start time, impacted services, tenants/regions
3. **Contain** — restart unhealthy services; revoke compromised API keys/JWTs; disable faulty route if required
4. **Mitigate** — roll forward with fix or roll back to last known-good compose/image tag
5. **Recover** — verify `/health`, re-run critical API checks (auth, national enrollment, policy trends), confirm portal login
6. **Review** — write short post-incident note (trigger, impact, root cause, actions, follow-ups)

## Security incidents

Suspected multi-tenancy breach, evidence tampering, or AI governance failure:

1. Preserve logs and request IDs (do not wipe containers until evidence captured)
2. Revoke affected credentials (ministry API keys, user sessions)
3. Confirm tenant isolation tests still pass (`TenantIsolationEndpointTests`)
4. Escalate to Security + Platform Engineering before re-enabling access

## Contacts (fill for production)

| Role | Contact |
|------|---------|
| Platform Operations on-call | TBD |
| Platform Engineering lead | TBD |
| Security | TBD |
| Education authority liaison | TBD |
