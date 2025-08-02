const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch({ 
    headless: true // Run in headless mode
  });
  
  const context = await browser.newContext();
  const page = await context.newPage();
  
  console.log('🚀 Starting authentication test...');
  
  try {
    // Navigate to the app
    console.log('📍 Navigating to http://localhost:3000');
    await page.goto('http://localhost:3000', { waitUntil: 'networkidle' });
    
    // Wait for login page to load
    await page.waitForSelector('input[id="username"]', { timeout: 5000 });
    console.log('✅ Login page loaded');
    
    // Fill in credentials
    console.log('🔑 Entering credentials...');
    await page.fill('input[id="username"]', 'admin');
    await page.fill('input[id="password"]', 'admin123');
    
    // Click sign in button
    console.log('🖱️ Clicking sign in button...');
    await page.click('button[type="submit"]');
    
    // Wait for navigation
    console.log('⏳ Waiting for authentication...');
    
    // Give it time to process
    await page.waitForTimeout(3000);
    
    // Check current URL
    const currentUrl = page.url();
    console.log('📍 Current URL:', currentUrl);
    
    if (currentUrl.includes('/dashboard')) {
      console.log('✅ SUCCESS: Logged in and redirected to dashboard!');
      console.log('🎉 Authentication is working correctly - no login loop!');
      
      // Check localStorage for auth data
      const authData = await page.evaluate(() => localStorage.getItem('dev-auth'));
      if (authData) {
        const auth = JSON.parse(authData);
        console.log('✅ Auth token stored in localStorage');
        console.log('👤 User email:', auth.user.email);
      }
    } else {
      console.log('❌ FAILED: Still on login page - authentication loop detected!');
      
      // Check for errors
      const errorVisible = await page.locator('.alert-destructive').isVisible().catch(() => false);
      if (errorVisible) {
        const errorText = await page.locator('.alert-destructive').textContent();
        console.log('⚠️ Error message:', errorText);
      }
    }
    
  } catch (error) {
    console.error('❌ Test error:', error.message);
  } finally {
    await browser.close();
    console.log('🏁 Test completed');
  }
})();