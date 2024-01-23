import { expect, test } from '@playwright/test';

test('index page has expected h1', async ({ page }) => {
	await page.goto('/');
	await expect(page.getByRole('heading', { name: 'ZIP File upload' })).toBeVisible();
});

test('index page has upload button', async ({ page }) => {
	await page.goto('/');
	await expect(page.getByRole('button')).toHaveText('Upload');
});

test('index page has file input', async ({ page }) => {
	await page.goto('/');
	await expect(page.getByLabel('File')).toBeVisible();
});

test('upload file with correct size', async ({ page }) => {
	const FILE_NAME = "fileWithCorrectSize.zip";

	await page.goto('/');
	await page.getByLabel('ZIP File:').setInputFiles('./tests/resources/' + FILE_NAME);
	await page.getByRole('button').click();

	await expect(page.getByRole('heading').first()).toHaveText("Differences");
});