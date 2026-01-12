// 測試案例 3.3: 跨頁面 Token 使用測試

const { test, expect } = require('@playwright/test');

test.describe('跨頁面 Token 使用測試', () => {
  const API_BASE = process.env.API_BASE || 'http://localhost:5073';

  test('應該在相同 User-Agent 下可以使用 Token', async ({ browser }) => {
    // 建立第一個頁面並取得 Token
    const context1 = await browser.newContext();
    const page1 = await context1.newPage();
    
    await page1.goto(`${API_BASE}/test.html`);
    await page1.fill('#maxUsage', '5');
    await page1.click('button:has-text("取得 Token")');
    await page1.waitForSelector('#tokenDisplay', { state: 'visible' });
    
    const token = await page1.locator('#currentToken').textContent();
    console.log(`✅ 頁面 A 取得 Token: ${token}`);

    // 在頁面 A 使用一次
    await page1.click('button:has-text("呼叫 Protected API")');
    await page1.waitForSelector('#apiResult.success');
    console.log('✅ 頁面 A 第一次使用成功');

    // 建立第二個頁面（相同 User-Agent）
    const page2 = await context1.newPage();
    await page2.goto(`${API_BASE}/test.html`);

    // 將 Token 注入到頁面 B
    await page2.evaluate((tokenValue) => {
      window.currentToken = tokenValue;
      document.getElementById('currentToken').textContent = tokenValue;
      document.getElementById('tokenDisplay').style.display = 'block';
      document.getElementById('callApiBtn').disabled = false;
    }, token);

    console.log('✅ Token 已注入到頁面 B');

    // 在頁面 B 使用 Token
    await page2.click('button:has-text("呼叫 Protected API")');
    
    // 等待響應 (成功或失敗)
    await page2.waitForSelector('#apiResult', { timeout: 5000 });
    
    // 檢查結果
    const resultElement = page2.locator('#apiResult');
    const hasSuccess = await resultElement.evaluate(el => el.classList.contains('success'));
    const resultText = await resultElement.textContent();
    
    console.log(`頁面 B 結果: ${resultText}`);
    
    // 由於 User-Agent 相同，應該成功
    expect(hasSuccess).toBe(true);
    console.log('✅ 頁面 B 使用相同 Token 成功（相同 User-Agent）');

    await context1.close();
  });

  test('應該在不同 User-Agent 下拒絕 Token', async ({ browser }) => {
    // 建立第一個頁面並取得 Token
    const context1 = await browser.newContext({
      userAgent: 'Mozilla/5.0 (TestBrowser-A) Playwright'
    });
    const page1 = await context1.newPage();
    
    await page1.goto(`${API_BASE}/test.html`);
    await page1.fill('#maxUsage', '5');
    await page1.click('button:has-text("取得 Token")');
    await page1.waitForSelector('#tokenDisplay', { state: 'visible' });
    
    const token = await page1.locator('#currentToken').textContent();
    console.log(`✅ User-Agent A 取得 Token: ${token}`);

    // 建立第二個頁面（不同 User-Agent）
    const context2 = await browser.newContext({
      userAgent: 'Mozilla/5.0 (TestBrowser-B) Playwright'
    });
    const page2 = await context2.newPage();
    
    await page2.goto(`${API_BASE}/test.html`);

    // 將 Token 注入到頁面 B
    await page2.evaluate((tokenValue) => {
      window.currentToken = tokenValue;
      document.getElementById('currentToken').textContent = tokenValue;
      document.getElementById('tokenDisplay').style.display = 'block';
      document.getElementById('callApiBtn').disabled = false;
    }, token);

    console.log('✅ Token 已注入到不同 User-Agent 的頁面 B');

    // 嘗試在頁面 B 使用 Token
    await page2.click('button:has-text("呼叫 Protected API")');
    
    // 應該失敗（User-Agent 不符）
    await page2.waitForSelector('#apiResult.error', { timeout: 5000 });
    console.log('✅ 不同 User-Agent 的頁面 B 被正確拒絕');

    await context1.close();
    await context2.close();
  });

  test('應該驗證安全性測試按鈕功能', async ({ page }) => {
    await page.goto(`${API_BASE}/test.html`);

    // 測試無效 Token
    await page.click('button:has-text("測試無效 Token")');
    await page.waitForSelector('#testResult', { state: 'visible' });
    
    let result = await page.locator('#testResult').textContent();
    // 接受 401 (Unauthorized) 或 403 (Forbidden)
    expect(result).toMatch(/40[13]/);
    console.log('✅ 測試無效 Token 功能正常');

    // 測試缺少 Token
    await page.click('button:has-text("測試缺少 Token")');
    await page.waitForSelector('#testResult', { state: 'visible' });
    
    result = await page.locator('#testResult').textContent();
    // 接受 401 (Unauthorized) 或 403 (Forbidden)
    expect(result).toMatch(/40[13]/);
    console.log('✅ 測試缺少 Token 功能正常');
  });
});
