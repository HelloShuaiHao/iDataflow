#!/usr/bin/env node

// 简单的测试脚本，模拟后端发送数据到 n8n
const http = require('http');

const testData = {
    type: 'websocket_data',
    companyId: 'test-company',
    clientId: 'test-client-123',
    timestamp: new Date().toISOString(),
    payload: {
        temperature: 25.5,
        humidity: 60,
        pressure: 1013.25,
        deviceId: 'test-device-001',
        location: 'test-location'
    }
};

const data = JSON.stringify(testData);

// 测试不同的 n8n webhook URL
const webhookUrls = [
    'localhost:5678/webhook-test/1158ae5b-867d-40ec-868c-5b3ffbdd7b7d',
    'localhost:5678/webhook/test',
    'localhost:5678/webhook/data',
];

async function testWebhook(url) {
    return new Promise((resolve) => {
        const options = {
            hostname: 'localhost',
            port: 5678,
            path: '/' + url.split('localhost:5678/')[1],
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Content-Length': data.length
            }
        };

        console.log(`🔍 Testing webhook: http://${url}`);
        
        const req = http.request(options, (res) => {
            let responseData = '';
            
            res.on('data', (chunk) => {
                responseData += chunk;
            });
            
            res.on('end', () => {
                if (res.statusCode === 200 || res.statusCode === 201) {
                    console.log(`✅ Success ${res.statusCode}: ${responseData}`);
                } else {
                    console.log(`❌ Error ${res.statusCode}: ${responseData}`);
                }
                resolve();
            });
        });

        req.on('error', (e) => {
            console.log(`💥 Request error: ${e.message}`);
            resolve();
        });

        req.write(data);
        req.end();
    });
}

async function testAll() {
    console.log('📡 Testing direct connection from backend to n8n...\n');
    console.log('📤 Test data:', JSON.stringify(testData, null, 2));
    console.log('\n' + '='.repeat(60) + '\n');
    
    for (const url of webhookUrls) {
        await testWebhook(url);
        await new Promise(resolve => setTimeout(resolve, 1000)); // Wait 1 second between tests
    }
    
    console.log('\n' + '='.repeat(60));
    console.log('🏁 Test completed');
}

testAll();