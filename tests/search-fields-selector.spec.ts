// Test for Search Fields selector functionality
// Based on manual testing performed on DataMagic administrators page

import { test, expect } from '@playwright/test';

test.describe('Search Fields Selector Functionality', () => {
  test.beforeEach(async ({ page }) => {
    // Navigate to Panoramic Data organization administrators
    await page.goto('https://localhost:55168/estate/administrators?fPath=%2FPanoramic%20Data');
    
    // Wait for page to load
    await expect(page.getByText('Administrators')).toBeVisible();
    await expect(page.getByText('Showing')).toBeVisible();
  });

  test('Search Fields Configuration Display', async ({ page }) => {
    // 1. Click "Search Fields" button to open dropdown
    await page.getByRole('button', { name: ' Search Fields ' }).click();
    
    // 2. Verify 9 total fields shown
    await expect(page.getByText('Text Fields')).toBeVisible();
    await expect(page.getByText('Boolean Fields')).toBeVisible();
    
    // Text Fields
    await expect(page.getByText('Account Status')).toBeVisible();
    await expect(page.getByText('Email')).toBeVisible();
    await expect(page.getByText('Name')).toBeVisible();
    await expect(page.getByText('Tags')).toBeVisible();
    await expect(page.getByText('Authentication Method')).toBeVisible();
    await expect(page.getByText('Meraki Id')).toBeVisible();
    await expect(page.getByText('Organization Name')).toBeVisible();
    
    // Boolean Fields
    await expect(page.getByText('Has Api Key')).toBeVisible();
    await expect(page.getByText('Two Factor Auth Enabled')).toBeVisible();
    
    // 3. Verify 4 default fields are enabled
    await expect(page.getByText('4 of 9 fields enabled')).toBeVisible();
    
    // Verify default fields are checked
    await expect(page.getByRole('checkbox', { name: 'Account Status' })).toBeChecked();
    await expect(page.getByRole('checkbox', { name: 'Email' })).toBeChecked();
    await expect(page.getByRole('checkbox', { name: 'Name' })).toBeChecked();
    await expect(page.getByRole('checkbox', { name: 'Tags' })).toBeChecked();
    
    // Verify non-default fields are unchecked
    await expect(page.getByRole('checkbox', { name: 'Has Api Key' })).not.toBeChecked();
    await expect(page.getByRole('checkbox', { name: 'Two Factor Auth Enabled' })).not.toBeChecked();
  });

  test('Field-Specific Search Control', async ({ page }) => {
    // 1. Open Search Fields and click "None" button
    await page.getByRole('button', { name: ' Search Fields ' }).click();
    await page.getByRole('button', { name: ' None' }).click();
    
    // 2. Verify 0 of 9 fields enabled
    await expect(page.getByText('0 of 9 fields enabled')).toBeVisible();
    
    // Verify all checkboxes are unchecked
    await expect(page.getByRole('checkbox', { name: 'Name' })).not.toBeChecked();
    await expect(page.getByRole('checkbox', { name: 'Email' })).not.toBeChecked();
    
    // 3. Enable only "Name" field
    await page.getByRole('checkbox', { name: 'Name' }).click();
    
    // 4. Verify 1 of 9 fields enabled
    await expect(page.getByText('1 of 9 fields enabled')).toBeVisible();
    await expect(page.getByRole('checkbox', { name: 'Name' })).toBeChecked();
    
    // 5. Close dropdown and test name search
    await page.getByRole('button', { name: ' Search Fields ' }).click();
    await page.fill('#pd-textbox-122', 'Roland');
    await page.getByRole('button', { name: ' Search' }).click();
    
    // 6. Verify Roland Banks found
    await expect(page.getByText('1 - 1 of 1')).toBeVisible();
    await expect(page.getByText('Roland Banks')).toBeVisible();
    
    // 7. Clear search and test email search (should fail)
    await page.getByRole('button').nth(1).click(); // Clear button
    await page.fill('#pd-textbox-122', 'roland.banks@panoramicdata.com');
    await page.getByRole('button', { name: ' Search' }).click();
    
    // 8. Verify "No data" (email not searched)
    await expect(page.getByText('No data')).toBeVisible();
  });

  test('Multi-Field Search Dynamic Updates', async ({ page }) => {
    // Setup: Enable only Name field and search by email (should fail)
    await page.getByRole('button', { name: ' Search Fields ' }).click();
    await page.getByRole('button', { name: ' None' }).click();
    await page.getByRole('checkbox', { name: 'Name' }).click();
    await page.getByRole('button', { name: ' Search Fields ' }).click();
    
    await page.fill('#pd-textbox-122', 'roland.banks@panoramicdata.com');
    await page.getByRole('button', { name: ' Search' }).click();
    await expect(page.getByText('No data')).toBeVisible();
    
    // 1. Re-open Search Fields dropdown
    await page.getByRole('button', { name: ' Search Fields ' }).click();
    
    // 2. Enable "Email" field (Name already enabled)
    await page.getByRole('checkbox', { name: 'Email' }).click();
    
    // 3. Verify 2 of 9 fields enabled
    await expect(page.getByText('2 of 9 fields enabled')).toBeVisible();
    
    // 4. Verify results immediately update to show Roland Banks
    // (Results should update dynamically while dropdown is still open)
    await expect(page.getByText('1 - 1 of 1')).toBeVisible();
    
    // 5. Close dropdown and verify email search now works
    await page.getByRole('button', { name: ' Search Fields ' }).click();
    await expect(page.getByText('Roland Banks')).toBeVisible();
    await expect(page.getByText('roland.banks@panoramicdata.com')).toBeVisible();
  });

  test('Restore Defaults Functionality', async ({ page }) => {
    // Setup: Modify field selection
    await page.getByRole('button', { name: ' Search Fields ' }).click();
    await page.getByRole('button', { name: ' None' }).click();
    await page.getByRole('checkbox', { name: 'Name' }).click();
    await page.getByRole('checkbox', { name: 'Email' }).click();
    
    // Verify custom selection (2 fields)
    await expect(page.getByText('2 of 9 fields enabled')).toBeVisible();
    
    // 1. Click "Defaults" button
    await page.getByRole('button', { name: ' Defaults' }).click();
    
    // 2. Verify 4 of 9 fields enabled (restored defaults)
    await expect(page.getByText('4 of 9 fields enabled')).toBeVisible();
    
    // 3. Verify correct default fields are enabled
    await expect(page.getByRole('checkbox', { name: 'Account Status' })).toBeChecked();
    await expect(page.getByRole('checkbox', { name: 'Email' })).toBeChecked();
    await expect(page.getByRole('checkbox', { name: 'Name' })).toBeChecked();
    await expect(page.getByRole('checkbox', { name: 'Tags' })).toBeChecked();
    
    // Verify non-default fields are unchecked
    await expect(page.getByRole('checkbox', { name: 'Authentication Method' })).not.toBeChecked();
    await expect(page.getByRole('checkbox', { name: 'Has Api Key' })).not.toBeChecked();
    
    // 4. Close dropdown and verify search still works
    await page.getByRole('button', { name: ' Search Fields ' }).click();
    await page.fill('#pd-textbox-122', 'John');
    await page.getByRole('button', { name: ' Search' }).click();
    
    await expect(page.getByText('John Odlin')).toBeVisible();
  });

  test('All and None Button Functionality', async ({ page }) => {
    await page.getByRole('button', { name: ' Search Fields ' }).click();
    
    // Test "All" button
    await page.getByRole('button', { name: ' All' }).click();
    await expect(page.getByText('9 of 9 fields enabled')).toBeVisible();
    
    // Verify all checkboxes are checked
    await expect(page.getByRole('checkbox', { name: 'Account Status' })).toBeChecked();
    await expect(page.getByRole('checkbox', { name: 'Email' })).toBeChecked();
    await expect(page.getByRole('checkbox', { name: 'Name' })).toBeChecked();
    await expect(page.getByRole('checkbox', { name: 'Tags' })).toBeChecked();
    await expect(page.getByRole('checkbox', { name: 'Authentication Method' })).toBeChecked();
    await expect(page.getByRole('checkbox', { name: 'Meraki Id' })).toBeChecked();
    await expect(page.getByRole('checkbox', { name: 'Organization Name' })).toBeChecked();
    await expect(page.getByRole('checkbox', { name: 'Has Api Key' })).toBeChecked();
    await expect(page.getByRole('checkbox', { name: 'Two Factor Auth Enabled' })).toBeChecked();
    
    // Test "None" button
    await page.getByRole('button', { name: ' None' }).click();
    await expect(page.getByText('0 of 9 fields enabled')).toBeVisible();
    
    // Verify all checkboxes are unchecked
    await expect(page.getByRole('checkbox', { name: 'Account Status' })).not.toBeChecked();
    await expect(page.getByRole('checkbox', { name: 'Email' })).not.toBeChecked();
    await expect(page.getByRole('checkbox', { name: 'Name' })).not.toBeChecked();
  });

  test('Search Field Selection Persistence', async ({ page }) => {
    // 1. Modify search fields
    await page.getByRole('button', { name: ' Search Fields ' }).click();
    await page.getByRole('button', { name: ' None' }).click();
    await page.getByRole('checkbox', { name: 'Name' }).click();
    await page.getByRole('checkbox', { name: 'Tags' }).click();
    await page.getByRole('button', { name: ' Search Fields ' }).click();
    
    // 2. Perform a search
    await page.fill('#pd-textbox-122', 'John');
    await page.getByRole('button', { name: ' Search' }).click();
    
    // 3. Re-open dropdown and verify selection persisted
    await page.getByRole('button', { name: ' Search Fields ' }).click();
    await expect(page.getByText('2 of 9 fields enabled')).toBeVisible();
    await expect(page.getByRole('checkbox', { name: 'Name' })).toBeChecked();
    await expect(page.getByRole('checkbox', { name: 'Tags' })).toBeChecked();
    await expect(page.getByRole('checkbox', { name: 'Email' })).not.toBeChecked();
    await expect(page.getByRole('checkbox', { name: 'Account Status' })).not.toBeChecked();
  });
});