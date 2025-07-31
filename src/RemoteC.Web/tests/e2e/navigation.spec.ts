import { test, expect } from '@playwright/test';

test.describe('Navigation', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
    await page.getByLabel('Username').fill('admin');
    await page.getByLabel('Password').fill('admin');
    await page.getByRole('button', { name: 'Sign in' }).click();
    await expect(page).toHaveURL('/dashboard');
  });

  test('should have working navigation sidebar', async ({ page }) => {
    // Check all navigation links are visible
    await expect(page.getByRole('link', { name: 'Dashboard' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Sessions' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Devices' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Users' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Audit Logs' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Settings' })).toBeVisible();
  });

  test('should highlight active navigation item', async ({ page }) => {
    // Dashboard should be active initially
    const dashboardLink = page.getByRole('link', { name: 'Dashboard' });
    await expect(dashboardLink).toHaveClass(/bg-primary/);
    
    // Navigate to Sessions
    await page.getByRole('link', { name: 'Sessions' }).click();
    await expect(page).toHaveURL('/sessions');
    
    // Sessions should now be active
    const sessionsLink = page.getByRole('link', { name: 'Sessions' });
    await expect(sessionsLink).toHaveClass(/bg-primary/);
    
    // Dashboard should not be active
    await expect(dashboardLink).not.toHaveClass(/bg-primary/);
  });

  test('should navigate with browser back/forward', async ({ page }) => {
    // Navigate through pages
    await page.getByRole('link', { name: 'Sessions' }).click();
    await expect(page).toHaveURL('/sessions');
    
    await page.getByRole('link', { name: 'Devices' }).click();
    await expect(page).toHaveURL('/devices');
    
    await page.getByRole('link', { name: 'Users' }).click();
    await expect(page).toHaveURL('/users');
    
    // Go back
    await page.goBack();
    await expect(page).toHaveURL('/devices');
    
    await page.goBack();
    await expect(page).toHaveURL('/sessions');
    
    // Go forward
    await page.goForward();
    await expect(page).toHaveURL('/devices');
  });

  test('should handle mobile navigation', async ({ page }) => {
    // Set mobile viewport
    await page.setViewportSize({ width: 375, height: 667 });
    
    // Mobile menu button should be visible
    const menuButton = page.getByRole('button', { name: 'Menu' });
    await expect(menuButton).toBeVisible();
    
    // Sidebar should be hidden
    const sidebar = page.locator('nav').first();
    await expect(sidebar).toHaveClass(/-translate-x-full/);
    
    // Click menu button to open sidebar
    await menuButton.click();
    
    // Sidebar should be visible
    await expect(sidebar).not.toHaveClass(/-translate-x-full/);
    
    // Click a link
    await page.getByRole('link', { name: 'Sessions' }).click();
    
    // Sidebar should close automatically
    await expect(sidebar).toHaveClass(/-translate-x-full/);
    
    // Should navigate to sessions
    await expect(page).toHaveURL('/sessions');
  });

  test('should handle 404 pages', async ({ page }) => {
    // Navigate to non-existent page
    await page.goto('/non-existent-page');
    
    // Should redirect to 404 page
    await expect(page).toHaveURL('/404');
    
    // Should show 404 content
    await expect(page.getByText('404')).toBeVisible();
    await expect(page.getByText(/page not found/i)).toBeVisible();
    
    // Should have link back to dashboard
    await page.getByRole('link', { name: /back to dashboard/i }).click();
    await expect(page).toHaveURL('/dashboard');
  });
});