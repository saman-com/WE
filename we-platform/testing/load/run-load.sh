#!/usr/bin/env bash
# Run WE Platform k6 load scenarios at VU=25 and VU=100.
# Threshold misses (k6 exit 99) are reported but do not fail the suite.
set -uo pipefail

ROOT="$(cd "$(dirname "$0")" && pwd)"
RESULTS_DIR="${RESULTS_DIR:-$ROOT/results}"
DURATION="${DURATION:-30s}"
VUS_LIST="${VUS_LIST:-25 100}"
TIMESTAMP="$(date -u +%Y%m%dT%H%M%SZ)"

SCENARIOS=(
  login
  student-home
  teacher-class
  approve-evidence
  leadership
  authority
)

if [[ $# -gt 0 ]]; then
  SCENARIOS=("$@")
fi

mkdir -p "$RESULTS_DIR"

if ! command -v k6 >/dev/null 2>&1; then
  echo "k6 not found. Install: https://grafana.com/docs/k6/latest/set-up/install-k6/" >&2
  echo "  brew install k6   # macOS" >&2
  exit 1
fi

echo "WE Platform load run — ${TIMESTAMP}"
echo "  duration=${DURATION}  vus=${VUS_LIST}"
echo "  results → ${RESULTS_DIR}"
echo

summary_file="$RESULTS_DIR/run-summary-${TIMESTAMP}.txt"
{
  echo "WE Platform load summary — ${TIMESTAMP}"
  echo "duration=${DURATION} vus=${VUS_LIST}"
  echo
} >"$summary_file"

threshold_misses=0
hard_failures=0

print_thresholds() {
  local json=$1
  if ! command -v jq >/dev/null 2>&1; then
    echo "  (install jq to pretty-print threshold results)"
    return
  fi
  if [[ ! -f $json ]]; then
    return
  fi

  local p95
  p95=$(jq -r '.metrics.http_req_duration.values["p(95)"] // .metrics.http_req_duration["p(95)"] // "n/a"' "$json" 2>/dev/null || echo "n/a")
  echo "  http_req_duration p95=${p95}ms (target <500)"

  local breached
  breached=$(jq -r '
    .metrics
    | to_entries[]
    | select(.value.thresholds != null)
    | .key as $m
    | .value.thresholds
    | to_entries[]
    | select(.value.ok == false)
    | "  MISS \($m): \(.key)"
  ' "$json" 2>/dev/null || true)

  if [[ -n $breached ]]; then
    echo "$breached"
    return 1
  fi

  local any
  any=$(jq -r '
    [.metrics
    | to_entries[]
    | select(.value.thresholds != null)
    | .value.thresholds
    | to_entries[]
    | .key] | length
  ' "$json" 2>/dev/null || echo 0)
  if [[ $any != 0 && $any != "0" ]]; then
    echo "  thresholds: OK"
  fi
  return 0
}

for vu in $VUS_LIST; do
  for scenario in "${SCENARIOS[@]}"; do
    script="$ROOT/scenarios/${scenario}.js"
    if [[ ! -f $script ]]; then
      echo "Missing scenario script: $script" >&2
      hard_failures=$((hard_failures + 1))
      continue
    fi

    export_json="$RESULTS_DIR/${scenario}-${vu}vu-${TIMESTAMP}.json"
    echo "▶ ${scenario}  VUs=${vu}  duration=${DURATION}"
    echo "  summary-export → ${export_json}"

    set +e
    k6 run \
      --vus "$vu" \
      --duration "$DURATION" \
      --summary-export "$export_json" \
      "$script"
    code=$?
    set -e

    line="${scenario} vu=${vu} exit=${code}"
    if [[ $code -eq 0 ]]; then
      echo "  exit 0"
      {
        echo "$line OK"
        print_thresholds "$export_json" || true
        echo
      } >>"$summary_file"
      print_thresholds "$export_json" || true
    elif [[ $code -eq 99 ]]; then
      echo "  THRESHOLD MISS (exit 99) — recorded; continuing (file as bug if p95≥500)"
      threshold_misses=$((threshold_misses + 1))
      {
        echo "$line THRESHOLD_MISS"
        print_thresholds "$export_json" || true
        echo
      } >>"$summary_file"
      print_thresholds "$export_json" || true
    else
      echo "  FAILED (exit ${code})"
      hard_failures=$((hard_failures + 1))
      echo "$line HARD_FAIL" >>"$summary_file"
    fi
    echo
  done
done

{
  echo "----"
  echo "threshold_misses=${threshold_misses}"
  echo "hard_failures=${hard_failures}"
} >>"$summary_file"

echo "Done. Summary: $summary_file"
echo "  threshold_misses=${threshold_misses}  hard_failures=${hard_failures}"

# Soft-fail: only hard k6 errors fail the wrapper (not threshold misses).
if [[ $hard_failures -gt 0 ]]; then
  exit 1
fi
exit 0
