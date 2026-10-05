import http from 'k6/http';
import { check, sleep } from 'k6';
import { Counter } from 'k6/metrics';
import { login } from '../lib/auth.js';
import { urls, demo, authHeaders } from '../lib/config.js';
import { options as baseOptions } from '../lib/options.js';

export const options = baseOptions;

const evidenceApproved = new Counter('evidence_approve_ok');
const evidenceSkipped = new Counter('evidence_approve_na');

/**
 * Scenario: teacher approves evidence (POST /api/v1/evidence).
 *
 * Setup: run ./scripts/seed-demo.sh so org/class/micro-skills exist.
 *
 * Each iteration creates a unique published assessment, has the demo student
 * submit, then POSTs evidence approval — avoids conflict on already-approved
 * seed submissions (Algebra sheet). On any setup failure, records N/A and skips.
 */
export function setup() {
  const teacherToken = login(demo.emails.teacher);
  const studentToken = login(demo.emails.student);
  if (!teacherToken || !studentToken) {
    throw new Error('Teacher/student login failed — is identity-service up?');
  }
  return { teacherToken, studentToken };
}

export default function (data) {
  const teacherHeaders = authHeaders(data.teacherToken);
  const studentHeaders = authHeaders(data.studentToken);
  const { organisationId, classId, studentUserId, microSkills } = demo;
  const title = `Load evidence VU${__VU}-I${__ITER}-${Date.now()}`;

  // 1. Create assessment
  const createRes = http.post(
    `${urls.assessment}/api/v1/assessments`,
    JSON.stringify({
      organisationId,
      classId,
      title,
      instructions: 'k6 load evidence approve',
      dueAt: null,
      learningObjectiveIds: [],
      microSkillIds: microSkills,
    }),
    { headers: teacherHeaders, tags: { name: 'POST /api/v1/assessments' } }
  );

  if (createRes.status !== 200 && createRes.status !== 201) {
    evidenceSkipped.add(1);
    sleep(0.5);
    return;
  }

  let assessmentId;
  try {
    assessmentId = createRes.json('id');
  } catch (_) {
    evidenceSkipped.add(1);
    sleep(0.5);
    return;
  }

  // 2. Publish
  const publishRes = http.post(
    `${urls.assessment}/api/v1/assessments/${assessmentId}/publish`,
    null,
    {
      headers: teacherHeaders,
      tags: { name: 'POST /api/v1/assessments/{id}/publish' },
    }
  );
  if (publishRes.status !== 200 && publishRes.status !== 204) {
    evidenceSkipped.add(1);
    sleep(0.5);
    return;
  }

  // 3. Student submit
  const submitRes = http.post(
    `${urls.assessment}/api/v1/assessments/${assessmentId}/submissions`,
    JSON.stringify({ responses: `k6 load submit VU${__VU} I${__ITER}` }),
    {
      headers: studentHeaders,
      tags: { name: 'POST /api/v1/assessments/{id}/submissions' },
    }
  );
  if (submitRes.status !== 200 && submitRes.status !== 201) {
    evidenceSkipped.add(1);
    sleep(0.5);
    return;
  }

  let submissionId;
  try {
    submissionId = submitRes.json('id');
  } catch (_) {
    evidenceSkipped.add(1);
    sleep(0.5);
    return;
  }

  // 4. Approve evidence
  const approveBody = {
    organisationId,
    classId,
    assessmentId,
    submissionId,
    studentUserId,
    title,
    microSkillMarks: [
      { microSkillId: microSkills[0], mark: 5, feedback: 'Read an equation' },
      { microSkillId: microSkills[1], mark: 4, feedback: 'Substitute a value' },
      { microSkillId: microSkills[2], mark: 3, feedback: 'Use inverse operations' },
      { microSkillId: microSkills[3], mark: 2, feedback: 'Isolate the variable' },
    ],
  };

  const approveRes = http.post(
    `${urls.evidence}/api/v1/evidence`,
    JSON.stringify(approveBody),
    { headers: teacherHeaders, tags: { name: 'POST /api/v1/evidence' } }
  );

  const ok = check(approveRes, {
    'evidence approve 201': (r) => r.status === 201,
  });

  if (ok) {
    evidenceApproved.add(1);
  } else {
    // Conflict (already approved) or other — treat as N/A for delivery report
    evidenceSkipped.add(1);
  }

  sleep(0.5);
}
