#!/usr/bin/env bash
# Bring up the backend stack (always rebuild images), seed demo data,
# start the portal via Playwright webServer, run e2e.
# Rebuild is required so local runs cannot pass against stale service images
# that differ from what CI builds.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../../.." && pwd)"
E2E_DIR="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

# A service can exit 139 (SIGSEGV) in the first seconds on a GitHub runner
# before it logs anything. Compose then fails the whole up. Retry once so a
# single startup crash does not fail the job, and print compose logs for every
# exited container before that retry so the crash leaves evidence.
dump_exited_compose_logs() {
  echo "==> docker compose logs for exited containers" >&2
  docker compose ps -a >&2 || true
  local service
  while read -r service; do
    [[ -z "$service" ]] && continue
    echo "==> docker compose logs ${service}" >&2
    docker compose logs --no-color --tail 200 "$service" >&2 || true
  done < <(docker compose ps -a --status exited --format '{{.Service}}')
}

compose_up() {
  if "$@"; then
    return 0
  fi
  dump_exited_compose_logs
  echo "==> retrying docker compose up once" >&2
  if "$@"; then
    return 0
  fi
  dump_exited_compose_logs
  return 1
}

# Build each image alone. Parallel BuildKit publishes OOM Docker Desktop hosts
# with ~8GiB. Set E2E_COMPOSE_PARALLEL=1 on larger hosts / CI to build in one pass.
if [[ "${E2E_COMPOSE_PARALLEL:-0}" == "1" ]]; then
  echo "==> docker compose up -d --build (parallel)"
  compose_up docker compose up -d --build
else
  echo "==> docker compose build (serial, one service at a time)"
  # shellcheck disable=SC2046
  for service in $(docker compose config --services); do
    has_build="$(docker compose config --format json | python3 -c "import json,sys; s=json.load(sys.stdin)['services'].get(sys.argv[1], {}); print('1' if 'build' in s else '0')" "$service")"
    if [[ "$has_build" == "1" ]]; then
      echo "--> building ${service}"
      # Cap container memory so a stuck publish cannot take down Docker Desktop.
      docker compose build --memory 3g "$service"
    fi
  done
  echo "==> docker compose up -d"
  compose_up docker compose up -d
fi

echo "==> wait for /health"
bash "$E2E_DIR/scripts/wait-for-stack.sh"

echo "==> seed demo data"
bash "$ROOT/scripts/seed-demo.sh"

# Portal env for Next.js (webServer inherits process env)
if [[ ! -f "$ROOT/apps/web-portal/.env.local" ]]; then
  cp "$ROOT/apps/web-portal/.env.local.example" "$ROOT/apps/web-portal/.env.local"
fi

echo "==> Playwright (portal via webServer config, not docker-compose)"
cd "$E2E_DIR"
pnpm exec playwright test "$@"
