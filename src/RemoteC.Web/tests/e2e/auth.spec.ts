import { test, expect } from '@playwright/test';

test.describe('Authentication', () => {
  test.describe('Development Mode', () => {
    test('should show development login page', async ({ page }) => {
      await page.goto('/');
      
      // Check for development mode indicators
      await expect(page.getByText('Development Mode')).toBeVisible();
      await expect(page.getByText('Authentication is simplified')).toBeVisible();
      
      // Check login form elements
      await expect(page.getByLabel('Username')).toBeVisible();
      await expect(page.getByLabel('Password')).toBeVisible();
      await expect(page.getByRole('button', { name: 'Sign in' })).toBeVisible();
    });

    test('should login with any credentials', async ({ page }) => {
      await page.goto('/');
      
      // Fill in any credentials
      await page.getByLabel('Username').fill('testuser');
      await page.getByLabel('Password').fill('testpass');
      
      // Click sign in
      await page.getByRole('button', { name: 'Sign in' }).click();
      
      // Should redirect to dashboard
      await expect(page).toHaveURL('/dashboard');
      
      // Should show DEV badge
      await expect(page.getByText('DEV')).toBeVisible();
      
      // Should show user menu with username
      await expect(page.getByText('testuser')).toBeVisible();
    });

    test('should logout successfully', async ({ page }) => {
      // First login
      await page.goto('/');
      await page.getByLabel('Username').fill('testuser');
      await page.getByLabel('Password').fill('testpass');
      await page.getByRole('button', { name: 'Sign in' }).click();
      
      // Wait for dashboard
      await expect(page).toHaveURL('/dashboard');
      
      // Click user avatar to open menu
      await page.getByRole('button', { name: /testuser/i }).click();
      
      // Click logout
      await page.getByRole('menuitem', { name: 'Log out' }).click();
      
      // Should redirect to login
      await expect(page).toHaveURL('/');
      await expect(page.getByText('Development Mode')).toBeVisible();
    });

    test('should persist authentication on refresh', async ({ page }) => {
      // Login
      await page.goto('/');
      await page.getByLabel('Username').fill('testuser');
      await page.getByLabel('Password').fill('testpass');
      await page.getByRole('button', { name: 'Sign in' }).click();
      
      // Wait for dashboard
      await expect(page).toHaveURL('/dashboard');
      
      // Refresh page
      await page.reload();
      
      // Should still be on dashboard
      await expect(page).toHaveURL('/dashboard');
      await expect(page.getByText('testuser')).toBeVisible();
    });
  });

  test.describe('Protected Routes', () => {
    test('should redirect to login when not authenticated', async ({ page }) => {
      // Try to access protected route directly
      await page.goto('/sessions');
      
      // Should redirect to login
      await expect(page).toHaveURL('/');
      await expect(page.getByText('Development Mode')).toBeVisible();
    });

    test('should access protected routes when authenticated', async ({ page }) => {
      // Login first
      await page.goto('/');
      await page.getByLabel('Username').fill('admin');
      await page.getByLabel('Password').fill('admin');
      await page.getByRole('button', { name: 'Sign in' }).click();
      
      // Navigate to different routes
      await page.getByRole('link', { name: 'Sessions' }).click();
      await expect(page).toHaveURL('/sessions');
      
      await page.getByRole('link', { name: 'Devices' }).click();
      await expect(page).toHaveURL('/devices');
      
      await page.getByRole('link', { name: 'Users' }).click();
      await expect(page).toHaveURL('/users');
      
      await page.getByRole('link', { name: 'Audit Logs' }).click();
      await expect(page).toHaveURL('/audit');
      
      await page.getByRole('link', { name: 'Settings' }).click();
      await expect(page).toHaveURL('/settings');
    });
  });
});