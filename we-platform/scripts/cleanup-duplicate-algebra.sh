#!/usr/bin/env bash
# Remove duplicate demo Algebra sheet/check rows and the notifications that
# each extra publish or approval created. Keeps the oldest assessment of each
# title. Idempotent: a second run changes nothing.
set -euo pipefail

root=$(cd "$(dirname "$0")/.." && pwd)
cd "$root"

CLASS=00000000-0000-4000-8000-000000000111
STUDENT=22222222-2222-2222-2222-222222222222

psql() {
  docker compose exec -T postgres psql -U we -v ON_ERROR_STOP=1 "$@"
}

count() {
  local db=$1 label=$2 sql=$3
  local n
  n=$(psql -d "$db" -Atc "$sql")
  printf '%-52s %s\n' "$label" "$n"
}

kept_id() {
  local title=$1
  psql -d we_assessment -Atc \
    "select id from assessments where class_id = '$CLASS' and title = '$title' order by created_at limit 1"
}

sql_list() {
  local ids=$1
  if [[ -z "$ids" ]]; then
    echo "null"
    return
  fi
  echo "$ids" | sed "/^$/d; s/.*/'&'/" | paste -sd, -
}

snapshot() {
  echo
  echo "$1"
  count we_assessment "Algebra sheet assessments" \
    "select count(*) from assessments where class_id = '$CLASS' and title = 'Algebra sheet'"
  count we_assessment "Algebra check assessments" \
    "select count(*) from assessments where class_id = '$CLASS' and title = 'Algebra check'"
  count we_evidence "Algebra sheet evidence" \
    "select count(*) from educational_evidence where title = 'Algebra sheet' and student_user_id = '$STUDENT'"
  count we_gaps "learning gaps for demo student" \
    "select count(*) from learning_gaps where student_user_id = '$STUDENT'"
  count we_notifications "Teacher feedback available" \
    "select count(*) from notifications where recipient_user_id = '$STUDENT' and title = 'Teacher feedback available'"
  count we_notifications "Assessment available: Algebra sheet" \
    "select count(*) from notifications where recipient_user_id = '$STUDENT' and title = 'Assessment available: Algebra sheet'"
  count we_notifications "Assessment available: Algebra check" \
    "select count(*) from notifications where recipient_user_id = '$STUDENT' and title = 'Assessment available: Algebra check'"
}

KEPT_SHEET=$(kept_id "Algebra sheet")
KEPT_CHECK=$(kept_id "Algebra check")
snapshot "Before"

if [[ -z "$KEPT_SHEET" || -z "$KEPT_CHECK" ]]; then
  echo "Demo Algebra sheet or Algebra check is missing; nothing removed."
  exit 0
fi

EXTRA_ASSESSMENTS=$(psql -d we_assessment -Atc \
  "select id from assessments where class_id = '$CLASS' and title in ('Algebra sheet', 'Algebra check') and id not in ('$KEPT_SHEET', '$KEPT_CHECK')")
EXTRA_EVIDENCE=$(psql -d we_evidence -Atc \
  "select id from educational_evidence where student_user_id = '$STUDENT' and title = 'Algebra sheet' and assessment_id <> '$KEPT_SHEET'")
ASSESSMENT_LIST=$(sql_list "$EXTRA_ASSESSMENTS")
EVIDENCE_LIST=$(sql_list "$EXTRA_EVIDENCE")

psql -d we_assessment -q <<SQL
delete from assessments
where class_id = '$CLASS'
  and title in ('Algebra sheet', 'Algebra check')
  and id not in ('$KEPT_SHEET', '$KEPT_CHECK');
SQL

if [[ "$EVIDENCE_LIST" != "null" ]]; then
  psql -d we_evidence -q <<SQL
delete from evidence_micro_skill_marks where evidence_id in ($EVIDENCE_LIST);
delete from educational_evidence where id in ($EVIDENCE_LIST);
SQL
  psql -d we_gaps -q <<SQL
delete from learning_gaps where student_user_id = '$STUDENT' and evidence_id in ($EVIDENCE_LIST);
SQL
  psql -d we_diagnostic -q <<SQL
delete from micro_skill_diagnostics where student_user_id = '$STUDENT' and evidence_id in ($EVIDENCE_LIST);
SQL
  psql -d we_mastery -q <<SQL
delete from mastery_evidence_marks where student_user_id = '$STUDENT' and evidence_id in ($EVIDENCE_LIST);
SQL
  psql -d we_edw -q <<SQL
delete from fact_evidence where student_user_id = '$STUDENT' and evidence_id in ($EVIDENCE_LIST);
SQL
fi

if [[ "$ASSESSMENT_LIST" != "null" ]]; then
  psql -d we_edw -q <<SQL
delete from fact_assessment
where student_user_id = '$STUDENT' and assessment_id in ($ASSESSMENT_LIST);
SQL
fi

psql -d we_learning -q <<SQL
delete from profile_evidence_entries
where title = 'Algebra sheet'
  and assessment_id <> '$KEPT_SHEET';
SQL

psql -d we_notifications -q <<SQL
delete from notifications
where recipient_user_id = '$STUDENT'
  and (
    (title = 'Assessment available: Algebra sheet' and related_entity_id <> '$KEPT_SHEET')
    or (title = 'Assessment available: Algebra check' and related_entity_id <> '$KEPT_CHECK')
    or (title = 'Teacher feedback available' and related_entity_id <> '$KEPT_SHEET')
  );

delete from notifications n
where n.recipient_user_id = '$STUDENT'
  and n.title in (
    'Teacher feedback available',
    'Assessment available: Algebra sheet',
    'Assessment available: Algebra check'
  )
  and n.id not in (
    select distinct on (title, related_entity_id) id
    from notifications
    where recipient_user_id = '$STUDENT'
      and title in (
        'Teacher feedback available',
        'Assessment available: Algebra sheet',
        'Assessment available: Algebra check'
      )
    order by title, related_entity_id, created_at
  );
SQL

snapshot "After"
