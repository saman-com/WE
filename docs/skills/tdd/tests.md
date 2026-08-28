# Testing Guidelines for Dana (Python/pytest)

Per **DOC-01 Engineering Specification v1.3**, deterministic testing, strict typing, and validation of authority boundaries are mandatory for all implementation tasks. Dana is a governed operational reasoning platform; therefore, tests must prove determinism, state authority, and governance compliance.

## 1. Core Testing Stack

We use the standard Python testing ecosystem, strictly configured:

* **Test Runner**: `pytest`
* **Async Testing**: `pytest-asyncio` (Critical for Temporal workflows and PostgreSQL interactions).
* **Mocking**: `pytest-mock` (wrapper around `unittest.mock`).
* **Coverage**: `pytest-cov` (Target: 95% minimum per Universal DONE WHEN Criteria).
* **Static Typing**: `mypy --strict` (Tests must be fully typed, just like application code).

## 2. Structural Conventions

Unlike inline testing in some TS frameworks, Python strictly segregates application code from test code to preserve production image purity.

* **Directory Structure**: Tests must live in a `tests/` directory at the root of the repository, mirroring the structure of the `src/` directory.
```text
src/
  anim/
    workflows/
      incident.py
tests/
  anim/
    workflows/
      test_incident.py

```


* **Naming**: Test files must be prefixed with `test_`. Test functions must be prefixed with `test_`.
* **Fixtures**: Shared setup (e.g., Database sessions, Temporal test environments, Redis ephemeral caches) must be defined in `conftest.py` using `@pytest.fixture`.

## 3. Dana-Specific Testing Invariants

When practicing TDD for Tasks 01–24, the AI must enforce the following architectural constraints during test authoring:

### A. State Authority Validation

PostgreSQL is the sole incident truth authority. Temporal is strictly the workflow execution authority.

* **Rule**: Tests verifying workflow outcomes *must* assert against the mocked or test PostgreSQL database state, not just the Temporal workflow return value.

### B. Determinism & Replayability (DOC-08)

Workflows must be perfectly replayable.

* **Rule**: Tests must mock any source of non-determinism. `datetime.now(timezone.utc)`, `uuid.uuid4()`, and external API responses MUST be patched.
* *Reference*: Use `freezegun` or `pytest-mock` to pin timestamps for consistent replay verification.

### C. Capability Gateway Constraints (SEC-01)

External source access is restricted.

* **Rule**: Tests interacting with external systems must route through mocked Capability Gateway adapters.
* **Rule**: Any test covering a write operation to an external system must assert that it gracefully handles the `WRITE_DISABLED_V1` response.

## 4. The TDD Workflow (Red, Green, Refactor, Verify)

1. **Write the Test (RED)**: Write a `pytest` function describing the expected behavior, enforcing `mypy` type hints for test variables and mocks. Run `pytest` to see it fail.
2. **Write the Code (GREEN)**: Implement the minimal Python code in `src/` to satisfy the test.
3. **Refactor**: Clean up the code. Ensure modular, grid-based logic.
4. **Verify (DONE WHEN)**:
* Run `mypy --strict src/ tests/` and ensure zero errors.
* Run `pytest --cov=src --cov-fail-under=100` to verify coverage compliance.



## 5. Syntax and Examples

### Basic Async Test with Mocking

Use `@pytest.mark.asyncio` for testing Temporal activities/workflows or async DB calls. Use the `mocker` fixture provided by `pytest-mock`.

```python
import pytest
from typing import AsyncGenerator
from unittest.mock import AsyncMock
from anim.activities.gateway import CapabilityGateway
from anim.models.ticket import TicketStatus

@pytest.mark.asyncio
async def test_capability_gateway_enforces_write_disabled(mocker: "MockerFixture") -> None:
    """
    Per SEC-01 and DOC-01: All external write operations via the Capability Gateway 
    must return WRITE_DISABLED_V1 and must not mutate operational state.
    """
    # Arrange
    mock_adapter = mocker.patch("anim.adapters.vendor.VendorAdapter.execute_write", new_callable=AsyncMock)
    gateway = CapabilityGateway()
    
    # Act
    response = await gateway.request_action(action="REBOOT_DEVICE", target="core-rtr-01")
    
    # Assert
    assert response.status == "WRITE_DISABLED_V1"
    assert response.error_context == "Autonomous remediation is disabled in v1."
    mock_adapter.assert_not_called()

```

### Parametrized Testing for Edge Cases

Use `@pytest.mark.parametrize` to rigorously test matrix conditions (e.g., NFF governance rules, confidence scoring schemas).

```python
import pytest
from anim.governance.policy import evaluate_nff_policy
from anim.models.incident import IncidentState, EvidenceBundle

@pytest.mark.parametrize(
    "confidence_score, complete_loss, expected_approval_required",
    [
        (0.95, False, False),  # High confidence, no complete loss -> Auto NFF allowed
        (0.85, True, True),    # Complete loss (DOC-04 rule) -> Human approval required
        (0.40, False, True),   # Low confidence -> Human approval required
    ]
)
def test_nff_governance_gates(
    confidence_score: float, 
    complete_loss: bool, 
    expected_approval_required: bool
) -> None:
    """
    Per PRD v1.1 Section 4: NFF governance and safety rules.
    """
    # Arrange
    evidence = EvidenceBundle(
        confidence=confidence_score, 
        complete_traffic_loss=complete_loss
    )
    
    # Act
    requires_human = evaluate_nff_policy(evidence)
    
    # Assert
    assert requires_human == expected_approval_required

```

## Summary for the AI Coding Partner

When instructed to use TDD:

1. Generate the implementation issues citing relevant DOC-01 / ADR authority.
2. Scaffold the `tests/test_*.py` files *first*.
3. Write strict `pytest` functions that mock Temporal, mock the Gateway, and assert against PostgreSQL truth (or mocked equivalents).
4. Implement the application logic.
5. Do not proceed to the next task until `pytest`, `mypy --strict`, and coverage mandates pass.