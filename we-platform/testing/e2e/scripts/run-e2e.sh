#!/usr/bin/env bash
# Bring up the backend stack, seed demo data, start the portal via Playwright webServer, run e2e.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../../.." && pwd)"
E2E_DIR="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

echo "==> docker compose up"
docker compose up -d --build

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
