#!/usr/bin/env bash
# One-off: remove the DFED school row left behind when configuration
# provisioning failed after the school was saved. seed-demo.sh then upserts
# the canonical North Federation School.
set -euo pipefail

sql=$(cat <<'SQL'
delete from federation_schools
where code = 'DFED'
  and has_default_configuration = false;
SQL
)

if command -v psql >/dev/null 2>&1; then
  psql -d we_federation -c "$sql"
else
  root=$(cd "$(dirname "$0")/.." && pwd)
  docker compose -f "$root/docker-compose.yml" exec -T postgres \
    psql -U we -d we_federation -c "$sql"
fi
