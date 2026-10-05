#!/usr/bin/env bash
# Poll WE Platform /health endpoints (compose host ports).
set -euo pipefail

ENDPOINTS=(
  "sample-service|http://localhost:8080/health"
  "identity-service|http://localhost:8081/health"
  "organisation-service|http://localhost:8082/health"
  "curriculum-service|http://localhost:8083/health"
  "student-learning-service|http://localhost:8084/health"
  "assessment-service|http://localhost:8085/health"
  "evidence-service|http://localhost:8086/health"
  "event-subscriber-service|http://localhost:8087/health"
  "diagnostic-service|http://localhost:8088/health"
  "learning-gap-service|http://localhost:8089/health"
  "mastery-service|http://localhost:8090/health"
  "ei-service|http://localhost:8091/health"
  "intervention-service|http://localhost:8092/health"
  "ai-gateway-service|http://localhost:8093/health"
  "communication-service|http://localhost:8094/health"
  "notification-service|http://localhost:8095/health"
  "reporting-service|http://localhost:8096/health"
  "edw-ingest-service|http://localhost:8097/health"
  "configuration-service|http://localhost:8098/health"
  "federation-service|http://localhost:8099/health"
  "national-reporting-service|http://localhost:8100/health"
)

TIMEOUT_SECS="${TIMEOUT_SECS:-120}"
INTERVAL_SECS="${INTERVAL_SECS:-3}"
deadline=$((SECONDS + TIMEOUT_SECS))
failed=0

for entry in "${ENDPOINTS[@]}"; do
  name="${entry%%|*}"
  url="${entry#*|}"
  ok=0
  while (( SECONDS < deadline )); do
    if curl -fsS "$url" >/dev/null 2>&1; then
      echo "OK   $name ($url)"
      ok=1
      break
    fi
    sleep "$INTERVAL_SECS"
  done
  if (( ok == 0 )); then
    echo "FAIL $name ($url) — not healthy within ${TIMEOUT_SECS}s"
    failed=1
  fi
done

if (( failed != 0 )); then
  exit 1
fi

echo "All ${#ENDPOINTS[@]} /health endpoints healthy."
