import {htmlReport} from "https://raw.githubusercontent.com/benc-uk/k6-reporter/main/dist/bundle.js";
import {textSummary} from "https://jslib.k6.io/k6-summary/0.0.1/index.js";

import http from 'k6/http';
import {sleep} from 'k6';

export default function () {
    console.log(`User agent is '${__ENV.MY_USER_AGENT}'`);
    http.get('http://test.k6.io');
    sleep(1);
}

export function handleSummary(data) {
    return {
        "result.html": htmlReport(data),
        stdout: textSummary(data, { indent: " ", enableColors: true }),
    };
}