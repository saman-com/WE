#!/usr/bin/env bash
# Remove k6 "Load evidence VU…" rows from the demo tenant. Idempotent.
# Does not hide those rows in page queries; it deletes the rows that the
# load scenario wrote into the demo school before it had its own tenant.
set -euo pipefail

root=$(cd "$(dirname "$0")/.." && pwd)
cd "$root"

ORG=00000000-0000-4000-8000-000000000001
id_dir=$(mktemp -d)
trap 'rm -rf "$id_dir"' EXIT

psql() {
  docker compose exec -T postgres psql -U we -v ON_ERROR_STOP=1 "$@"
}

count() {
  local db=$1 label=$2 sql=$3
  local n
  n=$(psql -d "$db" -Atc "$sql")
  printf '%-42s %s\n' "$label" "$n"
}

snapshot() {
  local phase=$1
  echo
  echo "$phase"
  count we_evidence "we_evidence.educational_evidence" \
    "select count(*) from educational_evidence where tenant_id = '$ORG' and title like 'Load evidence VU%'"
  count we_evidence "we_evidence.evidence_micro_skill_marks" \
    "select count(*) from evidence_micro_skill_marks m join educational_evidence e on e.id = m.evidence_id where e.tenant_id = '$ORG' and e.title like 'Load evidence VU%'"
  count we_assessment "we_assessment.assessments" \
    "select count(*) from assessments where tenant_id = '$ORG' and title like 'Load evidence VU%'"
  count we_assessment "we_assessment.assessment_submissions" \
    "select count(*) from assessment_submissions s join assessments a on a.id = s.assessment_id where a.tenant_id = '$ORG' and a.title like 'Load evidence VU%'"
  count we_learning "we_learning.profile_evidence_entries" \
    "select count(*) from profile_evidence_entries where title like 'Load evidence VU%'"
  count we_notifications "we_notifications.teacher_feedback" \
    "select count(*) from notifications where tenant_id = '$ORG' and title = 'Teacher feedback available'"
  count we_notifications "we_notifications.assessment_available" \
    "select count(*) from notifications where tenant_id = '$ORG' and title like 'Assessment available: Load evidence VU%'"
  count we_gaps "we_gaps.learning_gaps" \
    "select count(*) from learning_gaps where organisation_id = '$ORG'"
  count we_mastery "we_mastery.mastery_evidence_marks" \
    "select count(*) from mastery_evidence_marks where organisation_id = '$ORG'"
  count we_diagnostic "we_diagnostic.micro_skill_diagnostics" \
    "select count(*) from micro_skill_diagnostics where organisation_id = '$ORG'"
  count we_edw "we_edw.fact_evidence" \
    "select count(*) from fact_evidence where organisation_id = '$ORG'"
  count we_edw "we_edw.fact_assessment" \
    "select count(*) from fact_assessment where organisation_id = '$ORG'"
}

snapshot "Before"

psql -d we_assessment -Atc "select id from assessments where tenant_id = '$ORG' and title like 'Load evidence VU%'" >"$id_dir/assessments"
psql -d we_evidence -Atc "select id from educational_evidence where tenant_id = '$ORG' and title like 'Load evidence VU%'" >"$id_dir/evidence"

apply() {
  local db=$1
  local sql=$2
  {
    echo "begin;"
    echo "create temp table load_assessment_ids (id uuid);"
    echo "create temp table load_evidence_ids (id uuid);"
    echo "copy load_assessment_ids (id) from stdin;"
    cat "$id_dir/assessments"
    printf '%s\n' '\.'
    echo "copy load_evidence_ids (id) from stdin;"
    cat "$id_dir/evidence"
    printf '%s\n' '\.'
    echo "$sql"
    echo "commit;"
  } | psql -d "$db" -q
}

apply we_evidence "
delete from evidence_micro_skill_marks
where evidence_id in (select id from load_evidence_ids);
delete from educational_evidence
where id in (select id from load_evidence_ids)
  and tenant_id = '$ORG'
  and title like 'Load evidence VU%';
"

apply we_learning "
delete from profile_evidence_micro_skills
where evidence_entry_id in (
  select id from profile_evidence_entries where title like 'Load evidence VU%'
);
delete from profile_evidence_entries
where title like 'Load evidence VU%';
"

apply we_notifications "
delete from notifications
where tenant_id = '$ORG'
  and (
    (title = 'Teacher feedback available' and related_entity_id in (select id from load_assessment_ids))
    or title like 'Assessment available: Load evidence VU%'
  );
