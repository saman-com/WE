#!/usr/bin/env bash
# Idempotent school for k6 evidence writes. Separate from the demo tenant
# (00000000-0000-4000-8000-000000000001) and from School B.
set -euo pipefail

root=$(cd "$(dirname "$0")/../../.." && pwd)
cd "$root"

ORG=00000000-0000-4000-8000-0000000000a1
YEAR=00000000-0000-4000-8000-0000000000a2
CLASS=00000000-0000-4000-8000-0000000000a3
TEACHER=aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa
STUDENT=bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb

psql() {
  docker compose exec -T postgres psql -U we -v ON_ERROR_STOP=1 "$@"
}

if [[ -z $(psql -d we_identity -Atc "select \"Id\" from \"AspNetUsers\" where \"Email\" = 'load-teacher@load.local'") ]]; then
  echo "load-teacher@load.local is not in identity yet. Rebuild identity-service so the seeder runs, then re-run this script." >&2
  exit 1
fi

psql -d we_organisation <<SQL
insert into organisations (id, name, code, created_at, tenant_id)
values ('$ORG', 'Load test school', 'LOAD', now(), '$ORG') on conflict do nothing;
insert into year_levels (id, organisation_id, name, sort_order, tenant_id)
values ('$YEAR', '$ORG', 'Load year', 1, '$ORG') on conflict do nothing;
insert into classes (id, organisation_id, year_level_id, name, code, tenant_id)
values ('$CLASS', '$ORG', '$YEAR', 'Load test class', 'LOAD1', '$ORG') on conflict do nothing;
insert into class_teachers (class_id, teacher_user_id, tenant_id)
values ('$CLASS', '$TEACHER', '$ORG') on conflict do nothing;
insert into class_enrollments (class_id, student_user_id, tenant_id)
values ('$CLASS', '$STUDENT', '$ORG') on conflict do nothing;
SQL

echo "Load-test tenant $ORG class $CLASS is ready."
