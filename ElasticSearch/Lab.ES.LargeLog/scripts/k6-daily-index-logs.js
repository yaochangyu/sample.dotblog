const http = require('k6/http');
const { check } = require('k6');

const rps = Number.parseInt(__ENV.RPS ?? '50', 10);
const duration = __ENV.DURATION ?? '30s';
const apiUrl = __ENV.API_URL ?? 'http://127.0.0.1:5287/api/daily-index/logs';
const runId = __ENV.RUN_ID ?? 'local-run';

exports.options = {
  scenarios: {
    write_daily_index_logs: {
      executor: 'constant-arrival-rate',
      rate: rps,
      timeUnit: '1s',
      duration,
      preAllocatedVUs: Math.max(20, Math.ceil(rps / 2)),
      maxVUs: Math.max(100, rps * 4),
    },
  },
};

exports.default = function () {
  const res = http.post(apiUrl, JSON.stringify({
    service: `daily-index-load-test-${runId}-${rps}`,
    level: 'Information',
    message: `daily-index endpoint k6 write ${runId} rps=${rps}`,
    traceId: `${runId}-${rps}-${__VU}-${__ITER}`,
  }), {
    headers: {
      'Content-Type': 'application/json',
    },
  });

  check(res, {
    'status is 201': (response) => response.status === 201,
  });
};
