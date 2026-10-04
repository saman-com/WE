#!/usr/bin/env bash
# Seeds the UX-001 sample (Year 11 Mathematics, Algebra) for the demo users from
# IdentityDataSeeder, so /teacher and /student show real data after sign-in.
# Safe to re-run. Requires: docker compose services up, curl, jq.
set -euo pipefail

cd "$(dirname "$0")/.."

IDENTITY=${IDENTITY_API_URL:-http://localhost:8081}
LEARNING=${STUDENT_LEARNING_API_URL:-http://localhost:8084}
ASSESSMENT=${ASSESSMENT_API_URL:-http://localhost:8085}
EVIDENCE=${EVIDENCE_API_URL:-http://localhost:8086}

# The school must use the default tenant id: demo users' tokens carry it, and
# every /organisations/{id} route rejects a different id.
ORG=00000000-0000-4000-8000-000000000001
YEAR=00000000-0000-4000-8000-000000000011
CLASS=00000000-0000-4000-8000-000000000111
STUDENT=22222222-2222-2222-2222-222222222222

SKILL_READ=00000000-0000-4000-8000-000000001001
SKILL_SUBSTITUTE=00000000-0000-4000-8000-000000001002
SKILL_INVERSE=00000000-0000-4000-8000-000000001003
SKILL_ISOLATE=00000000-0000-4000-8000-000000001004

psql() {
  docker compose exec -T postgres psql -U we -v ON_ERROR_STOP=1 -Atq "$@"
}

token() {
  curl -sSf -X POST "$IDENTITY/api/v1/auth/login" -H 'Content-Type: application/json' \
    -d "{\"email\":\"$1\",\"password\":\"Password123!\"}" | jq -r .accessToken
}

api() {
  local method=$1 url=$2 bearer=$3 body=${4:-}
  if [[ -n $body ]]; then
    curl -sSf -X "$method" "$url" -H "Authorization: Bearer $bearer" -H 'Content-Type: application/json' -d "$body"
  else
    curl -sSf -X "$method" "$url" -H "Authorization: Bearer $bearer"
  fi
}

TEACHER=$(psql -d we_identity -c "select \"Id\" from \"AspNetUsers\" where \"Email\" = 'teacher@school.local'")
if [[ -z $TEACHER ]]; then
  echo "teacher@school.local not found. Start identity-service first." >&2
  exit 1
fi

echo "School, class, teacher and student"
psql -d we_organisation <<SQL
insert into organisations (id, name, code, created_at, tenant_id)
values ('$ORG', 'Demo school', 'DEMO', now(), '$ORG') on conflict do nothing;
insert into year_levels (id, organisation_id, name, sort_order, tenant_id)
values ('$YEAR', '$ORG', 'Year 11', 11, '$ORG') on conflict do nothing;
insert into classes (id, organisation_id, year_level_id, name, code, tenant_id)
values ('$CLASS', '$ORG', '$YEAR', 'Year 11 Mathematics', '11MAT', '$ORG') on conflict do nothing;
insert into class_teachers (class_id, teacher_user_id, tenant_id)
values ('$CLASS', '$TEACHER', '$ORG') on conflict do nothing;
insert into class_enrollments (class_id, student_user_id, tenant_id)
values ('$CLASS', '$STUDENT', '$ORG') on conflict do nothing;
SQL

ADMIN_TOKEN=$(token admin@school.local)
TEACHER_TOKEN=$(token teacher@school.local)
STUDENT_TOKEN=$(token student@school.local)

echo "Learning profile enrollment"
api POST "$LEARNING/api/v1/students/$STUDENT/profile/enrollments" "$ADMIN_TOKEN" \
  "{\"organisationId\":\"$ORG\",\"classId\":\"$CLASS\",\"className\":\"Year 11 Mathematics\",\"classCode\":\"11MAT\"}" >/dev/null

existing_assessment() {
  api GET "$ASSESSMENT/api/v1/assessments?organisationId=$ORG&classId=$CLASS" "$TEACHER_TOKEN" \
    | jq -r --arg title "$1" '[.[] | select(.title == $title)][0].id // empty'
}

published_assessment() {
  local title=$1 due=$2 id
  id=$(existing_assessment "$title")
  if [[ -z $id ]]; then
    id=$(api POST "$ASSESSMENT/api/v1/assessments" "$TEACHER_TOKEN" \
      "{\"organisationId\":\"$ORG\",\"classId\":\"$CLASS\",\"title\":\"$title\",\"instructions\":null,\"dueAt\":\"$due\",\"learningObjectiveIds\":[],\"microSkillIds\":[\"$SKILL_READ\",\"$SKILL_SUBSTITUTE\",\"$SKILL_INVERSE\",\"$SKILL_ISOLATE\"]}" \
      | jq -r .id)
    api POST "$ASSESSMENT/api/v1/assessments/$id/publish" "$TEACHER_TOKEN" >/dev/null
  fi
  echo "$id"
}

utc_days() {
  date -u -v"$1"d +%Y-%m-%dT09:00:00Z 2>/dev/null || date -u -d "$1 days" +%Y-%m-%dT09:00:00Z
}

echo "Algebra sheet (approved work)"
SHEET=$(published_assessment "Algebra sheet" "$(utc_days -7)")
SUBMISSION=$(api GET "$ASSESSMENT/api/v1/assessments/$SHEET/submissions/me" "$STUDENT_TOKEN" 2>/dev/null | jq -r '.id // empty' || true)
if [[ -z $SUBMISSION ]]; then
  SUBMISSION=$(api POST "$ASSESSMENT/api/v1/assessments/$SHEET/submissions" "$STUDENT_TOKEN" \
    '{"responses":"x + 3 = 7, so x = 4."}' | jq -r .id)
fi
if [[ $(api GET "$EVIDENCE/api/v1/evidence?assessmentId=$SHEET" "$TEACHER_TOKEN" | jq length) == 0 ]]; then
  api POST "$EVIDENCE/api/v1/evidence" "$TEACHER_TOKEN" "$(jq -n \
    --arg org "$ORG" --arg class "$CLASS" --arg assessment "$SHEET" --arg submission "$SUBMISSION" \
    --arg student "$STUDENT" --arg read "$SKILL_READ" --arg substitute "$SKILL_SUBSTITUTE" \
    --arg inverse "$SKILL_INVERSE" --arg isolate "$SKILL_ISOLATE" '{
      organisationId: $org, classId: $class, assessmentId: $assessment, submissionId: $submission,
      studentUserId: $student, title: "Algebra sheet",
      microSkillMarks: [
        {microSkillId: $read, mark: 5, feedback: "Read an equation"},
        {microSkillId: $substitute, mark: 4, feedback: "Substitute a value"},
        {microSkillId: $inverse, mark: 3, feedback: "Use inverse operations"},
        {microSkillId: $isolate, mark: 2, feedback: "You can substitute a number. The next step is getting the letter alone on one side."}
      ]}')" >/dev/null
fi

echo "Algebra check (due soon)"
published_assessment "Algebra check" "$(utc_days +4)" >/dev/null

echo "Done. Sign in as teacher@school.local or student@school.local (Password123!)."
