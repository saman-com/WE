#!/usr/bin/env bash
# Deactivate the qa1006 sweep accounts and remove the rows that sweep left
# in the demo school. Identity has no delete, so accounts are deactivated.
# Idempotent: a second run changes nothing.
set -euo pipefail

root=$(cd "$(dirname "$0")/.." && pwd)
cd "$root"

ORG=00000000-0000-4000-8000-000000000001
id_dir=$(mktemp -d)
trap 'rm -rf "$id_dir"' EXIT

emails=(
  qa1006.student@school.local
  qa1006.parent@school.local
  qa1006.teacher@school.local
  qa1006.schoolleader@school.local
  qa1006.educationauthorityofficer@school.local
  qa1006.federationadmin@school.local
  qa1006.systemadministrator@school.local
)

email_list=$(printf "'%s'," "${emails[@]}")
email_list=${email_list%,}
titles="'QA', 'QA check 1006', 'QA check 1006b', 'QA check 1006c'"

psql() {
  docker compose exec -T postgres psql -U we -v ON_ERROR_STOP=1 "$@"
}

count() {
  local db=$1 label=$2 sql=$3
  local n
  n=$(psql -d "$db" -Atc "$sql")
  printf '%-48s %s\n' "$label" "$n"
}

user_id() {
  psql -d we_identity -Atc "select \"Id\" from \"AspNetUsers\" where \"Email\" = '$1'"
}

apply_ids() {
  local db=$1
  local sql=$2
  {
    echo "begin;"
    echo "create temp table qa_assessment_ids (id uuid);"
    echo "copy qa_assessment_ids (id) from stdin;"
    cat "$id_dir/assessments"
    printf '%s\n' '\.'
    echo "$sql"
    echo "commit;"
  } | psql -d "$db" -q
}

psql -d we_identity -q <<'SQL'
alter table "AspNetUsers" add column if not exists is_active boolean not null default true;
SQL

student_id=$(user_id "qa1006.student@school.local")
parent_id=$(user_id "qa1006.parent@school.local")
class_id=$(psql -d we_organisation -Atc "select id from classes where organisation_id = '$ORG' and code = 'QA1006'")
psql -d we_assessment -Atc \
  "select id from assessments where tenant_id = '$ORG' and title in ($titles)" \
  >"$id_dir/assessments"

snapshot() {
  echo
  echo "$1"
  count we_identity "qa1006 accounts still active" \
    "select count(*) from \"AspNetUsers\" where \"Email\" in ($email_list) and is_active = true"
  count we_organisation "year Y or QA Year 1006" \
    "select count(*) from year_levels where organisation_id = '$ORG' and name in ('Y', 'QA Year 1006')"
  count we_organisation "class QA1006" \
    "select count(*) from classes where organisation_id = '$ORG' and code = 'QA1006'"
  count we_organisation "class_teachers for QA1006" \
    "select count(*) from class_teachers t join classes c on c.id = t.class_id where c.organisation_id = '$ORG' and c.code = 'QA1006'"
  count we_organisation "class_enrollments for QA1006" \
    "select count(*) from class_enrollments e join classes c on c.id = e.class_id where c.organisation_id = '$ORG' and c.code = 'QA1006'"
  if [[ -n "$parent_id" && -n "$student_id" ]]; then
    count we_organisation "parent_student_links for QA parent" \
      "select count(*) from parent_student_links where parent_user_id = '$parent_id' and student_user_id = '$student_id'"
  else
    printf '%-48s %s\n' "parent_student_links for QA parent" "0"
  fi
  count we_assessment "QA assessments" \
    "select count(*) from assessments where tenant_id = '$ORG' and title in ($titles)"
  count we_curriculum "subject Chemistry (CHEM)" \
    "select count(*) from subjects where tenant_id = '$ORG' and code = 'CHEM' and name = 'Chemistry'"
  count we_federation "federation policy qa-policy" \
    "select count(*) from federation_policies where policy_key = 'qa-policy'"
  count we_evidence "evidence for those assessments" \
    "select count(*) from educational_evidence where title in ($titles)"
  count we_notifications "notifications for those assessments" \
    "select count(*) from notifications where title in ('Assessment available: QA check 1006', 'Assessment available: QA check 1006b', 'Assessment available: QA check 1006c') or (title = 'Teacher feedback available' and recipient_user_id = '${student_id:-00000000-0000-0000-0000-000000000000}')"
  count we_learning "profile entries for those assessments" \
    "select count(*) from profile_evidence_entries where title in ($titles)"
  count we_learning "profile enrollments for QA1006" \
    "select count(*) from profile_class_enrollments where class_code = 'QA1006'"
  count we_diagnostic "diagnostics for QA student" \
    "select count(*) from micro_skill_diagnostics where student_user_id = '${student_id:-00000000-0000-0000-0000-000000000000}'"
  count we_mastery "mastery marks for QA student" \
    "select count(*) from mastery_evidence_marks where student_user_id = '${student_id:-00000000-0000-0000-0000-000000000000}'"
  count we_mastery "mastery records for QA student" \
    "select count(*) from mastery_records where student_user_id = '${student_id:-00000000-0000-0000-0000-000000000000}'"
}

