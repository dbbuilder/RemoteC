# RemoteC E2E Tests

This directory contains end-to-end tests for RemoteC using Playwright.

## Test Coverage

### Authentication (`auth.spec.ts`)
- ✅ Development mode login with any credentials
- ✅ Logout functionality
- ✅ Session persistence on refresh
- ✅ Protected route access control
- ✅ Navigation after authentication

### Session Management (`sessions.spec.ts`)
- ✅ Session listing and filtering
- ✅ Creating new sessions
- ✅ Session details view
- ✅ Connecting to sessions
- ✅ PIN generation
- ✅ Stopping active sessions

### Device Management (`devices.spec.ts`)
- ✅ Device listing and search
- ✅ Status filtering
- ✅ Adding new devices
- ✅ Viewing device details
- ✅ Editing device information
- ✅ Deleting devices
- ✅ Connection status indicators

### Navigation (`navigation.spec.ts`)
- ✅ Sidebar navigation
- ✅ Active link highlighting
- ✅ Browser back/forward
- ✅ Mobile responsive menu
- ✅ 404 page handling

### Dashboard (`dashboard.spec.ts`)
- ✅ Overview statistics
- ✅ Recent activity feed
- ✅ Quick actions
- ✅ System status
- ✅ Data refresh
- ✅ Charts and visualizations

### Theme (`theme.spec.ts`)
- ✅ Light/dark mode toggle
- ✅ Theme persistence
- ✅ Component styling consistency

## Running Tests

### Run all tests
```bash
npm run test:e2e
```

### Run tests with UI
```bash
npm run test:e2e:ui
```

### Debug tests
```bash
npm run test:e2e:debug
```

### Run specific test file
```bash
npx playwright test tests/e2e/auth.spec.ts
```

### Run in headed mode (see browser)
```bash
npx playwright test --headed
```

## Configuration

Tests are configured in `playwright.config.ts`:
- Base URL: `http://localhost:17002`
- Auto-starts API server on port 17001
- Auto-starts UI dev server on port 17002
- Runs in Chrome, Firefox, Safari, and mobile viewports
- Takes screenshots on failure
- Records traces for debugging

## Writing New Tests

1. Create a new `.spec.ts` file in this directory
2. Import Playwright test utilities:
   ```typescript
   import { test, expect } from '@playwright/test';
   ```
3. Group related tests with `test.describe()`
4. Use `test.beforeEach()` for common setup
5. Write individual tests with `test()`

### Best Practices

1. **Use data-testid attributes** for reliable element selection
2. **Wait for elements** with `expect().toBeVisible()`
3. **Use descriptive test names** that explain what is being tested
4. **Keep tests independent** - each test should work in isolation
5. **Clean up after tests** if they create data

## Debugging Failed Tests

1. **View test report**:
   ```bash
   npx playwright show-report
   ```

2. **Run specific test in debug mode**:
   ```bash
   npx playwright test auth.spec.ts --debug
   ```

3. **Use trace viewer**:
   ```bash
   npx playwright show-trace trace.zip
   ```

4. **Take screenshots during test**:
   ```typescript
   await page.screenshot({ path: 'debug.png' });
   ```

## CI/CD Integration

The tests are configured to:
- Run with retries on CI
- Use single worker on CI
- Generate HTML reports
- Take screenshots on failure
- Record traces for failed tests

Add to your CI pipeline:
```yaml
- name: Install dependencies
  run: npm ci
  
- name: Install Playwright browsers
  run: npx playwright install
  
- name: Run E2E tests
  run: npm run test:e2e
  
- name: Upload test results
  if: always()
  uses: actions/upload-artifact@v2
  with:
    name: playwright-report
    path: playwright-report/
```