import http from 'k6/http';
import { check } from 'k6';
import { urls, demo, jsonHeaders, authHeaders } from './config.js';

/**
 * POST /api/v1/auth/login — returns accessToken or null.
 */
export function login(email, password = demo.password) {
  const res = http.post(
    `${urls.identity}/api/v1/auth/login`,
    JSON.stringify({ email, password }),
    { headers: jsonHeaders, tags: { name: 'POST /api/v1/auth/login' } }
  );

  const ok = check(res, {
    'login status 200': (r) => r.status === 200,
    'login has accessToken': (r) => {
      try {
        return !!r.json('accessToken');
      } catch (_) {
        return false;
      }
    },
  });

  if (!ok) {
    return null;
  }
  return res.json('accessToken');
}

/** GET /api/v1/auth/me */
export function fetchMe(token) {
  return http.get(`${urls.identity}/api/v1/auth/me`, {
    headers: authHeaders(token),
    tags: { name: 'GET /api/v1/auth/me' },
  });
}
