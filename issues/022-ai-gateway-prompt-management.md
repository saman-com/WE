## Type

HITL

## Parent PRD

`SP-001_WE_Platform_Functional_Specification.md`

Supporting references: `ABC_EP001.md` (Ch.12), `ABC.md` (Ch.10)

## What to build

Implement the AI Gateway and prompt management skeleton per SP-001 Ch.19 and EP-001 Ch.12. This slice requires human decisions: AI provider choice (OpenAI, Azure OpenAI, etc.), API key management, and cost controls. No application code calls AI providers directly — all requests route through the AI Gateway.

Includes: AI Gateway service, prompt registry in `ai/prompts/`, provider adapter interface, context builder skeleton, response validator, safety filter stub, and ADR for provider choice.

## Acceptance criteria

- [ ] AI Gateway service exposes `/api/v1/ai/complete` (internal only, not public)
- [ ] Prompt templates versioned in `ai/prompts/` with metadata
- [ ] Provider adapter interface with at least one implementation (or mock for local dev)
- [ ] No service other than AI Gateway calls external AI APIs
- [ ] API keys stored in environment/secrets manager, not in code
- [ ] ADR documents provider choice, cost limits, and fallback strategy
- [ ] Human approves provider selection before issue 023 begins

## Blocked by

- `issues/021-phase3-validation-gate.md`

## User stories addressed

- SP-001 Ch.19 (AI Assistant and Intelligent Services)
- EP-001 Ch.12 (Artificial Intelligence Implementation Guide)
- ABC.md Ch.10 (AI Architecture)
