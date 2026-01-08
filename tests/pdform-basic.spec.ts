import { test, expect } from '@playwright/test';

test.describe('Form Components and Input Controls', () => {
  test('PDForm Basic Functionality', async ({ page }) => {
    // 1. Navigate to http://localhost:5000/
    await page.goto('/');
    
    // 2. Click on 'PDForm' in the navigation menu
    await page.getByRole('link', { name: 'PDForm' }).click();
    
    // 3. Verify the form renders with various input field types
    await expect(page.locator('form, .pd-form')).toBeVisible();
    
    // 4. Test text input fields for basic text entry
    const textInputs = page.locator('input[type="text"], .form-control');
    if (await textInputs.count() > 0) {
      await textInputs.first().fill('Test input value');
      await expect(textInputs.first()).toHaveValue('Test input value');
    }
    
    // 5. Test validation messages appear for invalid input
    const requiredFields = page.locator('input[required], .required');
    if (await requiredFields.count() > 0) {
      await requiredFields.first().fill('');
      await requiredFields.first().blur();
      // Check for validation messages
      await expect(page.locator('.validation-message, .error, .invalid-feedback')).toBeVisible();
    }
    
    // 6. Test form submission with valid data
    const submitButton = page.locator('button[type="submit"], .btn-primary').filter({ hasText: /submit|save|send/i }).first();
    if (await submitButton.isVisible()) {
      await submitButton.click();
    }
    
    // 7. Test form reset/clear functionality
    const resetButton = page.locator('button[type="reset"], .btn').filter({ hasText: /reset|clear/i }).first();
    if (await resetButton.isVisible()) {
      await resetButton.click();
    }
    
    // 8. Verify form accessibility with keyboard navigation
    await page.keyboard.press('Tab');
    await expect(page.locator(':focus')).toBeVisible();
  });
});
