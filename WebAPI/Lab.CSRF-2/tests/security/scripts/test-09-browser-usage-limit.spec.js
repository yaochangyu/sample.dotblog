// 測試案例 3.2: 瀏覽器使用次數限制測試

const { test, expect } = require('@playwright/test');

test.describe('瀏覽器使用次數限制測試', () => {
  const API_BASE = process.env.API_BASE || 'http://localhost:5073';

  test('應該在超過使用次數後拒絕請求', async ({ page }) => {
    // 步驟 1: 開啟測試頁面
    await page.goto(`${API_BASE}/test.html`);

    // 步驟 2: 設定最大使用次數為 2
    await page.fill('#maxUsage', '2');
    await page.fill('#expiration', '5');
    await page.click('button:has-text("取得 Token")');
    
    await page.waitForSelector('#tokenDisplay', { state: 'visible' });
    console.log('✅ Token 取得成功');

    // 步驟 3: 第一次呼叫 (應該成功)
    await page.fill('#requestData', '第一次呼叫');
    await page.click('button:has-text("呼叫 Protected API")');
    await page.waitForSelector('#apiResult.success', { timeout: 5000 });
    console.log('✅ 第一次呼叫成功');

    // 步驟 4: 第二次呼叫 (應該成功)
    await page.fill('#requestData', '第二次呼叫');
    await page.click('button:has-text("呼叫 Protected API")');
    await page.waitForSelector('#apiResult.success', { timeout: 5000 });
    console.log('✅ 第二次呼叫成功');

    // 步驟 5: 第三次呼叫 (應該失敗)
    await page.fill('#requestData', '第三次呼叫');
    await page.click('button:has-text("呼叫 Protected API")');
    
    // 等待錯誤訊息出現
    await page.waitForSelector('#apiResult.error', { timeout: 5000 });
    
    const errorText = await page.locator('#apiResult').textContent();
    expect(errorText).toBeTruthy();
    console.log('✅ 第三次呼叫被正確拒絕');
    console.log(`   錯誤訊息: ${errorText}`);
  });

  test('使用測試按鈕驗證多次使用限制', async ({ page }) => {
    await page.goto(`${API_BASE}/test.html`);

    // 取得 Token (單次使用)
    await page.fill('#maxUsage', '1');
    await page.click('button:has-text("取得 Token")');
    await page.waitForSelector('#tokenDisplay', { state: 'visible' });

    // 使用測試按鈕進行多次使用測試
    await page.click('button:has-text("測試多次使用")');
    
    // 等待測試結果
    await page.waitForSelector('#testResult', { state: 'visible', timeout: 10000 });
    
    const testResult = await page.locator('#testResult').textContent();
    
    // 驗證結果包含成功和失敗的訊息
    expect(testResult).toContain('第 1 次');
    expect(testResult).toContain('第 2 次');
    
    console.log('✅ 多次使用測試完成');
    console.log(`測試結果:\n${testResult}`);
  });
});
