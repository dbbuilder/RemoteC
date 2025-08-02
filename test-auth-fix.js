const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch({ 
    headless: false, // Set to true for headless mode
    slowMo: 100 // Slow down actions by 100ms for visibility
  });
  
  const context = await browser.newContext();
  const page = await context.newPage();
  
  console.log('🚀 Starting authentication test...');
  
  try {
    // Navigate to the app
    console.log('📍 Navigating to http://localhost:3000');
    await page.goto('http://localhost:3000');
    
    // Wait for login page to load
    await page.waitForSelector('input[id="username"]', { timeout: 10000 });
    console.log('✅ Login page loaded');
    
    // Take screenshot of login page
    await page.screenshot({ path: 'login-page.png' });
    console.log('📸 Screenshot saved: login-page.png');
    
    // Fill in credentials
    console.log('🔑 Entering credentials...');
    await page.fill('input[id="username"]', 'admin');
    await page.fill('input[id="password"]', 'admin123');
    
    // Click sign in button
    console.log('🖱️ Clicking sign in button...');
    await page.click('button[type="submit"]');
    
    // Wait for navigation or error
    try {
      // Wait for dashboard to load (successful login)
      await page.waitForURL('**/dashboard', { timeout: 10000 });
      console.log('✅ Successfully logged in! Redirected to dashboard');
      
      // Take screenshot of dashboard
      await page.screenshot({ path: 'dashboard.png' });
      console.log('📸 Screenshot saved: dashboard.png');
      
      // Check for SignalR connection
      await page.waitForTimeout(2000); // Wait for SignalR to connect
      
      // Check console logs for SignalR connection
      page.on('console', msg => {
        if (msg.text().includes('SignalR')) {
          console.log('🔌 SignalR:', msg.text());
        }
      });
      
      // Verify user is authenticated by checking localStorage
      const authData = await page.evaluate(() => {
        return localStorage.getItem('dev-auth');
      });
      
      if (authData) {
        const auth = JSON.parse(authData);
        console.log('✅ Authentication data found in localStorage');
        console.log('👤 User:', auth.user.email);
        console.log('🎫 Token exists:', !!auth.token);
      }
      
      // Try navigating to different pages
      console.log('🧭 Testing navigation...');
      
      // Click on Sessions
      const sessionsLink = await page.locator('a[href="/sessions"]').first();
      if (await sessionsLink.isVisible()) {
        await sessionsLink.click();
        await page.waitForURL('**/sessions', { timeout: 5000 });
        console.log('✅ Successfully navigated to Sessions page');
        await page.screenshot({ path: 'sessions-page.png' });
      }
      
      // Click on Settings
      const settingsLink = await page.locator('a[href="/settings"]').first();
      if (await settingsLink.isVisible()) {
        await settingsLink.click();
        await page.waitForURL('**/settings', { timeout: 5000 });
        console.log('✅ Successfully navigated to Settings page');
        await page.screenshot({ path: 'settings-page.png' });
      }
      
      console.log('\n🎉 Authentication test PASSED! No login loop detected.');
      
    } catch (navError) {
      // Check if we're stuck on login page
      const currentUrl = page.url();
      if (currentUrl.includes('localhost:3000') && !currentUrl.includes('dashboard')) {
        console.error('❌ Login failed - still on login page');
        console.error('Current URL:', currentUrl);
        
        // Check for error messages
        const errorElement = await page.locator('.alert-destructive').first();
        if (await errorElement.isVisible()) {
          const errorText = await errorElement.textContent();
          console.error('Error message:', errorText);
        }
        
        // Take screenshot of failed state
        await page.screenshot({ path: 'login-failed.png' });
        console.log('📸 Screenshot saved: login-failed.png');
      }
      throw navError;
    }
    
  } catch (error) {
    console.error('❌ Test failed:', error.message);
    await page.screenshot({ path: 'error-state.png' });
    console.log('📸 Error screenshot saved: error-state.png');
    process.exit(1);
  } finally {
    // Keep browser open for 5 seconds to observe
    console.log('\n⏳ Keeping browser open for 5 seconds...');
    await page.waitForTimeout(5000);
    await browser.close();
    console.log('🏁 Test completed');
  }
})();