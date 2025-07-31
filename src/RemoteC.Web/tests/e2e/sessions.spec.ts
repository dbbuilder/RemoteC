import { test, expect } from '@playwright/test';

test.describe('Session Management', () => {
  // Login before each test
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
    await page.getByLabel('Username').fill('admin');
    await page.getByLabel('Password').fill('admin');
    await page.getByRole('button', { name: 'Sign in' }).click();
    await expect(page).toHaveURL('/dashboard');
  });

  test('should display sessions page', async ({ page }) => {
    await page.getByRole('link', { name: 'Sessions' }).click();
    await expect(page).toHaveURL('/sessions');
    
    // Check page elements
    await expect(page.getByRole('heading', { name: 'Sessions' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Create Session' })).toBeVisible();
    
    // Check filters
    await expect(page.getByRole('button', { name: 'All' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Active' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Completed' })).toBeVisible();
  });

  test('should create a new session', async ({ page }) => {
    await page.getByRole('link', { name: 'Sessions' }).click();
    
    // Click create session
    await page.getByRole('button', { name: 'Create Session' }).click();
    
    // Fill session form (assuming a dialog opens)
    await expect(page.getByRole('dialog')).toBeVisible();
    await page.getByLabel('Device').selectOption({ index: 1 }); // Select first device
    await page.getByLabel('Session Type').selectOption('remote_control');
    
    // Submit form
    await page.getByRole('button', { name: 'Create' }).click();
    
    // Should show success message
    await expect(page.getByText(/Session created successfully/i)).toBeVisible();
  });

  test('should filter sessions by status', async ({ page }) => {
    await page.getByRole('link', { name: 'Sessions' }).click();
    
    // Click Active filter
    await page.getByRole('button', { name: 'Active' }).click();
    
    // Check that only active sessions are shown
    const sessionCards = page.locator('[data-testid="session-card"]');
    const count = await sessionCards.count();
    
    for (let i = 0; i < count; i++) {
      const status = await sessionCards.nth(i).locator('[data-testid="session-status"]').textContent();
      expect(status).toBe('Active');
    }
  });

  test('should show session details', async ({ page }) => {
    await page.getByRole('link', { name: 'Sessions' }).click();
    
    // Click on first session card
    const firstSession = page.locator('[data-testid="session-card"]').first();
    await firstSession.click();
    
    // Should navigate to session details
    await expect(page.url()).toMatch(/\/sessions\/[a-f0-9-]+$/);
    
    // Check details page elements
    await expect(page.getByText('Session Details')).toBeVisible();
    await expect(page.getByText('Device Information')).toBeVisible();
    await expect(page.getByText('Session Log')).toBeVisible();
  });

  test('should connect to a session', async ({ page }) => {
    await page.getByRole('link', { name: 'Sessions' }).click();
    
    // Find a session with "Ready" status
    const readySession = page.locator('[data-testid="session-card"]').filter({
      hasText: 'Ready'
    }).first();
    
    // Click connect button
    await readySession.getByRole('button', { name: 'Connect' }).click();
    
    // Should show connecting state
    await expect(readySession.getByText('Connecting...')).toBeVisible();
    
    // Should eventually show connected state (mock)
    await expect(readySession.getByText('Connected')).toBeVisible({ timeout: 10000 });
  });

  test('should generate PIN for session', async ({ page }) => {
    await page.getByRole('link', { name: 'Sessions' }).click();
    
    // Find a session that supports PIN
    const session = page.locator('[data-testid="session-card"]').first();
    
    // Click generate PIN button
    await session.getByRole('button', { name: 'Generate PIN' }).click();
    
    // Should show PIN dialog
    await expect(page.getByRole('dialog', { name: /PIN Generated/i })).toBeVisible();
    
    // Should display a 6-digit PIN
    const pinText = await page.getByTestId('generated-pin').textContent();
    expect(pinText).toMatch(/^\d{6}$/);
    
    // Close dialog
    await page.getByRole('button', { name: 'Close' }).click();
  });

  test('should stop an active session', async ({ page }) => {
    await page.getByRole('link', { name: 'Sessions' }).click();
    
    // Find an active session
    const activeSession = page.locator('[data-testid="session-card"]').filter({
      hasText: 'Active'
    }).first();
    
    // Click stop button
    await activeSession.getByRole('button', { name: 'Stop' }).click();
    
    // Confirm in dialog
    await page.getByRole('button', { name: 'Yes, stop session' }).click();
    
    // Should show success message
    await expect(page.getByText(/Session stopped successfully/i)).toBeVisible();
    
    // Session status should change
    await expect(activeSession.getByText('Completed')).toBeVisible();
  });
});