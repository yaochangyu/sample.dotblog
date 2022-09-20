import {SharedArray} from 'k6/data';
import {sleep} from 'k6';

const data = new SharedArray('users', function () {
    // const d = open('D:\\src\\sample.dotblog\\Test\\Lab k6 sample\\users.json');
    // here you can open files, and then do additional processing or generate the array with data dynamically
    const f = JSON.parse(open('./data/users.json'));
    return f; // f must be an array[]
});

export default () => {
    console.log('Getting random user...');
    const randomUser = data[Math.floor(Math.random() * data.length)];
    console.log(`${randomUser.username}, ${randomUser.password}`);
    sleep(3);
};
