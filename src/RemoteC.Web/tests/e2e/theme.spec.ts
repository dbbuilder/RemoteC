import { test, expect } from '@playwright/test';

test.describe('Theme Switching', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
    await page.getByLabel('Username').fill('admin');
    await page.getByLabel('Password').fill('admin');
    await page.getByRole('button', { name: 'Sign in' }).click();
    await expect(page).toHaveURL('/dashboard');
  });

  test('should toggle between light and dark themes', async ({ page }) => {
    // Find theme toggle button
    const themeToggle = page.getByRole('button', { name: /theme|sun|moon/i });
    await expect(themeToggle).toBeVisible();
    
    // Get initial theme (check if dark class exists on html)
    const htmlElement = page.locator('html');
    const isDarkInitially = await htmlElement.evaluate(el => el.classList.contains('dark'));
    
    // Click theme toggle
    await themeToggle.click();
    
    // Theme should change
    const isDarkAfterToggle = await htmlElement.evaluate(el => el.classList.contains('dark'));
    expect(isDarkAfterToggle).toBe(!isDarkInitially);
    
    // Icon should change
    if (isDarkAfterToggle) {
      await expect(page.locator('[data-icon="moon"]')).toBeVisible();
    } else {
      await expect(page.locator('[data-icon="sun"]')).toBeVisible();
    }
    
    // Toggle back
    await themeToggle.click();
    
    // Should return to original theme
    const isDarkFinal = await htmlElement.evaluate(el => el.classList.contains('dark'));
    expect(isDarkFinal).toBe(isDarkInitially);
  });

  test('should persist theme preference', async ({ page, context }) => {
    // Set to light theme
    const themeToggle = page.getByRole('button', { name: /theme|sun|moon/i });
    const htmlElement = page.locator('html');
    
    // Ensure we're in light mode
    const isDark = await htmlElement.evaluate(el => el.classList.contains('dark'));
    if (isDark) {
      await themeToggle.click();
    }
    
    // Verify light mode
    await expect(htmlElement).not.toHaveClass(/dark/);
    
    // Reload page
    await page.reload();
    
    // Should still be in light mode
    await expect(htmlElement).not.toHaveClass(/dark/);
    
    // Open new page in same context
    const newPage = await context.newPage();
    await newPage.goto('/');
    
    // Should also be in light mode
    const newHtmlElement = newPage.locator('html');
    await expect(newHtmlElement).not.toHaveClass(/dark/);
    
    await newPage.close();
  });

  test('should apply theme to all components', async ({ page }) => {
    const htmlElement = page.locator('html');
    
    // Switch to light theme
    const isDark = await htmlElement.evaluate(el => el.classList.contains('dark'));
    if (isDark) {
      const themeToggle = page.getByRole('button', { name: /theme|sun|moon/i });
      await themeToggle.click();
    }
    
    // Check background colors in light mode
    const bodyBg = await page.locator('body').evaluate(el => 
      window.getComputedStyle(el).backgroundColor
    );
    
    // Light mode should have light background (high RGB values)
    const rgbMatch = bodyBg.match(/\d+/g);
    if (rgbMatch) {
      const [r, g, b] = rgbMatch.map(Number);
      expect(r + g + b).toBeGreaterThan(600); // Light color
    }
    
    // Check text colors
    const textColor = await page.locator('h1').first().evaluate(el => 
      window.getComputedStyle(el).color
    );
    
    // Light mode should have dark text (low RGB values)
    const textRgbMatch = textColor.match(/\d+/g);
    if (textRgbMatch) {
      const [r, g, b] = textRgbMatch.map(Number);
      expect(r + g + b).toBeLessThan(200); // Dark color
    }
  });
});