import http from 'k6/http';
import { check, sleep } from 'k6';
import { login, fetchMe } from '../lib/auth.js';
import { urls, demo, authHeaders } from '../lib/config.js';
import { options as baseOptions } from '../lib/options.js';

export const options = baseOptions;

/**
 * Scenario: teacher class dashboard — organisation class dashboard + EI insights
 * (same GETs as portal /teacher/classes/[classId]).
 *
 * Requires: compose up + ./scripts/seed-demo.sh
 */
export function setup() {
  const token = login(demo.emails.teacher);
  if (!token) {
    throw new Error('Teacher login failed — is identity-service up and seeded?');
  }
  return { token };
}

export default function (data) {
  const headers = authHeaders(data.token);
  const { organisationId, classId } = demo;

  const me = fetchMe(data.token);
  check(me, { 'auth/me 200': (r) => r.status === 200 });

  const dashboard = http.get(
    `${urls.organisation}/api/v1/organisations/${organisationId}/classes/${classId}/dashboard`,
    {
      headers,
      tags: { name: 'GET .../classes/{id}/dashboard' },
    }
  );
  check(dashboard, { 'class dashboard 200': (r) => r.status === 200 });

  const insights = http.get(
    `${urls.ei}/api/v1/ei/organisations/${organisationId}/classes/${classId}/insights`,
    {
      headers,
      tags: { name: 'GET .../classes/{id}/insights' },
    }
  );
  check(insights, { 'class EI insights 200': (r) => r.status === 200 });

  // Teacher home also lists orgs/classes.
  const orgs = http.get(`${urls.organisation}/api/v1/organisations`, {
    headers,
    tags: { name: 'GET /api/v1/organisations' },
  });
  check(orgs, { 'organisations 200': (r) => r.status === 200 });

  const classes = http.get(
    `${urls.organisation}/api/v1/organisations/${organisationId}/classes`,
    { headers, tags: { name: 'GET .../organisations/{id}/classes' } }
  );
  check(classes, { 'classes list 200': (r) => r.status === 200 });

  sleep(0.5);
}
