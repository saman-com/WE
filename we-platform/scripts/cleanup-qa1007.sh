#!/usr/bin/env bash
# Remove the rows the 7 October QA sweep left behind. The qa1007 accounts
# are already deactivated, so this does not touch AspNetUsers.
# Idempotent: a second run changes nothing.
set -euo pipefail

root=$(cd "$(dirname "$0")/.." && pwd)
cd "$root"

ORG=00000000-0000-4000-8000-000000000001

psql() {
  docker compose exec -T postgres psql -U we -v ON_ERROR_STOP=1 "$@"
}

count() {
  local db=$1 label=$2 sql=$3
  local n
  n=$(psql -d "$db" -Atc "$sql")
  printf '%-48s %s\n' "$label" "$n"
}

snapshot() {
  local tenant=${2:-}
  echo
  echo "$1"
  count we_organisation "year QA Year 1007" \
    "select count(*) from year_levels where organisation_id = '$ORG' and name = 'QA Year 1007'"
  count we_organisation "class QA1007" \
    "select count(*) from classes where organisation_id = '$ORG' and code = 'QA1007'"
  count we_assessment "draft QA check 1007" \
    "select count(*) from assessments where title = 'QA check 1007'"
  count we_federation "federation school QA1007" \
    "select count(*) from federation_schools where code = 'QA1007' and name = 'QA School 1007'"
  count we_federation "policy qa-policy" \
    "select count(*) from federation_policies where policy_key = 'qa-policy'"
  if [[ -n "$tenant" ]]; then
    count we_configuration "regional config for QA School 1007" \
      "select count(*) from regional_configurations where tenant_id = '$tenant'"
  else
    printf '%-48s %s\n' "regional config for QA School 1007" "0"
  fi
}

TENANT=$(psql -d we_federation -Atc "select tenant_id from federation_schools where code = 'QA1007' and name = 'QA School 1007'")
snapshot "Before" "$TENANT"

psql -d we_assessment -q <<SQL
delete from assessments
where title = 'QA check 1007';
SQL

psql -d we_organisation -q <<SQL
delete from class_teachers
where class_id in (
  select id from classes where organisation_id = '$ORG' and code = 'QA1007'
);
delete from class_enrollments
where class_id in (
  select id from classes where organisation_id = '$ORG' and code = 'QA1007'
);
delete from classes
where organisation_id = '$ORG' and code = 'QA1007';
delete from year_levels
where organisation_id = '$ORG' and name = 'QA Year 1007';
SQL

if [[ -n "$TENANT" ]]; then
  psql -d we_configuration -q <<SQL
delete from regional_configurations where tenant_id = '$TENANT';
SQL
fi

psql -d we_federation -q <<SQL
delete from federation_schools
where code = 'QA1007' and name = 'QA School 1007';
delete from federation_policies
where policy_key = 'qa-policy';
SQL

snapshot "After" "$TENANT"
