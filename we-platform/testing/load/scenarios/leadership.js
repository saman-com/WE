import http from 'k6/http';
import { check, sleep } from 'k6';
import { login, fetchMe } from '../lib/auth.js';
import { urls, demo, authHeaders } from '../lib/config.js';
import { options as baseOptions } from '../lib/options.js';

export const options = baseOptions;

/**
 * Scenario: school leadership dashboard GETs
 * (portal /leadership — orgs list, dashboard, interventions).
 *
 * Requires: compose up + ./scripts/seed-demo.sh (leader assigned to org).
 * Note: SchoolLeader role may still hit known product gaps (see e2e README);
 * checks record status for reporting rather than aborting the suite.
 */
export function setup() {
  const token = login(demo.emails.leader);
  if (!token) {
    throw new Error('Leader login failed — is identity-service up and seeded?');
  }
  return { token };
}

export default function (data) {
  const headers = authHeaders(data.token);
  const { organisationId, yearLevelId, classId } = demo;

  const me = fetchMe(data.token);
  check(me, { 'auth/me 200': (r) => r.status === 200 });

  const orgs = http.get(`${urls.organisation}/api/v1/organisations`, {
    headers,
    tags: { name: 'GET /api/v1/organisations' },
  });
  check(orgs, { 'organisations 200': (r) => r.status === 200 });

  const dashboard = http.get(
    `${urls.organisation}/api/v1/organisations/${organisationId}/leadership/dashboard`,
    {
      headers,
      tags: { name: 'GET .../leadership/dashboard' },
    }
  );
  check(dashboard, {
    'leadership dashboard 2xx': (r) => r.status >= 200 && r.status < 300,
  });

  const interventions = http.get(
    `${urls.organisation}/api/v1/organisations/${organisationId}/leadership/interventions`,
    {
      headers,
      tags: { name: 'GET .../leadership/interventions' },
    }
  );
  check(interventions, {
    'leadership interventions 2xx': (r) => r.status >= 200 && r.status < 300,
  });

  const yearDash = http.get(
    `${urls.organisation}/api/v1/organisations/${organisationId}/year-levels/${yearLevelId}/leadership/dashboard`,
    {
      headers,
      tags: { name: 'GET .../year-levels/{id}/leadership/dashboard' },
    }
  );
  check(yearDash, {
    'year-level leadership 2xx': (r) => r.status >= 200 && r.status < 300,
  });

  const classSummary = http.get(
    `${urls.organisation}/api/v1/organisations/${organisationId}/classes/${classId}/leadership/summary`,
    {
      headers,
      tags: { name: 'GET .../classes/{id}/leadership/summary' },
    }
  );
  check(classSummary, {
    'class leadership summary 2xx': (r) => r.status >= 200 && r.status < 300,
  });

  sleep(0.5);
}
