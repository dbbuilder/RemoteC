# E2E Test Troubleshooting Guide

## Permission Error: EPERM operation not permitted

### Problem
```
Error: EPERM: operation not permitted, rmdir 'D:\Dev2\remoteC\src\RemoteC.Web\node_modules\.vite\deps'
```

### Solutions

#### Solution 1: Clean Vite Cache
```batch
# Run the fix script
scripts\fix-vite-deps.bat
```

#### Solution 2: Manual Server Start
Instead of auto-starting servers, start them manually:

1. **Terminal 1 - Start API Server**:
   ```batch
   scripts\start-server-windows.bat
   ```

2. **Terminal 2 - Start UI**:
   ```batch
   scripts\start-dev-ui.bat
   ```

3. **Terminal 3 - Run Tests**:
   ```batch
   cd src\RemoteC.Web
   npm run test:e2e:manual
   ```

#### Solution 3: Run as Administrator
1. Open Command Prompt as Administrator
2. Navigate to project directory
3. Run the tests

#### Solution 4: Disable Antivirus
Some antivirus software can interfere with file operations:
1. Temporarily disable real-time protection
2. Add project folder to exclusions
3. Run tests again

## Alternative Test Execution Methods

### Method 1: Manual Server Start (Recommended)
```batch
scripts\run-e2e-tests-manual.bat
```
This script:
- Prompts you to start servers manually
- Avoids permission issues with auto-start
- Runs tests against already-running servers

### Method 2: Individual Test Files
```batch
cd src\RemoteC.Web

# Run specific test file
npx playwright test tests/e2e/auth.spec.ts

# Run in headed mode to see browser
npx playwright test --headed tests/e2e/auth.spec.ts
```

### Method 3: Interactive UI Mode
```batch
cd src\RemoteC.Web
npm run test:e2e:ui
```
This opens Playwright's UI where you can:
- Select specific tests to run
- See real-time results
- Debug failures interactively

## Build Errors: File Locked by Process

### Problem
```
error MSB3027: Could not copy "RemoteC.Shared.dll". The file is locked by: "RemoteC.Api (9688)"
```

### Solutions

#### Quick Fix
```batch
# Kill the specific process
scripts\kill-locked-process.bat

# Or stop all servers
scripts\stop-all-servers.bat
```

#### Safe Build Process
Use the safe build script that stops servers first:
```batch
scripts\build-safe.bat
```

## Common Issues and Fixes

### Issue: Servers not starting
**Fix**: Start servers manually in separate terminals

### Issue: Port already in use
**Fix**: Kill processes on ports
```batch
scripts\check-and-kill-port.bat 17001
scripts\check-and-kill-port.bat 17002
```

### Issue: Tests timeout
**Fix**: Increase timeout in test
```typescript
test('my test', async ({ page }) => {
  test.setTimeout(60000); // 60 seconds
  // ... test code
});
```

### Issue: Can't find elements
**Fix**: Check if development mode UI is different
- Ensure you're testing against dev UI (port 17002)
- Check for DEV mode specific elements

## Verification Steps

1. **Verify API is running**:
   ```batch
   curl http://localhost:17001/health
   ```

2. **Verify UI is running**:
   ```batch
   curl http://localhost:17002
   ```

3. **Run single test**:
   ```batch
   npx playwright test tests/e2e/simple-test.spec.ts
   ```

## Best Practices

1. **Always clean before testing**:
   ```batch
   rmdir /s /q node_modules\.vite
   ```

2. **Use manual config for stability**:
   ```batch
   npm run test:e2e:manual
   ```

3. **Run one browser at a time**:
   ```batch
   npx playwright test --project=chromium
   ```

4. **Debug with screenshots**:
   ```typescript
   await page.screenshot({ path: 'debug.png' });
   ```

## If All Else Fails

1. **Restart everything**:
   - Close all terminals
   - Kill all Node processes
   - Clean node_modules\.vite
   - Start fresh

2. **Run minimal test**:
   ```batch
   npx playwright test simple-test --project=chromium
   ```

3. **Check logs**:
   - Look at playwright-report/index.html
   - Check console output
   - Review screenshots in test-results/