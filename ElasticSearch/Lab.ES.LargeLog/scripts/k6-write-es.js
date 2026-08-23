import http from 'k6/http';
import { check, sleep } from 'k6';

const rps = Number.parseInt(__ENV.RPS ?? '50', 10);
const duration = __ENV.DURATION ?? '60s';
const apiUrl = __ENV.API_URL ?? 'http://127.0.0.1:5287/api/logs';

export const options = {
  scenarios: {
    write_to_api: {
      executor: 'constant-arrival-rate',
      rate: rps,
      timeUnit: '1s',
      duration,
      preAllocatedVUs: Math.max(20, Math.ceil(rps / 2)),
      maxVUs: Math.max(100, rps * 4),
    },
  },
};

export default function () {
  const res = http.post(apiUrl, JSON.stringify({
    service: 'payment-service',
    level: 'Information',
    message: `k6 write test ${Date.now()}`,
    traceId: `trace-${__VU}-${__ITER}`,
  }), {
    headers: {
      'Content-Type': 'application/json',
    },
  });

  check(res, {
    'status is 202': (response) => response.status === 202,
  });
}
