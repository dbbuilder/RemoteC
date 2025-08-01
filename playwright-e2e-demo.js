// Playwright E2E test with screenshots and video recording
const { chromium } = require('playwright');

(async () => {
  console.log('=== RemoteC Playwright E2E Demo ===\n');
  
  // Launch browser with video recording
  const browser = await chromium.launch({
    headless: false, // Set to true for CI/CD
    slowMo: 500 // Slow down for visibility
  });

  const context = await browser.newContext({
    recordVideo: {
      dir: './e2e-videos/',
      size: { width: 1280, height: 720 }
    }
  });

  const page = await context.newPage();
  
  try {
    // 1. Navigate to API Swagger UI
    console.log('1. Navigating to API documentation...');
    await page.goto('http://localhost:17001/swagger/index.html');
    await page.waitForSelector('.swagger-ui', { timeout: 5000 });
    await page.screenshot({ 
      path: 'screenshots/01-api-swagger.png',
      fullPage: true 
    });
    console.log('✓ Screenshot: API Swagger UI');

    // 2. Navigate to test client
    console.log('\n2. Opening RemoteC test client...');
    await page.goto('file://' + process.cwd() + '/test-remote-client.html');
    await page.screenshot({ path: 'screenshots/02-test-client-initial.png' });
    console.log('✓ Screenshot: Test client initial state');

    // 3. Test API connection
    console.log('\n3. Testing API connection...');
    await page.click('button:has-text("Test API Connection")');
    await page.waitForSelector('.status.connected', { timeout: 5000 });
    await page.screenshot({ path: 'screenshots/03-api-connected.png' });
    console.log('✓ API connection established');

    // 4. Authenticate
    console.log('\n4. Authenticating...');
    await page.click('button:has-text("Authenticate")');
    await page.waitForTimeout(1000);
    
    // Check if authentication succeeded by looking for enabled buttons
    const connectBtn = await page.$('button#connectBtn:not([disabled])');
    if (connectBtn) {
      await page.screenshot({ path: 'screenshots/04-authenticated.png' });
      console.log('✓ Authentication successful');
    }

    // 5. Connect SignalR
    console.log('\n5. Connecting to SignalR...');
    await page.click('button#connectBtn');
    await page.waitForSelector('#signalrStatus.connected', { timeout: 10000 });
    await page.screenshot({ path: 'screenshots/05-signalr-connected.png' });
    console.log('✓ SignalR connected');

    // 6. List devices
    console.log('\n6. Listing devices...');
    await page.click('button#listDevicesBtn');
    await page.waitForTimeout(1000);
    
    // Check device name updated
    const deviceName = await page.textContent('#deviceName');
    if (deviceName !== '-') {
      await page.screenshot({ path: 'screenshots/06-devices-listed.png' });
      console.log(`✓ Device found: ${deviceName}`);
    }

    // 7. Create session
    console.log('\n7. Creating session...');
    await page.click('button#createSessionBtn');
    await page.waitForTimeout(2000);
    
    const sessionId = await page.textContent('#sessionId');
    if (sessionId !== 'None') {
      await page.screenshot({ path: 'screenshots/07-session-created.png' });
      console.log(`✓ Session created: ${sessionId}`);
    }

    // 8. Start session
    console.log('\n8. Starting session...');
    await page.click('button#startSessionBtn');
    await page.waitForTimeout(2000);
    
    const pin = await page.textContent('#sessionPin');
    if (pin !== '-') {
      await page.screenshot({ path: 'screenshots/08-session-started-with-pin.png' });
      console.log(`✓ Session started with PIN: ${pin}`);
    }

    // 9. Test remote control buttons
    console.log('\n9. Testing remote control...');
    await page.click('button#mouseBtn');
    await page.waitForTimeout(500);
    await page.click('button#keyBtn');
    await page.waitForTimeout(500);
    await page.screenshot({ path: 'screenshots/09-remote-control-active.png' });
    console.log('✓ Remote control commands sent');

    // 10. Check event log
    console.log('\n10. Capturing event log...');
    await page.evaluate(() => {
      const logDiv = document.getElementById('eventLog');
      if (logDiv) logDiv.scrollTop = logDiv.scrollHeight;
    });
    await page.screenshot({ 
      path: 'screenshots/10-event-log.png',
      fullPage: true 
    });
    console.log('✓ Event log captured');

    // 11. Stop session
    console.log('\n11. Stopping session...');
    await page.click('button#stopSessionBtn');
    await page.waitForTimeout(1000);
    await page.screenshot({ path: 'screenshots/11-session-stopped.png' });
    console.log('✓ Session stopped');

    // 12. Navigate to visualization
    console.log('\n12. Opening system visualization...');
    await page.goto('file://' + process.cwd() + '/system-visualization.html');
    await page.waitForSelector('.diagram', { timeout: 5000 });
    await page.screenshot({ 
      path: 'screenshots/12-system-architecture.png',
      fullPage: true 
    });
    console.log('✓ System architecture captured');

    console.log('\n=== E2E Demo Complete ===');
    console.log('\nScreenshots saved in: ./screenshots/');
    console.log('Video saved in: ./e2e-videos/');
    
  } catch (error) {
    console.error('Error during E2E test:', error.message);
    await page.screenshot({ path: 'screenshots/error-state.png' });
  } finally {
    // Keep browser open for a moment to finish video
    await page.waitForTimeout(2000);
    await context.close();
    await browser.close();
  }
})();