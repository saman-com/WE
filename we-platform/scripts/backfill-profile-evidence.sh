#!/usr/bin/env bash
# One-off backfill: copy approved evidence into student learning profiles when the
# profile timeline entry is missing (e.g. evidence approved before the event-driven
# SLP consumer). Safe to re-run. Requires: docker compose postgres, curl, jq, and
# identity + student-learning services.
set -euo pipefail

cd "$(dirname "$0")/.."

IDENTITY=${IDENTITY_API_URL:-http://localhost:8081}
LEARNING=${STUDENT_LEARNING_API_URL:-http://localhost:8084}

psql() {
  docker compose exec -T postgres psql -U we -v ON_ERROR_STOP=1 -Atq "$@"
}

token() {
  curl -sSf -X POST "$IDENTITY/api/v1/auth/login" -H 'Content-Type: application/json' \
    -d "{\"email\":\"$1\",\"password\":\"Password123!\"}" | jq -r .accessToken
}

ADMIN_TOKEN=$(token admin@school.local)
backfilled=0
skipped=0

while IFS='|' read -r evidence_id student_user_id assessment_id title recorded_at micro_skills; do
  [[ -z ${evidence_id:-} ]] && continue

  exists=$(psql -d we_learning -c \
    "select 1 from profile_evidence_entries where id = '$evidence_id' limit 1")
  if [[ -n $exists ]]; then
    skipped=$((skipped + 1))
    continue
  fi

  profile=$(psql -d we_learning -c \
    "select 1 from student_learning_profiles where student_user_id = '$student_user_id' limit 1")
  if [[ -z $profile ]]; then
    echo "Skip $evidence_id: no profile for student $student_user_id" >&2
    skipped=$((skipped + 1))
    continue
  fi

  ids_json=$(printf '%s' "$micro_skills" | jq -R 'split(",") | map(select(length>0))')
  body=$(jq -n \
    --arg evidenceId "$evidence_id" \
    --arg assessmentId "$assessment_id" \
    --arg title "$title" \
    --arg recordedAt "$recorded_at" \
    --argjson microSkillIds "$ids_json" \
    '{evidenceId:$evidenceId,assessmentId:$assessmentId,title:$title,recordedAt:$recordedAt,microSkillIds:$microSkillIds}')

  curl -sSf -X POST "$LEARNING/api/v1/students/$student_user_id/profile/evidence" \
    -H "Authorization: Bearer $ADMIN_TOKEN" \
    -H 'Content-Type: application/json' \
    -d "$body" >/dev/null

  echo "Backfilled $evidence_id -> $student_user_id"
  backfilled=$((backfilled + 1))
done < <(psql -d we_evidence -c "
select e.id::text,
       e.student_user_id,
       e.assessment_id::text,
       e.title,
       e.approved_at::text,
       coalesce(string_agg(m.micro_skill_id::text, ',' order by m.micro_skill_id), '')
from educational_evidence e
left join evidence_micro_skill_marks m on m.evidence_id = e.id
where e.status = 'Approved'
group by e.id
order by e.approved_at;
")

echo "Done. Backfilled=$backfilled skipped=$skipped"
