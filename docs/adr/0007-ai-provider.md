# ADR-0007: AI Provider and Gateway

## Status

Proposed

## Date

2026-08-31

## Approval

| Reviewer | Date | Decision |
|----------|------|----------|
| Saman | _pending_ | Awaiting provider selection approval before issue 023 |

## Context

Issue 022 requires a central AI Gateway so no platform service calls external AI providers directly. SP-001 Ch.19, EP-001 Ch.12, and TD-001 Ch.10 define gateway responsibilities: routing, prompt orchestration, provider abstraction, response validation, and safety filtering.

Human decisions are required for:

- Primary AI provider (OpenAI, Azure OpenAI, Anthropic, etc.)
- API key management and secrets storage
- Cost controls and fallback behaviour

## Decision

### Provider recommendation (pending approval)

| Environment | Provider | Rationale |
|-------------|----------|-----------|
| Local / CI | **Mock adapter** | Deterministic tests; no external API calls or keys |
| Production (recommended) | **Azure OpenAI Service** | Aligns with enterprise .NET stack; supports regional deployment, private networking, and Azure Key Vault integration |
| Alternative | **OpenAI API** | Simpler onboarding for early pilots; same adapter interface |

All environments use the `IAiProviderAdapter` abstraction in `ai-gateway-service`. Switching providers is configuration-only once a real adapter is implemented.

### API key management

| Secret | Storage | Never in code |
|--------|---------|---------------|
| `AI_PROVIDER_OPENAI_API_KEY` | Environment variable / Azure Key Vault | Yes |
| `AI_PROVIDER_AZURE_OPENAI_ENDPOINT` | Environment variable / Azure Key Vault | Yes |
| `AI_PROVIDER_AZURE_OPENAI_API_KEY` | Environment variable / Azure Key Vault | Yes |

Local development defaults to the Mock provider (`AiProvider:Provider=Mock`) so no keys are required.

### Cost limits

| Control | Initial limit | Enforcement |
|---------|---------------|-------------|
| Per-organisation daily token budget | 500,000 tokens | Gateway rate limiter (issue 025+) |
| Per-request max tokens | 4,096 output | Provider adapter request config |
| Concurrent requests per service | 10 | Gateway queue / 429 response |

Exceeding limits returns HTTP 429; callers must retry or surface a user-friendly message.

### Fallback strategy

1. **Primary provider unavailable** → retry once with exponential backoff (500 ms)
2. **Retry fails** → if a secondary provider is configured, route to secondary adapter
3. **No provider available** → return 503 with correlation ID; never call provider APIs from non-gateway services
4. **Local / test** → Mock adapter always available; no external fallback needed

### Prompt management

Version-controlled templates live in `we-platform/ai/prompts/{prompt-id}/v{n}/` with `metadata.json` and `template.md`. The gateway loads templates via `IPromptRegistry`; application services reference prompt IDs and versions only.

## Consequences

### Positive

- Single gateway enforces the "no direct provider calls" rule
- Mock adapter enables full TDD without network or billing
- Provider swap is an infrastructure change, not an application rewrite
- Prompt templates are reviewable, versioned assets

### Negative

- Additional network hop for every AI request
- Human approval gate blocks issue 023 until provider choice is confirmed

### Neutral

- Real OpenAI/Azure adapters deferred until provider approval
- Cost limit enforcement skeleton documented here; hard enforcement in issue 025

## Related documentation

- [`../product/SP-001-functional-specification.md`](../product/SP-001-functional-specification.md) — Ch.19
- [`../engineering/EP-001-engineering-package.md`](../engineering/EP-001-engineering-package.md) — Ch.12
- [`../architecture/TD-001-technical-architecture.md`](../architecture/TD-001-technical-architecture.md) — Ch.10
- [`../../issues/022-ai-gateway-prompt-management.md`](../../issues/022-ai-gateway-prompt-management.md)
