import http from 'k6/http';
import { sleep } from 'k6';

export default function () {
    console.log(`User agent is '${__ENV.ASPNETCORE_ENVIRONMENT }'`);
    http.get('http://test.k6.io');
    sleep(1);
}
