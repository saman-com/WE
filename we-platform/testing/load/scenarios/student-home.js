import http from 'k6/http';
import { check, sleep } from 'k6';
import { login, fetchMe } from '../lib/auth.js';
import { urls, demo, authHeaders } from '../lib/config.js';
import { options as baseOptions } from '../lib/options.js';

export const options = baseOptions;

/**
 * Scenario: student home data — mirrors portal student home + related GETs
 * (auth/me, organisation workspace, learning profile, assessments list, mastery).
 *
 * Requires: compose up + ./scripts/seed-demo.sh
 */
export function setup() {
  const token = login(demo.emails.student);
  if (!token) {
    throw new Error('Student login failed — is identity-service up and seeded?');
  }
  return { token };
}

export default function (data) {
  const headers = authHeaders(data.token);
  const studentId = demo.studentUserId;
  const { organisationId, classId } = demo;

  const me = fetchMe(data.token);
  check(me, { 'auth/me 200': (r) => r.status === 200 });

  const workspace = http.get(
    `${urls.organisation}/api/v1/students/${studentId}/workspace`,
    { headers, tags: { name: 'GET /api/v1/students/{id}/workspace' } }
  );
  check(workspace, { 'student workspace 200': (r) => r.status === 200 });

  const profile = http.get(
    `${urls.studentLearning}/api/v1/students/${studentId}/profile`,
    { headers, tags: { name: 'GET /api/v1/students/{id}/profile' } }
  );
  check(profile, { 'student profile 200': (r) => r.status === 200 });

  const assessments = http.get(
    `${urls.assessment}/api/v1/assessments?organisationId=${organisationId}&classId=${classId}`,
    { headers, tags: { name: 'GET /api/v1/assessments' } }
  );
  check(assessments, { 'assessments list 200': (r) => r.status === 200 });

  const mastery = http.get(
    `${urls.mastery}/api/v1/mastery/students/${studentId}`,
    { headers, tags: { name: 'GET /api/v1/mastery/students/{id}' } }
  );
  check(mastery, { 'student mastery 200': (r) => r.status === 200 });

  sleep(0.5);
}