"

apply we_gaps "
delete from learning_gaps
where organisation_id = '$ORG'
  and (
    evidence_id in (select id from load_evidence_ids)
    or assessment_id in (select id from load_assessment_ids)
  );
"

apply we_diagnostic "
delete from micro_skill_diagnostics
where organisation_id = '$ORG'
  and (
    evidence_id in (select id from load_evidence_ids)
    or assessment_id in (select id from load_assessment_ids)
  );
"

apply we_mastery "
delete from mastery_evidence_marks
where organisation_id = '$ORG'
  and (
    evidence_id in (select id from load_evidence_ids)
    or assessment_id in (select id from load_assessment_ids)
  );

with remaining as (
  select student_user_id,
         micro_skill_id,
         organisation_id,
         count(*)::int as cnt,
         round(sum(mark * coalesce(weight, 1)) / nullif(sum(coalesce(weight, 1)), 0), 2) as avg_mark
  from mastery_evidence_marks
  where organisation_id = '$ORG'
  group by student_user_id, micro_skill_id, organisation_id
), leveled as (
  select *,
    case
      when avg_mark < 3 then 'Developing'
      when avg_mark >= 4 and cnt >= 2 then 'Mastered'
      else 'Proficient'
    end as level
  from remaining
)
update mastery_records r
set evidence_count = l.cnt,
    weighted_average = l.avg_mark,
    confidence_score = round(least(1.0, l.cnt / 2.0) * (l.avg_mark / 5.0), 2),
    mastery_level = l.level,
    explanation = 'Aggregated ' || l.cnt || ' evidence source'
      || case when l.cnt = 1 then '' else 's' end
      || ' with weighted average ' || l.avg_mark || '/5. Mastery level: ' || l.level || '.'
from leveled l
where r.organisation_id = l.organisation_id
  and r.student_user_id = l.student_user_id
  and r.micro_skill_id = l.micro_skill_id;

delete from mastery_records r
where r.organisation_id = '$ORG'
  and not exists (
    select 1 from mastery_evidence_marks m
    where m.organisation_id = r.organisation_id
      and m.student_user_id = r.student_user_id
      and m.micro_skill_id = r.micro_skill_id
  );
"

apply we_edw "
delete from fact_evidence
where organisation_id = '$ORG'
  and (
    evidence_id in (select id from load_evidence_ids)
    or assessment_id in (select id from load_assessment_ids)
  );
delete from fact_assessment
where organisation_id = '$ORG'
  and (
    evidence_id in (select id from load_evidence_ids)
    or assessment_id in (select id from load_assessment_ids)
  );
"

apply we_assessment "
delete from ai_feedback_audit_logs
where assessment_id in (select id from load_assessment_ids);
delete from assessment_submissions
where assessment_id in (select id from load_assessment_ids);
delete from assessment_micro_skills
where assessment_id in (select id from load_assessment_ids);
delete from assessment_learning_objectives
where assessment_id in (select id from load_assessment_ids);
delete from assessments
where id in (select id from load_assessment_ids)
  and tenant_id = '$ORG'
  and title like 'Load evidence VU%';
"

# Facts and feedback notifications are keyed by assessment id. Some of those
# ids belong to load-test rows that were already removed, so a title match
# cannot see them. Drop demo-tenant rows whose assessment is gone.
psql -d we_assessment -Atc "select id from assessments where tenant_id = '$ORG'" >"$id_dir/live"
if [[ ! -s "$id_dir/live" ]]; then
  echo "Refusing to drop orphan rows: the demo school has no assessments." >&2
  exit 1
fi

apply_live() {
  local db=$1
  local sql=$2
  {
    echo "begin;"
    echo "create temp table live_assessment_ids (id uuid);"
    echo "copy live_assessment_ids (id) from stdin;"
    cat "$id_dir/live"
    printf '%s\n' '\.'
    echo "$sql"
    echo "commit;"
  } | psql -d "$db" -q
}

apply_live we_notifications "
delete from notifications
where tenant_id = '$ORG'
  and title = 'Teacher feedback available'
  and related_entity_id not in (select id from live_assessment_ids);
"

apply_live we_edw "
delete from fact_evidence
where organisation_id = '$ORG'
  and assessment_id not in (select id from live_assessment_ids);
delete from fact_assessment
where organisation_id = '$ORG'
  and assessment_id not in (select id from live_assessment_ids);
"

snapshot "After"
