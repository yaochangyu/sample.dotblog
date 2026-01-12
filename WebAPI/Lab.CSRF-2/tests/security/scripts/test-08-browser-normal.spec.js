// 測試案例 3.1: 瀏覽器正常流程

const { test, expect } = require('@playwright/test');

test.describe('瀏覽器正常流程測試', () => {
  const API_BASE = process.env.API_BASE || 'http://localhost:5073';

  test('應該能夠正常取得 Token 並呼叫 Protected API', async ({ page }) => {
    // 步驟 1: 開啟測試頁面
    await page.goto(`${API_BASE}/test.html`);
    await expect(page.locator('h1')).toContainText('WebAPI Token 防濫用機制測試');

    // 步驟 2: 設定參數並取得 Token
    await page.fill('#maxUsage', '3');
    await page.fill('#expiration', '5');
    await page.click('button:has-text("取得 Token")');

    // 步驟 3: 等待 Token 顯示
    await page.waitForSelector('#tokenDisplay', { state: 'visible', timeout: 5000 });
    
    const tokenText = await page.locator('#currentToken').textContent();
    expect(tokenText).toBeTruthy();
    expect(tokenText).toMatch(/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i);

    console.log(`✅ Token 取得成功: ${tokenText}`);

    // 步驟 4: 呼叫 Protected API
    await page.fill('#requestData', '測試資料');
    await page.click('button:has-text("呼叫 Protected API")');

    // 步驟 5: 驗證成功訊息
    await page.waitForSelector('#apiResult.success', { timeout: 5000 });
    
    const resultText = await page.locator('#apiResult').textContent();
    expect(resultText).toContain('message');
    expect(resultText).toContain('Request processed successfully');

    console.log('✅ Protected API 呼叫成功');
  });

  test('應該能夠處理多次呼叫', async ({ page }) => {
    await page.goto(`${API_BASE}/test.html`);

    // 取得 Token (允許 3 次使用)
    await page.fill('#maxUsage', '3');
    await page.click('button:has-text("取得 Token")');
    await page.waitForSelector('#tokenDisplay', { state: 'visible' });

    // 第一次呼叫
    await page.click('button:has-text("呼叫 Protected API")');
    await page.waitForSelector('#apiResult.success');
    console.log('✅ 第一次呼叫成功');

    // 第二次呼叫
    await page.click('button:has-text("呼叫 Protected API")');
    await page.waitForSelector('#apiResult.success');
    console.log('✅ 第二次呼叫成功');

    // 第三次呼叫
    await page.click('button:has-text("呼叫 Protected API")');
    await page.waitForSelector('#apiResult.success');
    console.log('✅ 第三次呼叫成功');
  });
});
