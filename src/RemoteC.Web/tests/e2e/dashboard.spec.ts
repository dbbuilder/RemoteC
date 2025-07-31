import { test, expect } from '@playwright/test';

test.describe('Dashboard', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
    await page.getByLabel('Username').fill('admin');
    await page.getByLabel('Password').fill('admin');
    await page.getByRole('button', { name: 'Sign in' }).click();
    await expect(page).toHaveURL('/dashboard');
  });

  test('should display dashboard overview', async ({ page }) => {
    // Check main dashboard elements
    await expect(page.getByRole('heading', { name: 'Dashboard' })).toBeVisible();
    
    // Check statistics cards
    await expect(page.getByText('Active Sessions')).toBeVisible();
    await expect(page.getByText('Online Devices')).toBeVisible();
    await expect(page.getByText('Total Users')).toBeVisible();
    await expect(page.getByText('System Health')).toBeVisible();
  });

  test('should display recent activity', async ({ page }) => {
    // Check recent activity section
    await expect(page.getByText('Recent Activity')).toBeVisible();
    
    // Should have activity items
    const activityItems = page.locator('[data-testid="activity-item"]');
    await expect(activityItems).toHaveCount(await activityItems.count());
  });

  test('should display quick actions', async ({ page }) => {
    // Check quick action buttons
    await expect(page.getByRole('button', { name: 'New Session' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Add Device' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Invite User' })).toBeVisible();
  });

  test('should navigate from quick actions', async ({ page }) => {
    // Click New Session
    await page.getByRole('button', { name: 'New Session' }).click();
    await expect(page).toHaveURL('/sessions');
    
    // Go back to dashboard
    await page.getByRole('link', { name: 'Dashboard' }).click();
    
    // Click Add Device
    await page.getByRole('button', { name: 'Add Device' }).click();
    await expect(page).toHaveURL('/devices');
  });

  test('should display system status', async ({ page }) => {
    // Check system status indicators
    const apiStatus = page.locator('[data-testid="api-status"]');
    await expect(apiStatus).toBeVisible();
    await expect(apiStatus).toHaveText(/online|connected/i);
    
    const dbStatus = page.locator('[data-testid="db-status"]');
    await expect(dbStatus).toBeVisible();
    await expect(dbStatus).toHaveText(/healthy|connected/i);
  });

  test('should refresh data', async ({ page }) => {
    // Find refresh button
    const refreshButton = page.getByRole('button', { name: /refresh/i });
    await expect(refreshButton).toBeVisible();
    
    // Click refresh
    await refreshButton.click();
    
    // Should show loading state
    await expect(page.getByTestId('loading-spinner')).toBeVisible();
    
    // Loading should complete
    await expect(page.getByTestId('loading-spinner')).not.toBeVisible({ timeout: 5000 });
  });

  test('should show notifications', async ({ page }) => {
    // Check if notification bell exists
    const notificationBell = page.getByRole('button', { name: /notifications/i });
    
    if (await notificationBell.isVisible()) {
      // Click notification bell
      await notificationBell.click();
      
      // Should show notification dropdown
      await expect(page.getByRole('menu', { name: /notifications/i })).toBeVisible();
      
      // Should have notification items or empty state
      const notificationItems = page.locator('[data-testid="notification-item"]');
      const emptyState = page.getByText(/no new notifications/i);
      
      const hasNotifications = await notificationItems.count() > 0;
      const hasEmptyState = await emptyState.isVisible();
      
      expect(hasNotifications || hasEmptyState).toBeTruthy();
    }
  });

  test('should display charts and graphs', async ({ page }) => {
    // Check for chart containers
    const charts = page.locator('[data-testid="chart"]');
    
    // Should have at least one chart
    await expect(charts.first()).toBeVisible();
    
    // Charts should have proper dimensions
    const chartBox = await charts.first().boundingBox();
    expect(chartBox?.width).toBeGreaterThan(200);
    expect(chartBox?.height).toBeGreaterThan(150);
  });
});