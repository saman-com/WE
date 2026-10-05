/**
 * Shared k6 options. Thresholds are recorded for delivery reporting but
 * run-load.sh treats exit code 99 (threshold miss) as non-fatal so misses
 * can be filed as bugs without aborting the suite.
 */
export const options = {
  thresholds: {
    http_req_duration: ['p(95)<500'],
  },
  // Soft defaults; run-load.sh overrides with --vus / --duration.
  vus: Number(__ENV.VUS || 1),
  duration: __ENV.DURATION || '30s',
};
