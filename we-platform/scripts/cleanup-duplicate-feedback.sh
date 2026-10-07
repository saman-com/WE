#!/usr/bin/env bash
# Two AssessmentApproved events were published (different assessments) and the
# notification used one fixed sentence, so the inbox showed two identical
# "Teacher feedback available" rows. Keep the oldest row for each recipient
# and body. Idempotent: a second run changes nothing.
set -euo pipefail

root=$(cd "$(dirname "$0")/.." && pwd)
cd "$root"

psql() {
  docker compose exec -T postgres psql -U we -v ON_ERROR_STOP=1 "$@"
}

count() {
  psql -d we_notifications -Atc \
    "select count(*) from notifications where title = 'Teacher feedback available'"
}

echo "Before: $(count)"
psql -d we_notifications -c \
  "select id, recipient_user_id, related_entity_id, created_at from notifications where title = 'Teacher feedback available' order by created_at;"

psql -d we_notifications -q <<'SQL'
delete from notifications n
where n.title = 'Teacher feedback available'
  and n.id not in (
    select distinct on (recipient_user_id, title, body) id
    from notifications
    where title = 'Teacher feedback available'
    order by recipient_user_id, title, body, created_at
  );
SQL

echo "After: $(count)"
psql -d we_notifications -c \
  "select id, recipient_user_id, related_entity_id, created_at from notifications where title = 'Teacher feedback available' order by created_at;"
