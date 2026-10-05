import http from 'k6/http';
import { check, sleep } from 'k6';
import { login, fetchMe } from '../lib/auth.js';
import { urls, demo, authHeaders } from '../lib/config.js';
import { options as baseOptions } from '../lib/options.js';

export const options = baseOptions;

/**
 * Scenario: authority policy dashboard GETs (EducationAuthorityOfficer JWT).
 * Mirrors portal /authority — four national-reporting policy endpoints.
 */
export function setup() {
  const token = login(demo.emails.authority);
  if (!token) {
    throw new Error(
      'Authority login failed — is identity-service up and seeded?'
    );
  }
  return { token };
}

export default function (data) {
  const headers = authHeaders(data.token);
  const base = `${urls.nationalReporting}/api/v1/policy-dashboards`;

  const me = fetchMe(data.token);
  check(me, { 'auth/me 200': (r) => r.status === 200 });

  const trends = http.get(`${base}/trends`, {
    headers,
    tags: { name: 'GET /api/v1/policy-dashboards/trends' },
  });
  check(trends, { 'policy trends 200': (r) => r.status === 200 });

  const equity = http.get(`${base}/equity`, {
    headers,
    tags: { name: 'GET /api/v1/policy-dashboards/equity' },
  });
  check(equity, { 'policy equity 200': (r) => r.status === 200 });

  const curriculum = http.get(`${base}/curriculum-effectiveness`, {
    headers,
    tags: { name: 'GET /api/v1/policy-dashboards/curriculum-effectiveness' },
  });
  check(curriculum, {
    'policy curriculum-effectiveness 200': (r) => r.status === 200,
  });

  const interventions = http.get(`${base}/intervention-impact`, {
    headers,
    tags: { name: 'GET /api/v1/policy-dashboards/intervention-impact' },
  });
  check(interventions, {
    'policy intervention-impact 200': (r) => r.status === 200,
  });

  sleep(0.5);
}
