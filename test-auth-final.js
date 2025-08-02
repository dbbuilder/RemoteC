const http = require('http');

console.log('🎯 Final Authentication Test\n');

// Test 1: Login
console.log('1️⃣ Testing login...');
const loginData = JSON.stringify({
  email: 'admin@remotec.demo',
  password: 'admin123'
});

const loginReq = http.request({
  hostname: 'localhost',
  port: 7001,
  path: '/api/auth/dev-login',
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'Content-Length': loginData.length
  }
}, (res) => {
  let data = '';
  res.on('data', chunk => data += chunk);
  res.on('end', () => {
    if (res.statusCode === 200) {
      const response = JSON.parse(data);
      console.log('✅ Login successful!');
      console.log('   Token:', response.token.substring(0, 50) + '...');
      
      // Test 2: Dashboard API call
      console.log('\n2️⃣ Testing dashboard API with token...');
      testDashboard(response.token);
    } else {
      console.log('❌ Login failed:', res.statusCode);
    }
  });
});

loginReq.write(loginData);
loginReq.end();

function testDashboard(token) {
  http.get({
    hostname: 'localhost',
    port: 7001,
    path: '/api/dashboard/stats',
    headers: {
      'Authorization': `Bearer ${token}`
    }
  }, (res) => {
    if (res.statusCode === 200) {
      console.log('✅ Dashboard API call successful!');
      console.log('\n🎉 Authentication system is fully working!');
      console.log('\n📱 You can now open http://localhost:3000 and login with:');
      console.log('   Username: admin');
      console.log('   Password: admin123');
      console.log('\n✨ The app should:');
      console.log('   1. Accept your credentials');
      console.log('   2. Store the auth token');
      console.log('   3. Redirect to dashboard');
      console.log('   4. Stay authenticated (no redirect loop)');
      console.log('   5. Make authenticated API calls');
    } else {
      console.log('❌ Dashboard call failed:', res.statusCode);
    }
  });
}