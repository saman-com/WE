import { check, sleep } from 'k6';
import { login } from '../lib/auth.js';
import { demo } from '../lib/config.js';
import { options as baseOptions } from '../lib/options.js';

export const options = baseOptions;

/**
 * Scenario: identity login (student + teacher round-robin).
 */
export default function () {
  const email =
    __ITER % 2 === 0 ? demo.emails.student : demo.emails.teacher;
  const token = login(email);
  check(token, { 'obtained access token': (t) => !!t });
  sleep(0.5);
}
