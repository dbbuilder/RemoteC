import { test, expect } from '@playwright/test';

test.describe('Device Management', () => {
  // Login before each test
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
    await page.getByLabel('Username').fill('admin');
    await page.getByLabel('Password').fill('admin');
    await page.getByRole('button', { name: 'Sign in' }).click();
    await expect(page).toHaveURL('/dashboard');
  });

  test('should display devices page', async ({ page }) => {
    await page.getByRole('link', { name: 'Devices' }).click();
    await expect(page).toHaveURL('/devices');
    
    // Check page elements
    await expect(page.getByRole('heading', { name: 'Devices' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Add Device' })).toBeVisible();
    
    // Check search and filter
    await expect(page.getByPlaceholder('Search devices...')).toBeVisible();
    await expect(page.getByRole('combobox', { name: 'Status filter' })).toBeVisible();
  });

  test('should search for devices', async ({ page }) => {
    await page.getByRole('link', { name: 'Devices' }).click();
    
    // Type in search box
    await page.getByPlaceholder('Search devices...').fill('Server');
    
    // Wait for results to filter
    await page.waitForTimeout(500);
    
    // Check that only matching devices are shown
    const deviceCards = page.locator('[data-testid="device-card"]');
    const count = await deviceCards.count();
    
    for (let i = 0; i < count; i++) {
      const deviceName = await deviceCards.nth(i).locator('[data-testid="device-name"]').textContent();
      expect(deviceName?.toLowerCase()).toContain('server');
    }
  });

  test('should filter devices by status', async ({ page }) => {
    await page.getByRole('link', { name: 'Devices' }).click();
    
    // Select "Online" from status filter
    await page.getByRole('combobox', { name: 'Status filter' }).selectOption('online');
    
    // Wait for filter to apply
    await page.waitForTimeout(500);
    
    // Check that only online devices are shown
    const deviceCards = page.locator('[data-testid="device-card"]');
    const count = await deviceCards.count();
    
    for (let i = 0; i < count; i++) {
      const status = await deviceCards.nth(i).locator('[data-testid="device-status"]').textContent();
      expect(status).toBe('Online');
    }
  });

  test('should add a new device', async ({ page }) => {
    await page.getByRole('link', { name: 'Devices' }).click();
    
    // Click add device button
    await page.getByRole('button', { name: 'Add Device' }).click();
    
    // Fill device form
    await expect(page.getByRole('dialog', { name: 'Add Device' })).toBeVisible();
    await page.getByLabel('Device Name').fill('Test Device');
    await page.getByLabel('Device Type').selectOption('desktop');
    await page.getByLabel('Operating System').selectOption('windows');
    await page.getByLabel('Description').fill('Test device for E2E testing');
    
    // Submit form
    await page.getByRole('button', { name: 'Add Device' }).last().click();
    
    // Should show success message
    await expect(page.getByText(/Device added successfully/i)).toBeVisible();
    
    // New device should appear in list
    await expect(page.getByText('Test Device')).toBeVisible();
  });

  test('should view device details', async ({ page }) => {
    await page.getByRole('link', { name: 'Devices' }).click();
    
    // Click on first device card
    const firstDevice = page.locator('[data-testid="device-card"]').first();
    const deviceName = await firstDevice.locator('[data-testid="device-name"]').textContent();
    await firstDevice.click();
    
    // Should show device details dialog
    await expect(page.getByRole('dialog', { name: 'Device Details' })).toBeVisible();
    
    // Check details are displayed
    await expect(page.getByText(deviceName!)).toBeVisible();
    await expect(page.getByText('System Information')).toBeVisible();
    await expect(page.getByText('Connection Details')).toBeVisible();
    await expect(page.getByText('Recent Sessions')).toBeVisible();
  });

  test('should edit device information', async ({ page }) => {
    await page.getByRole('link', { name: 'Devices' }).click();
    
    // Click on first device's edit button
    const firstDevice = page.locator('[data-testid="device-card"]').first();
    await firstDevice.getByRole('button', { name: 'Edit' }).click();
    
    // Edit form should appear
    await expect(page.getByRole('dialog', { name: 'Edit Device' })).toBeVisible();
    
    // Update device name
    await page.getByLabel('Device Name').fill('Updated Device Name');
    await page.getByLabel('Description').fill('Updated description');
    
    // Save changes
    await page.getByRole('button', { name: 'Save Changes' }).click();
    
    // Should show success message
    await expect(page.getByText(/Device updated successfully/i)).toBeVisible();
    
    // Updated name should be visible
    await expect(page.getByText('Updated Device Name')).toBeVisible();
  });

  test('should delete a device', async ({ page }) => {
    await page.getByRole('link', { name: 'Devices' }).click();
    
    // Get count of devices before deletion
    const initialCount = await page.locator('[data-testid="device-card"]').count();
    
    // Click delete on first device
    const firstDevice = page.locator('[data-testid="device-card"]').first();
    const deviceName = await firstDevice.locator('[data-testid="device-name"]').textContent();
    await firstDevice.getByRole('button', { name: 'Delete' }).click();
    
    // Confirm deletion
    await expect(page.getByRole('dialog', { name: 'Confirm Deletion' })).toBeVisible();
    await page.getByRole('button', { name: 'Yes, delete device' }).click();
    
    // Should show success message
    await expect(page.getByText(/Device deleted successfully/i)).toBeVisible();
    
    // Device should be removed from list
    await expect(page.getByText(deviceName!)).not.toBeVisible();
    
    // Count should decrease
    const finalCount = await page.locator('[data-testid="device-card"]').count();
    expect(finalCount).toBe(initialCount - 1);
  });

  test('should show device connection status', async ({ page }) => {
    await page.getByRole('link', { name: 'Devices' }).click();
    
    // Check different status indicators
    const onlineDevices = page.locator('[data-testid="device-card"]').filter({
      has: page.locator('[data-testid="device-status"]:has-text("Online")')
    });
    
    const offlineDevices = page.locator('[data-testid="device-card"]').filter({
      has: page.locator('[data-testid="device-status"]:has-text("Offline")')
    });
    
    // Online devices should have green indicator
    if (await onlineDevices.count() > 0) {
      const indicator = onlineDevices.first().locator('[data-testid="status-indicator"]');
      await expect(indicator).toHaveClass(/bg-green-500/);
    }
    
    // Offline devices should have gray indicator
    if (await offlineDevices.count() > 0) {
      const indicator = offlineDevices.first().locator('[data-testid="status-indicator"]');
      await expect(indicator).toHaveClass(/bg-gray-500/);
    }
  });
});