# Fact counts cannot use apply_ids, which commits a delete-shaped statement.
# Count them with a one-off query instead.
fact_count() {
  if [[ ! -s "$id_dir/assessments" ]]; then
    echo 0
    return
  fi
  local list
  list=$(paste -sd, "$id_dir/assessments" | sed "s/\([^,]*\)/'\1'/g")
  local evidence assessment
  evidence=$(psql -d we_edw -Atc "select count(*) from fact_evidence where assessment_id in ($list)")
  assessment=$(psql -d we_edw -Atc "select count(*) from fact_assessment where assessment_id in ($list)")
  echo $((evidence + assessment))
}

print_facts() {
  printf '%-48s %s\n' "edw facts for those assessments" "$(fact_count)"
}

snapshot "Before"
print_facts

apply_ids we_evidence "
delete from evidence_micro_skill_marks
where evidence_id in (
  select id from educational_evidence
  where assessment_id in (select id from qa_assessment_ids)
);
delete from educational_evidence
where assessment_id in (select id from qa_assessment_ids);
"

apply_ids we_learning "
delete from profile_evidence_micro_skills
where evidence_entry_id in (
  select id from profile_evidence_entries
  where assessment_id in (select id from qa_assessment_ids)
);
delete from profile_evidence_entries
where assessment_id in (select id from qa_assessment_ids);
"
if [[ -n "$class_id" ]]; then
  psql -d we_learning -q -c "delete from profile_class_enrollments where class_id = '$class_id';"
fi

apply_ids we_notifications "
delete from notifications
where related_entity_id::text in (select id::text from qa_assessment_ids);
"

apply_ids we_gaps "
delete from learning_gaps
where assessment_id in (select id from qa_assessment_ids);
"

apply_ids we_diagnostic "
delete from micro_skill_diagnostics
where assessment_id in (select id from qa_assessment_ids);
"

apply_ids we_mastery "
delete from mastery_evidence_marks
where assessment_id in (select id from qa_assessment_ids);
"
if [[ -n "$student_id" ]]; then
  psql -d we_mastery -q -c "
delete from mastery_records r
where r.student_user_id = '$student_id'
  and not exists (
    select 1 from mastery_evidence_marks m
    where m.student_user_id = r.student_user_id
      and m.micro_skill_id = r.micro_skill_id
  );"
fi

apply_ids we_edw "
delete from fact_evidence
where assessment_id in (select id from qa_assessment_ids);
delete from fact_assessment
where assessment_id in (select id from qa_assessment_ids);
"

apply_ids we_assessment "
delete from ai_feedback_audit_logs
where assessment_id in (select id from qa_assessment_ids);
delete from assessments
where id in (select id from qa_assessment_ids);
"

{
  echo "begin;"
  echo "delete from class_teachers where class_id in (select id from classes where organisation_id = '$ORG' and code = 'QA1006');"
  echo "delete from class_enrollments where class_id in (select id from classes where organisation_id = '$ORG' and code = 'QA1006');"
  if [[ -n "$parent_id" && -n "$student_id" ]]; then
    echo "delete from parent_student_links where parent_user_id = '$parent_id' and student_user_id = '$student_id';"
  fi
  echo "delete from classes where organisation_id = '$ORG' and code = 'QA1006';"
  echo "delete from year_levels where organisation_id = '$ORG' and name in ('Y', 'QA Year 1006');"
  echo "commit;"
} | psql -d we_organisation -q

psql -d we_curriculum -q -c \
  "delete from subjects where tenant_id = '$ORG' and code = 'CHEM' and name = 'Chemistry';"

psql -d we_federation -q -c \
  "delete from federation_policies where policy_key = 'qa-policy';"

psql -d we_identity -q -c \
  "update \"AspNetUsers\" set is_active = false where \"Email\" in ($email_list);"

snapshot "After"
print_facts
