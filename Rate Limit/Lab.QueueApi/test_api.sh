#!/bin/bash

echo "=== Testing Queued Web API ==="
echo

# Test health endpoint
echo "1. Testing health endpoint:"
curl -s http://localhost:5001/api/queuedapi/health
echo -e "\n"

# Test first request (should be processed directly)
echo "2. Testing first request (should be processed directly):"
curl -s -X POST -H "Content-Type: application/json" -d '{"data":"Test request 1"}' http://localhost:5001/api/queuedapi/process
echo -e "\n"

# Test second request (should be processed directly)
echo "3. Testing second request (should be processed directly):"
curl -s -X POST -H "Content-Type: application/json" -d '{"data":"Test request 2"}' http://localhost:5001/api/queuedapi/process
echo -e "\n"

# Test third request (should be queued and return 429)
echo "4. Testing third request (should be queued and return 429):"
response=$(curl -s -i -X POST -H "Content-Type: application/json" -d '{"data":"Test request 3"}' http://localhost:5001/api/queuedapi/process)
echo "$response"
echo

# Extract request ID from the response
request_id=$(echo "$response" | grep -o '"requestId":"[^"]*"' | cut -d'"' -f4)
echo "Extracted Request ID: $request_id"
echo

# Test status endpoint
echo "5. Testing status endpoint:"
curl -s http://localhost:5001/api/queuedapi/status/$request_id
echo -e "\n"

# Wait a bit and test status again
echo "6. Waiting 5 seconds and testing status again:"
sleep 5
curl -s http://localhost:5001/api/queuedapi/status/$request_id
echo -e "\n"

# Test health endpoint again to see queue status
echo "7. Testing health endpoint again:"
curl -s http://localhost:5001/api/queuedapi/health
echo -e "\n"

echo "=== Test completed ==="

