#!/usr/bin/env bash
# Wait until every docker-compose app /health endpoint responds OK.
set -euo pipefail

PORTS=(
  8080 8081 8082 8083 8084 8085 8086 8087 8088 8089
  8090 8091 8092 8093 8094 8095 8096 8097 8098 8099 8100
)

deadline=$((SECONDS + ${E2E_HEALTH_TIMEOUT:-300}))

for port in "${PORTS[@]}"; do
  echo "Waiting for http://127.0.0.1:${port}/health"
  until curl -sf "http://127.0.0.1:${port}/health" >/dev/null; do
    if (( SECONDS > deadline )); then
      echo "Timed out waiting for port ${port}" >&2
      exit 1
    fi
    sleep 2
  done
done

echo "All health endpoints are up."
