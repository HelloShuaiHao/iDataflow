

测试 workflow wss api 是
curl -X POST http://localhost:5678/webhook-test/1158ae5b-867d-40ec-868c-5b3ffbdd7b7d \
  -H "Content-Type: application/json" \
  -d '{"test mode": "direct webhook test", "temperature": 25.5, "timestamp": "'$(date -u +%Y-%m-%dT%H:%M:%SZ)'"}'

