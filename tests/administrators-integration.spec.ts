// Comprehensive integration tests for administrators table functionality
// Tests the interaction between organization filtering and search field selection

import { test, expect } from '@playwright/test';

test.describe('Administrators Table - Integration Tests', () => {
  test.beforeEach(async ({ page }) => {
    // Navigate to administrators page
    await page.goto('https://localhost:55168/estate/administrators?fPath=%2F');
    await expect(page.getByText('Administrators')).toBeVisible();
  });

  test('Organization Filtering with Custom Search Fields', async ({ page }) => {
    // 1. Expand organization tree and select Panoramic Data
    await page.locator('.far').first().click();
    await page.getByText('Panoramic Data').click();
    
    // 2. Configure search fields to only search in Email field
    await page.getByRole('button', { name: ' Search Fields ' }).click();
    await page.getByRole('button', { name: ' None' }).click();
    await page.getByRole('checkbox', { name: 'Email' }).click();
    await page.getByRole('button', { name: ' Search Fields ' }).click();
    
    // 3. Search for john.odlin (should find by email)
    await page.fill('#pd-textbox-122', 'john.odlin');
    await page.getByRole('button', { name: ' Search' }).click();
    
    // 4. Verify found John Odlin from Panoramic Data only
    await expect(page.getByText('1 - 1 of 1')).toBeVisible();
    const row = page.locator('tbody tr').first();
    await expect(row.getByText('John Odlin')).toBeVisible();
    await expect(row.getByText('john.odlin@panoramicdata.com')).toBeVisible();
    await expect(row.getByText('2025-11-12 09:58:03')).toBeVisible();
    
    // 5. Verify URL shows Panoramic Data organization
    expect(page.url()).toContain('Panoramic%20Data');
  });

  test('Search Field Changes with Organization Context', async ({ page }) => {
    // Test that search field configuration works across different organizations
    
    // 1. Configure search fields for Name only
    await page.getByRole('button', { name: ' Search Fields ' }).click();
    await page.getByRole('button', { name: ' None' }).click();
    await page.getByRole('checkbox', { name: 'Name' }).click();
    await page.getByRole('button', { name: ' Search Fields ' }).click();
    
    // 2. Expand tree and search for "Ryan Halley"
    await page.locator('.far').first().click();
    await page.fill('#pd-textbox-122', 'Ryan Halley');
    await page.getByRole('button', { name: ' Search' }).click();
    
    // Should find 2 results (both organizations)
    await expect(page.getByText('1 - 2 of 2')).toBeVisible();
    
    // 3. Filter by Panoramic Data
    await page.getByText('Panoramic Data').click();
    await expect(page.getByText('1 - 1 of 1')).toBeVisible();
    
    // 4. Verify search field configuration persisted through org change
    await page.getByRole('button', { name: ' Search Fields ' }).click();
    await expect(page.getByText('1 of 9 fields enabled')).toBeVisible();
    await expect(page.getByRole('checkbox', { name: 'Name' })).toBeChecked();
    await expect(page.getByRole('checkbox', { name: 'Email' })).not.toBeChecked();
    
    // 5. Enable Email field and verify email search works in this org context
    await page.getByRole('checkbox', { name: 'Email' }).click();
    await page.getByRole('button', { name: ' Search Fields ' }).click();
    
    await page.fill('#pd-textbox-122', 'ryan.halley@panoramicdata.com');
    await page.getByRole('button', { name: ' Search' }).click();
    
    await expect(page.getByText('1 - 1 of 1')).toBeVisible();
    await expect(page.getByText('Ryan Halley')).toBeVisible();
  });

  test('Complex Search Scenarios Across Organizations', async ({ page }) => {
    // Test complex combinations of organization filtering and field-specific search
    
    await page.locator('.far').first().click();
    
    // 1. Search with default fields (should find across all orgs)
    await page.fill('#pd-textbox-122', 'panoramicdata.com');
    await page.getByRole('button', { name: ' Search' }).click();
    
    // Should find multiple administrators with panoramicdata.com email
    await expect(page.getByText('Showing')).toBeVisible();
    
    // 2. Filter by CAE Labs organization
    await page.getByText('.CAE Labs Assure Sandbox - Co-op').click();
    
    // Should find fewer results (only CAE Labs users with panoramicdata.com emails)
    const caeResults = await page.getByText(/1 - \d+ of \d+/).textContent();
    
    // 3. Switch to Panoramic Data organization
    await page.getByText('Panoramic Data').click();
    
    // Should find different set of results
    const panoramicResults = await page.getByText(/1 - \d+ of \d+/).textContent();
    
    // Results should be different between organizations
    expect(caeResults).not.toEqual(panoramicResults);
    
    // 4. Configure search to only look in specific field
    await page.getByRole('button', { name: ' Search Fields ' }).click();
    await page.getByRole('button', { name: ' None' }).click();
    await page.getByRole('checkbox', { name: 'Email' }).click();
    await page.getByRole('button', { name: ' Search Fields ' }).click();
    
    // Results should update to show only email matches
    await expect(page.getByText('Showing')).toBeVisible();
  });

  test('URL State Management with Organization and Search', async ({ page }) => {
    // Test that URL correctly reflects both organization context and search parameters
    
    await page.locator('.far').first().click();
    
    // 1. Search without organization filter
    await page.fill('#pd-textbox-122', 'test search');
    await page.getByRole('button', { name: ' Search' }).click();
    
    // Verify URL contains search parameter
    expect(page.url()).toContain('sText=test%20search');
    expect(page.url()).toContain('fPath=%2F'); // Root path
    
    // 2. Add organization filter
    await page.getByText('Panoramic Data').click();
    
    // Verify URL contains both search and organization
    expect(page.url()).toContain('sText=test%20search');
    expect(page.url()).toContain('fPath=%2FPanoramic%20Data');
    
    // 3. Change search term while maintaining organization context
    await page.fill('#pd-textbox-122', 'different search');
    await page.getByRole('button', { name: ' Search' }).click();
    
    expect(page.url()).toContain('sText=different%20search');
    expect(page.url()).toContain('fPath=%2FPanoramic%20Data');
    
    // 4. Clear search but maintain organization
    await page.getByRole('button').nth(1).click(); // Clear button
    
    expect(page.url()).toContain('fPath=%2FPanoramic%20Data');
    expect(page.url()).toContain('sText=');
  });

  test('Performance with Large Dataset Filtering', async ({ page }) => {
    // Test that filtering and search work efficiently with the full dataset
    
    await page.locator('.far').first().click();
    
    // 1. Load all data (no search, no org filter)
    await expect(page.getByText('1 - 84 of 84')).toBeVisible();
    
    // 2. Apply organization filter to reduce dataset
    const startTime = Date.now();
    await page.getByText('Panoramic Data').click();
    
    // Should quickly filter to smaller subset
    await expect(page.getByText(/1 - \d+ of \d+/)).toBeVisible();
    const filterTime = Date.now() - startTime;
    
    // Should complete filtering within reasonable time (< 3 seconds)
    expect(filterTime).toBeLessThan(3000);
    
    // 3. Apply search on filtered dataset
    const searchStartTime = Date.now();
    await page.fill('#pd-textbox-122', 'John');
    await page.getByRole('button', { name: ' Search' }).click();
    
    await expect(page.getByText('John Odlin')).toBeVisible();
    const searchTime = Date.now() - searchStartTime;
    
    // Search should also be fast
    expect(searchTime).toBeLessThan(2000);
    
    // 4. Verify data integrity after multiple operations
    const row = page.locator('tbody tr').first();
    await expect(row.getByText('John Odlin')).toBeVisible();
    await expect(row.getByText('john.odlin@panoramicdata.com')).toBeVisible();
  });
});