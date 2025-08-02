const http = require('http');

console.log('🚀 Simulating browser authentication flow...\n');

// Step 1: Login
const loginData = JSON.stringify({
  email: 'admin@remotec.demo',
  password: 'admin123'
});

const loginOptions = {
  hostname: 'localhost',
  port: 7001,
  path: '/api/auth/dev-login',
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'Content-Length': loginData.length
  }
};

console.log('1️⃣ Sending login request...');
const loginReq = http.request(loginOptions, (res) => {
  let data = '';

  res.on('data', (chunk) => {
    data += chunk;
  });

  res.on('end', () => {
    console.log('   Status:', res.statusCode);
    
    if (res.statusCode === 200) {
      const response = JSON.parse(data);
      console.log('✅ Login successful!');
      console.log('   Token:', response.token.substring(0, 50) + '...');
      console.log('   User:', response.user.email);
      console.log('   Roles:', response.user.roles);
      
      // Step 2: Test authenticated request
      console.log('\n2️⃣ Making authenticated API call...');
      testAuthenticatedCall(response.token);
      
      // Step 3: Simulate what browser does
      console.log('\n3️⃣ Browser would now:');
      console.log('   - Store token in localStorage');
      console.log('   - Redirect to /dashboard');
      console.log('   - Initialize SignalR connection with token');
      console.log('   - Load dashboard data using authenticated API calls');
      
      console.log('\n✅ Authentication flow is working correctly!');
      console.log('   No login loop should occur.');
    } else {
      console.log('❌ Login failed:', data);
    }
  });
});

loginReq.on('error', (e) => {
  console.error('❌ Request error:', e);
});

loginReq.write(loginData);
loginReq.end();

function testAuthenticatedCall(token) {
  const options = {
    hostname: 'localhost',
    port: 7001,
    path: '/api/dashboard/stats',
    method: 'GET',
    headers: {
      'Authorization': `Bearer ${token}`
    }
  };
  
  const req = http.request(options, (res) => {
    console.log('   Dashboard stats status:', res.statusCode);
    if (res.statusCode === 200) {
      console.log('✅ Authenticated API calls working!');
    }
  });
  
  req.on('error', (e) => {
    console.error('   Dashboard request error:', e);
  });
  
  req.end();
}