const { defineConfig, devices } = require('@playwright/test');

module.exports = defineConfig({
  testDir: './scripts',
  testMatch: '**/*.spec.js',
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: 1,
  reporter: [
    ['list'],
    ['html', { outputFolder: 'test-results/html' }]
  ],
  
  use: {
    baseURL: process.env.API_BASE || 'http://localhost:5073',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
  },

  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],

  webServer: process.env.CI ? undefined : {
    command: 'cd ../../Lab.CSRF2.WebAPI && dotnet run',
    url: 'http://localhost:5073',
    reuseExistingServer: true,
    timeout: 120000,
  },
});
