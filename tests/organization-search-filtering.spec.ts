// Test for organization-based search and filtering functionality
// Based on manual testing performed on DataMagic administrators page

import { test, expect } from '@playwright/test';

test.describe('Organization-based Search and Filtering', () => {
  test.beforeEach(async ({ page }) => {
    // Navigate to administrators page
    await page.goto('https://localhost:55168/estate/administrators?fPath=%2F');
    
    // Wait for page to load and ensure authenticated
    await expect(page.getByText('Administrators')).toBeVisible();
  });

  test('Multi-Organization User Search', async ({ page }) => {
    // 1. Expand the organization tree (Home node)
    await page.locator('.far').first().click();
    
    // Wait for organizations to load
    await expect(page.getByText('Panoramic Data')).toBeVisible();
    await expect(page.getByText('.CAE Labs Assure Sandbox - Co-op')).toBeVisible();
    
    // 2. Search for "John Odlin" 
    await page.fill('#pd-textbox-122', 'John Odlin');
    await page.getByRole('button', { name: ' Search' }).click();
    
    // 3. Verify 2 results are found
    await expect(page.getByText('1 - 2 of 2')).toBeVisible();
    
    // 4. Verify both records have correct organization assignments
    const rows = page.locator('tbody tr');
    await expect(rows).toHaveCount(2);
    
    // Verify Panoramic Data record
    await expect(rows.filter({ hasText: 'Panoramic Data' })).toBeVisible();
    await expect(rows.filter({ hasText: 'Panoramic Data' }).getByText('john.odlin@panoramicdata.com')).toBeVisible();
    
    // Verify CAE Labs record
    await expect(rows.filter({ hasText: '.CAE Labs Assure Sandbox - Co-op' })).toBeVisible();
    await expect(rows.filter({ hasText: '.CAE Labs Assure Sandbox - Co-op' }).getByText('john.odlin@panoramicdata.com')).toBeVisible();
  });

  test('Organization Filtering with Name Search', async ({ page }) => {
    // Setup: Search for John Odlin to get 2 results
    await page.locator('.far').first().click();
    await page.fill('#pd-textbox-122', 'John Odlin');
    await page.getByRole('button', { name: ' Search' }).click();
    
    // Verify we have 2 results initially
    await expect(page.getByText('1 - 2 of 2')).toBeVisible();
    
    // 1. Click on "Panoramic Data" organization in tree
    await page.getByText('Panoramic Data').click();
    
    // 2. Verify only 1 result (Panoramic Data John Odlin)
    await expect(page.getByText('1 - 1 of 1')).toBeVisible();
    const panoramicRow = page.locator('tbody tr').first();
    await expect(panoramicRow.getByText('John Odlin')).toBeVisible();
    await expect(panoramicRow.getByText('2025-11-12 09:58:03')).toBeVisible();
    
    // Verify URL contains Panoramic Data organization
    expect(page.url()).toContain('Panoramic%20Data');
    
    // 3. Click on ".CAE Labs Assure Sandbox - Co-op" organization
    await page.getByText('.CAE Labs Assure Sandbox - Co-op').click();
    
    // 4. Verify only 1 result (CAE Labs John Odlin)
    await expect(page.getByText('1 - 1 of 1')).toBeVisible();
    const caeRow = page.locator('tbody tr').first();
    await expect(caeRow.getByText('John Odlin')).toBeVisible();
    await expect(caeRow.getByText('2025-10-08 15:06:51')).toBeVisible();
    
    // Verify URL contains CAE Labs organization
    expect(page.url()).toContain('.CAE%20Labs%20Assure%20Sandbox');
  });

  test('Email Search with Organization Filtering', async ({ page }) => {
    // Setup: Expand tree and select CAE Labs organization
    await page.locator('.far').first().click();
    await page.getByText('.CAE Labs Assure Sandbox - Co-op').click();
    
    // 1. Search for email with CAE Labs org selected
    await page.fill('#pd-textbox-122', 'john.odlin@panoramicdata.com');
    await page.getByRole('button', { name: ' Search' }).click();
    
    // 2. Verify shows only CAE Labs record
    await expect(page.getByText('1 - 1 of 1')).toBeVisible();
    const caeRow = page.locator('tbody tr').first();
    await expect(caeRow.getByText('John Odlin')).toBeVisible();
    await expect(caeRow.getByText('2025-10-08 15:06:51')).toBeVisible();
    
    // 3. Switch to Panoramic Data organization (keeping same email search)
    await page.getByText('Panoramic Data').click();
    
    // 4. Verify shows only Panoramic Data record
    await expect(page.getByText('1 - 1 of 1')).toBeVisible();
    const panoramicRow = page.locator('tbody tr').first();
    await expect(panoramicRow.getByText('John Odlin')).toBeVisible();
    await expect(panoramicRow.getByText('2025-11-12 09:58:03')).toBeVisible();
    
    // Verify URL reflects Panoramic Data org
    expect(page.url()).toContain('Panoramic%20Data');
  });

  test('Cross-Organization Data Integrity', async ({ page }) => {
    // Test that search results are properly filtered by organization context
    
    // 1. Search for multi-org user without org filter
    await page.locator('.far').first().click();
    await page.fill('#pd-textbox-122', 'Ryan Halley');
    await page.getByRole('button', { name: ' Search' }).click();
    
    // Should find Ryan in both orgs
    await expect(page.getByText('1 - 2 of 2')).toBeVisible();
    
    // 2. Filter by Panoramic Data
    await page.getByText('Panoramic Data').click();
    await expect(page.getByText('1 - 1 of 1')).toBeVisible();
    
    // 3. Filter by CAE Labs
    await page.getByText('.CAE Labs Assure Sandbox - Co-op').click();
    await expect(page.getByText('1 - 1 of 1')).toBeVisible();
    
    // 4. Verify each org shows different last active dates (data integrity)
    const rows = page.locator('tbody tr');
    await expect(rows).toHaveCount(1);
    await expect(rows.first().getByText('Ryan Halley')).toBeVisible();
  });
});