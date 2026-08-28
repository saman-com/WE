# Skill: Test-Driven Development (TDD) for Dana

## Context
You are implementing one of the 24 tasks defined in DOC-01 Engineering Specification v1.3. You must use Test-Driven Development (TDD) to ensure all code is strictly deterministic, typed, and bounded by our authority architecture.

## Execution Rules

When instructed to implement a feature or task using TDD, you MUST adhere to the following sequence:

1. **Review Standards**: Silently read `.claude/skills/tdd/tests.md` to internalize the Dana testing invariants (e.g., PostgreSQL as incident truth, Temporal as execution authority, mocked Capability Gateways).
2. **Write the Test (RED)**:
   - Create or update the corresponding test file in the `tests/` directory (e.g., `tests/anim/workflows/test_incident.py`).
   - Write the failing `pytest` function. 
   - Ensure the test includes strict Python type hints (`mypy` compliant).
3. **Verify Failure**: 
   - Instruct the user to run the test or execute it if you have terminal capability: `pytest tests/path/to/test_file.py`
   - Confirm the test fails. (Do not write the implementation code until you have confirmed the test fails).
4. **Implement Code (GREEN)**:
   - Write the minimal Python 3.12 implementation in the `src/` directory to make the test pass.
   - Strictly adhere to service boundaries: agents do not hold long-lived credentials, and the Capability Gateway handles all external source access.
5. **Verify Success (DONE WHEN Gate)**:
   - Run the test again: `pytest tests/path/to/test_file.py`
   - Verify static typing: `mypy --strict src/ tests/`
   - Verify coverage: `pytest --cov=src/anim --cov-fail-under=100`
6. **Refactor**: Clean up the implementation. Maintain a modular, grid-based logic structure.

## Invariants & Guardrails
- **No TypeScript/Node**: Do not use `npm`, `jest`, or `vitest`. This is a Python 3.12 environment.
- **Strict Typing**: Code is not complete if `mypy --strict` fails.
- **Authority**: Assert against PostgreSQL state for incident truth. Do not assert against Temporal workflow state for operational truth.