#!/usr/bin/env bash
# Seeds the UX-001 sample (Year 11 Mathematics, Algebra) for the demo users from
# IdentityDataSeeder, so /teacher and /student show real data after sign-in.
# Safe to re-run. Requires: docker compose services up, curl, jq.
set -euo pipefail

cd "$(dirname "$0")/.."

IDENTITY=${IDENTITY_API_URL:-http://localhost:8081}
ORGANISATION=${ORGANISATION_API_URL:-http://localhost:8082}
CURRICULUM=${CURRICULUM_API_URL:-http://localhost:8083}
LEARNING=${STUDENT_LEARNING_API_URL:-http://localhost:8084}
ASSESSMENT=${ASSESSMENT_API_URL:-http://localhost:8085}
EVIDENCE=${EVIDENCE_API_URL:-http://localhost:8086}

# The school must use the default tenant id: demo users' tokens carry it, and
# every /organisations/{id} route rejects a different id.
ORG=00000000-0000-4000-8000-000000000001
YEAR=00000000-0000-4000-8000-000000000011
CLASS=00000000-0000-4000-8000-000000000111
STUDENT=22222222-2222-2222-2222-222222222222
PARENT=33333333-3333-3333-3333-333333333333
LEADER=55555555-5555-5555-5555-555555555555

# Second school (tenant isolation). Users teacher-b / student-b carry this tenant.
ORG_B=00000000-0000-4000-8000-000000000002
YEAR_B=00000000-0000-4000-8000-000000000012
CLASS_B=00000000-0000-4000-8000-000000000112
STUDENT_B=88888888-8888-8888-8888-888888888888

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

api_ok() {
  local method=$1 url=$2 bearer=$3 body=${4:-}
  local code
  if [[ -n $body ]]; then
    code=$(curl -sS -o /tmp/seed-demo-body.json -w '%{http_code}' -X "$method" "$url" \
      -H "Authorization: Bearer $bearer" -H 'Content-Type: application/json' -d "$body")
  else
    code=$(curl -sS -o /tmp/seed-demo-body.json -w '%{http_code}' -X "$method" "$url" \
      -H "Authorization: Bearer $bearer")
  fi
  if [[ $code == 200 || $code == 201 || $code == 204 || $code == 409 ]]; then
    cat /tmp/seed-demo-body.json
    return 0
  fi
  echo "API $method $url failed ($code): $(cat /tmp/seed-demo-body.json)" >&2
  return 1
}

TEACHER=$(psql -d we_identity -c "select \"Id\" from \"AspNetUsers\" where \"Email\" = 'teacher@school.local'")
TEACHER_B=$(psql -d we_identity -c "select \"Id\" from \"AspNetUsers\" where \"Email\" = 'teacher-b@schoolb.local'")
if [[ -z $TEACHER ]]; then
  echo "teacher@school.local not found. Start identity-service first." >&2
  exit 1
fi
if [[ -z $TEACHER_B ]]; then
  echo "teacher-b@schoolb.local not found. Rebuild/restart identity-service to pick up new seed users." >&2
  exit 1
fi

echo "School A, class, teacher, student, leader"
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

echo "School B (tenant isolation)"
psql -d we_organisation <<SQL
insert into organisations (id, name, code, created_at, tenant_id)
values ('$ORG_B', 'School B', 'SCHB', now(), '$ORG_B') on conflict do nothing;
insert into year_levels (id, organisation_id, name, sort_order, tenant_id)
values ('$YEAR_B', '$ORG_B', 'Year 11', 11, '$ORG_B') on conflict do nothing;
insert into classes (id, organisation_id, year_level_id, name, code, tenant_id)
values ('$CLASS_B', '$ORG_B', '$YEAR_B', 'Year 11 Science', '11SCI', '$ORG_B') on conflict do nothing;
insert into class_teachers (class_id, teacher_user_id, tenant_id)
values ('$CLASS_B', '$TEACHER_B', '$ORG_B') on conflict do nothing;
insert into class_enrollments (class_id, student_user_id, tenant_id)
values ('$CLASS_B', '$STUDENT_B', '$ORG_B') on conflict do nothing;
SQL

ADMIN_TOKEN=$(token admin@school.local)
TEACHER_TOKEN=$(token teacher@school.local)
STUDENT_TOKEN=$(token student@school.local)

echo "Parent–student link and school leader assignment"
api_ok POST "$ORGANISATION/api/v1/parents/$PARENT/children" "$ADMIN_TOKEN" \
  "{\"studentUserId\":\"$STUDENT\"}" >/dev/null
api_ok POST "$ORGANISATION/api/v1/organisations/$ORG/leaders" "$ADMIN_TOKEN" \
  "{\"userId\":\"$LEADER\"}" >/dev/null

echo "Algebra curriculum (for assessment LO / micro-skill pickers)"
CURRICULUM_EXISTING=$(api GET "$CURRICULUM/api/v1/curriculum?organisationId=$ORG" "$TEACHER_TOKEN" \
  | jq -r '[.[] | select(.name == "Year 11 Mathematics")][0].id // empty')
if [[ -z $CURRICULUM_EXISTING ]]; then
  CURRICULUM_EXISTING=$(api POST "$CURRICULUM/api/v1/curriculum" "$TEACHER_TOKEN" \
    "{\"organisationId\":\"$ORG\",\"name\":\"Year 11 Mathematics\",\"version\":\"1.0\",\"status\":\"Active\",\"scope\":\"School\"}" \
    | jq -r .id)
  SUBJECT_CREATED=$(api POST "$CURRICULUM/api/v1/curriculum/$CURRICULUM_EXISTING/subjects" "$TEACHER_TOKEN" \
    '{"name":"Mathematics","code":"MATH","sortOrder":1}' | jq -r .id)
  UNIT_CREATED=$(api POST "$CURRICULUM/api/v1/curriculum/$CURRICULUM_EXISTING/subjects/$SUBJECT_CREATED/units" "$TEACHER_TOKEN" \
    '{"name":"Algebra","sortOrder":1}' | jq -r .id)
  LO_CREATED=$(api POST "$CURRICULUM/api/v1/curriculum/$CURRICULUM_EXISTING/subjects/$SUBJECT_CREATED/units/$UNIT_CREATED/learning-objectives" "$TEACHER_TOKEN" \
    '{"title":"Solve linear equations","sortOrder":1}' | jq -r .id)
  for skill in "Read an equation" "Substitute a value" "Use inverse operations" "Isolate the variable"; do
    api POST "$CURRICULUM/api/v1/curriculum/$CURRICULUM_EXISTING/subjects/$SUBJECT_CREATED/units/$UNIT_CREATED/learning-objectives/$LO_CREATED/micro-skills" "$TEACHER_TOKEN" \
      "{\"name\":\"$skill\",\"sortOrder\":1}" >/dev/null
  done
fi

echo "Learning profile enrollment"
api POST "$LEARNING/api/v1/students/$STUDENT/profile/enrollments" "$ADMIN_TOKEN" \
  "{\"organisationId\":\"$ORG\",\"classId\":\"$CLASS\",\"className\":\"Year 11 Mathematics\",\"classCode\":\"11MAT\"}" >/dev/null || true

existing_assessment() {
  # The list API returns one page. This class can hold thousands of rows, so a
  # title that is not on the first page was inserted again on every seed run.
  local title=${1//\'/\'\'}
  psql -d we_assessment -c \
    "select id from assessments where class_id = '$CLASS' and title = '$title' order by created_at limit 1"
}

drop_duplicate_assessments() {
  local title=${1//\'/\'\'}
  psql -d we_assessment <<SQL
delete from assessments
where class_id = '$CLASS'
  and title = '$title'
  and id <> (
    select id from assessments
    where class_id = '$CLASS' and title = '$title'
    order by created_at
    limit 1
  );
SQL
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
if [[ $(api GET "$EVIDENCE/api/v1/evidence?assessmentId=$SHEET" "$TEACHER_TOKEN" | jq '.items | length') == 0 ]]; then
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
drop_duplicate_assessments "Algebra check"

echo "Federation school and policy for federation@ministry.local"
# Identity puts this id on the FederationAdmin token. Upsert directly: the
# create-school API provisions regional configuration with no service credential.
# An existing DFED row, including one left by a failed provision, is updated in place.
FED_ID=00000000-0000-4000-8000-0000000000f1
psql -d we_federation <<SQL
insert into federation_schools (
  id, tenant_id, federation_id, name, code, enrollment_count, average_progress_percent,
  has_default_configuration, created_at)
values (
  '00000000-0000-4000-8000-0000000000f3',
  '00000000-0000-4000-8000-0000000000f2',
  '$FED_ID',
  'North Federation School',
  'DFED',
  120,
  68.5,
  true,
  now()
)
on conflict (federation_id, code) do update set
  name = excluded.name,
  enrollment_count = excluded.enrollment_count,
  average_progress_percent = excluded.average_progress_percent,
  has_default_configuration = true;
insert into federation_policies (id, federation_id, policy_key, policy_value, updated_at)
select
  '00000000-0000-4000-8000-0000000000f4',
  '$FED_ID',
  'shared-curriculum',
  'enabled',
  now()
where not exists (
  select 1 from federation_policies
  where federation_id = '$FED_ID' and policy_key = 'shared-curriculum'
);
SQL

echo "Done. Sign in as teacher@school.local or student@school.local (Password123!)."
echo "Also: leader@school.local, federation@ministry.local, teacher-b@schoolb.local, student-b@schoolb.local"
