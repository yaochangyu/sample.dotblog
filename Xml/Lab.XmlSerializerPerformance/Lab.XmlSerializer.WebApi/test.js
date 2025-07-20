import http from 'k6/http';
import {check, sleep} from 'k6';

export const options = {
    stages: [
        {duration: '30s', target: 50}, // Ramp-up to 50 users
        {duration: '10s', target: 50}, // Maintain 50 users for 1 minute
        {duration: '30s', target: 0}, // Ramp-down
        {duration: '10s', target: 0}, // Ramp-down
    ],
};

// const BASE_URL = 'https://localhost:7244'; // Web API base URL
const BASE_URL = 'http://localhost:5142'; // Web API base URL
// const BASE_URL = 'http://localost:5000'; // Web API base URL

export default function () {

    const response = http.get(`${BASE_URL}/XmlSerializer/bad`);
    // const response = http.get(`${BASE_URL}/XmlSerializer/good`);

    check(response, {
        'Bad Serialize - status is 200': (r) => r.status === 200,
    });
}