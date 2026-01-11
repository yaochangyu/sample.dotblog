# Frontend Playwright 自動化測試計畫

## 測試目標

使用 Playwright 自動化測試前端頁面與 `api/protected` 端點的整合，取代手動測試，確保測試可重複執行且結果可靠。

---

## 測試範圍

### 包含項目
- ✅ 瀏覽器環境下的 Token 取得與使用
- ✅ CORS 跨域請求驗證
- ✅ Referer/Origin Header 自動帶入
- ✅ 正常用戶互動流程
- ✅ JavaScript 執行環境驗證
- ✅ 瀏覽器安全機制測試（SameSite Cookie 等）

### 不包含項目
- ❌ 純 HTTP 請求測試（已由 cURL 測試覆蓋）
- ❌ 後端 API 單元測試
- ❌ 效能壓力測試

---

## 測試環境設定

### 前置需求

- [ ] **安裝 Node.js**: 18.x 或以上
- [ ] **安裝 Playwright**:
  ```bash
  npm init playwright@latest
  # 或加入現有專案
  npm install -D @playwright/test
  npx playwright install
  ```
- [ ] **測試伺服器**: ASP.NET Core API 執行於 `https://localhost:5001`
- [ ] **測試前端頁面**: 需建立測試用 HTML 頁面

### 專案結構

```
Lab.CSRF-2/
├── tests/
│   ├── playwright/
│   │   ├── api-protected.spec.ts        # API 安全測試
│   │   ├── csrf-protection.spec.ts      # CSRF 防護測試
│   │   ├── token-validation.spec.ts     # Token 驗證測試
│   │   └── cross-origin.spec.ts         # 跨域測試
│   └── fixtures/
│       ├── test-page.html               # 測試用主頁面
│       └── attacker-page.html           # 模擬攻擊者頁面
├── playwright.config.ts                 # Playwright 設定
└── package.json
```

---

## 測試案例規劃

### 一、Token 取得與使用測試

#### TC-PW-TOKEN-01: 正常取得並使用 Token
- **測試步驟**:
  1. 使用 Playwright 開啟測試頁面
  2. 呼叫 `GET /api/token` 取得 Token
  3. 使用 Token 呼叫 `POST /api/protected`
  4. 驗證回應為 `200 OK`
- **驗證重點**: 
  - Token 正確存在於 Response Header
  - API 呼叫成功
  - Referer/Origin 自動帶入

#### TC-PW-TOKEN-02: Token 過期後重新取得
- **測試步驟**:
  1. 取得 Token
  2. 等待 Token 過期（模擬時間或實際等待）
  3. 使用過期 Token 呼叫 API
  4. 驗證回應為 `401 Unauthorized`
  5. 重新取得新 Token
  6. 使用新 Token 成功呼叫 API
- **驗證重點**: Token 時效性管控

#### TC-PW-TOKEN-03: Token 使用次數限制
- **測試步驟**:
  1. 取得 Token
  2. 迴圈呼叫 API（使用相同 Token）
  3. 記錄成功次數與失敗時的回應
- **驗證重點**: 
  - 前 N 次成功
  - 第 N+1 次回傳 `401 Unauthorized`

---

### 二、CSRF 防護測試

#### TC-PW-CSRF-01: 同源請求成功
- **測試步驟**:
  1. 從 `https://localhost:5001/test-page.html` 發起請求
  2. JavaScript fetch 呼叫 `POST /api/protected`
  3. 驗證 Referer/Origin 為 `https://localhost:5001`
  4. 驗證回應 `200 OK`
- **驗證重點**: 同源請求允許通過

#### TC-PW-CSRF-02: 跨域請求被阻擋
- **測試步驟**:
  1. 啟動另一個測試伺服器於 `http://localhost:3000`
  2. 從 `http://localhost:3000/attacker-page.html` 發起請求
  3. 嘗試呼叫 `https://localhost:5001/api/protected`
  4. 驗證瀏覽器 CORS 錯誤或 API 回傳 `403 Forbidden`
- **驗證重點**: CORS 政策阻擋跨域請求

#### TC-PW-CSRF-03: 偽造表單提交（模擬 CSRF 攻擊）
- **測試步驟**:
  1. 建立惡意頁面 `attacker-page.html`
  2. 頁面包含自動提交表單，目標為 `/api/protected`
  3. 使用 Playwright 載入惡意頁面
  4. 驗證請求被拒絕
- **驗證重點**: 
  - 缺少 Token → `401 Unauthorized`
  - 或 Origin 不符 → `403 Forbidden`

---

### 三、Header 驗證測試

#### TC-PW-HEADER-01: Referer Header 自動帶入
- **測試步驟**:
  1. 從測試頁面發起 API 請求
  2. 使用 Playwright 攔截網路請求
  3. 驗證請求 Header 包含 `Referer: https://localhost:5001/`
- **驗證重點**: 瀏覽器自動帶入 Referer

#### TC-PW-HEADER-02: Origin Header 自動帶入
- **測試步驟**:
  1. 從測試頁面發起跨域 API 請求（Preflight）
  2. 攔截請求並驗證 `Origin` Header
- **驗證重點**: 瀏覽器自動帶入 Origin

#### TC-PW-HEADER-03: User-Agent 正常瀏覽器值
- **測試步驟**:
  1. 攔截 API 請求
  2. 驗證 User-Agent 為瀏覽器值（非 cURL）
- **驗證重點**: User-Agent 檢查機制能區分瀏覽器與腳本

---

### 四、多瀏覽器相容性測試

#### TC-PW-BROWSER-01: Chromium 瀏覽器測試
- **執行環境**: Chromium (Chrome/Edge)
- **測試範圍**: 執行所有測試案例

#### TC-PW-BROWSER-02: Firefox 瀏覽器測試
- **執行環境**: Firefox
- **測試範圍**: 執行所有測試案例

#### TC-PW-BROWSER-03: WebKit 瀏覽器測試
- **執行環境**: WebKit (Safari)
- **測試範圍**: 執行所有測試案例

**驗證重點**: 
- 所有主流瀏覽器行為一致
- CORS、SameSite Cookie 等機制正常運作

---

### 五、JavaScript 環境驗證

#### TC-PW-JS-01: JavaScript 必須啟用
- **測試步驟**:
  1. 使用 Playwright 停用 JavaScript
  2. 嘗試存取測試頁面
  3. 驗證無法取得 Token 或呼叫 API
- **驗證重點**: 非 JavaScript 環境無法正常使用（防止簡單爬蟲）

#### TC-PW-JS-02: JavaScript 挑戰機制（可選）
- **測試步驟**:
  1. 測試頁面包含簡單計算挑戰（例如: `2 + 2 = ?`）
  2. JavaScript 自動解答並取得 Token
  3. 純 HTTP 請求無法取得正確 Token
- **驗證重點**: JavaScript 挑戰增加爬蟲難度

---

### 六、Cookie 與 Session 測試

#### TC-PW-COOKIE-01: SameSite Cookie 設定
- **測試步驟**:
  1. 檢查 API 回應的 Set-Cookie Header
  2. 驗證包含 `SameSite=Strict` 或 `SameSite=Lax`
  3. 嘗試跨域請求時驗證 Cookie 未被帶入
- **驗證重點**: SameSite 屬性防止 CSRF

#### TC-PW-COOKIE-02: Secure 與 HttpOnly 屬性
- **測試步驟**:
  1. 檢查 Cookie 包含 `Secure; HttpOnly`
  2. 使用 JavaScript 嘗試存取 Cookie
  3. 驗證無法存取（HttpOnly）
- **驗證重點**: Cookie 安全屬性設定正確

---

### 七、使用者互動流程測試

#### TC-PW-FLOW-01: 完整表單提交流程
- **測試步驟**:
  1. 開啟測試頁面
  2. 填寫表單資料
  3. 點擊提交按鈕
  4. 自動取得 Token
  5. 發送 POST 請求到 `/api/protected`
  6. 顯示成功訊息
- **驗證重點**: 正常用戶流程順暢

#### TC-PW-FLOW-02: Token 失效後自動重試
- **測試步驟**:
  1. 提交表單
  2. 首次請求因 Token 過期失敗
  3. 自動重新取得 Token
  4. 重試請求成功
- **驗證重點**: 自動錯誤恢復機制

---

## Playwright 設定範例

### playwright.config.ts

```typescript
import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './tests/playwright',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: [
    ['html'],
    ['json', { outputFile: 'test-results/results.json' }],
    ['junit', { outputFile: 'test-results/results.xml' }]
  ],
  use: {
    baseURL: 'https://localhost:5001',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    ignoreHTTPSErrors: true, // 本機測試用
  },

  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
    {
      name: 'firefox',
      use: { ...devices['Desktop Firefox'] },
    },
    {
      name: 'webkit',
      use: { ...devices['Desktop Safari'] },
    },
  ],

  webServer: {
    command: 'dotnet run --project Lab.CSRF2.WebAPI',
    url: 'https://localhost:5001',
    reuseExistingServer: !process.env.CI,
    ignoreHTTPSErrors: true,
  },
});
```

---

## 測試程式範例

### tests/playwright/api-protected.spec.ts

```typescript
import { test, expect } from '@playwright/test';

test.describe('API Protected Endpoint Tests', () => {
  
  test('TC-PW-TOKEN-01: 正常取得並使用 Token', async ({ page }) => {
    // 1. 開啟測試頁面
    await page.goto('/test-page.html');
    
    // 2. 取得 Token（攔截網路請求）
    const tokenResponse = await page.request.get('/api/token');
    const token = tokenResponse.headers()['x-csrf-token'];
    expect(token).toBeTruthy();
    
    // 3. 使用 Token 呼叫 API
    const apiResponse = await page.request.post('/api/protected', {
      headers: {
        'X-CSRF-Token': token,
        'Content-Type': 'application/json',
      },
      data: { data: 'test' },
    });
    
    // 4. 驗證成功
    expect(apiResponse.status()).toBe(200);
  });

  test('TC-PW-TOKEN-03: Token 使用次數限制', async ({ page }) => {
    await page.goto('/test-page.html');
    
    const tokenResponse = await page.request.get('/api/token');
    const token = tokenResponse.headers()['x-csrf-token'];
    
    let successCount = 0;
    let firstFailureStatus = null;
    
    // 嘗試使用 Token 多次
    for (let i = 0; i < 10; i++) {
      const response = await page.request.post('/api/protected', {
        headers: { 'X-CSRF-Token': token },
        data: { data: `request-${i}` },
        failOnStatusCode: false,
      });
      
      if (response.status() === 200) {
        successCount++;
      } else if (!firstFailureStatus) {
        firstFailureStatus = response.status();
      }
    }
    
    // 驗證有次數限制
    expect(successCount).toBeLessThan(10);
    expect(firstFailureStatus).toBe(401);
  });
  
});
```

### tests/playwright/csrf-protection.spec.ts

```typescript
import { test, expect } from '@playwright/test';

test.describe('CSRF Protection Tests', () => {
  
  test('TC-PW-CSRF-01: 同源請求成功', async ({ page }) => {
    await page.goto('/test-page.html');
    
    // 攔截請求以驗證 Headers
    let requestHeaders: any = null;
    page.on('request', request => {
      if (request.url().includes('/api/protected')) {
        requestHeaders = request.headers();
      }
    });
    
    // 從頁面發起請求
    await page.evaluate(async () => {
      const tokenRes = await fetch('/api/token');
      const token = tokenRes.headers.get('X-CSRF-Token');
      
      const apiRes = await fetch('/api/protected', {
        method: 'POST',
        headers: {
          'X-CSRF-Token': token!,
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ data: 'test' }),
      });
      
      return apiRes.status;
    });
    
    // 驗證 Referer/Origin
    expect(requestHeaders?.referer).toContain('localhost:5001');
    expect(requestHeaders?.origin).toContain('localhost:5001');
  });

  test('TC-PW-CSRF-02: 跨域請求被阻擋', async ({ page, context }) => {
    // 建立攻擊者頁面（不同 origin）
    const attackerPage = await context.newPage();
    await attackerPage.goto('http://localhost:3000/attacker-page.html');
    
    // 嘗試跨域請求
    const result = await attackerPage.evaluate(async () => {
      try {
        const response = await fetch('https://localhost:5001/api/protected', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ data: 'attack' }),
        });
        return { success: true, status: response.status };
      } catch (error) {
        return { success: false, error: error.message };
      }
    });
    
    // 驗證被阻擋
    expect(result.success).toBe(false);
    expect(result.error).toContain('CORS');
  });

});
```

---

## 測試執行指令

### 執行所有測試
```bash
npx playwright test
```

### 執行特定測試檔案
```bash
npx playwright test api-protected.spec.ts
```

### 執行特定瀏覽器
```bash
npx playwright test --project=chromium
npx playwright test --project=firefox
npx playwright test --project=webkit
```

### 顯示測試報告
```bash
npx playwright show-report
```

### Debug 模式
```bash
npx playwright test --debug
```

### UI 模式（互動式）
```bash
npx playwright test --ui
```

---

## 測試報告格式

Playwright 自動產生：

1. **HTML 報告**: 
   - 位置: `playwright-report/index.html`
   - 包含詳細測試結果、截圖、影片

2. **JSON 報告**: 
   - 位置: `test-results/results.json`
   - 可整合至 CI/CD 系統

3. **JUnit 報告**: 
   - 位置: `test-results/results.xml`
   - 可整合至 Azure DevOps、Jenkins 等

---

## CI/CD 整合

### GitHub Actions 範例

```yaml
name: Playwright Tests
on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-node@v3
        with:
          node-version: 18
      - name: Install dependencies
        run: npm ci
      - name: Install Playwright Browsers
        run: npx playwright install --with-deps
      - name: Run Playwright tests
        run: npx playwright test
      - uses: actions/upload-artifact@v3
        if: always()
        with:
          name: playwright-report
          path: playwright-report/
          retention-days: 30
```

---

## 測試檢查清單

### Token 測試: 3 項
- [ ] TC-PW-TOKEN-01: 正常取得並使用 Token
- [ ] TC-PW-TOKEN-02: Token 過期後重新取得
- [ ] TC-PW-TOKEN-03: Token 使用次數限制

### CSRF 防護測試: 3 項
- [ ] TC-PW-CSRF-01: 同源請求成功
- [ ] TC-PW-CSRF-02: 跨域請求被阻擋
- [ ] TC-PW-CSRF-03: 偽造表單提交

### Header 驗證測試: 3 項
- [ ] TC-PW-HEADER-01: Referer Header 自動帶入
- [ ] TC-PW-HEADER-02: Origin Header 自動帶入
- [ ] TC-PW-HEADER-03: User-Agent 正常瀏覽器值

### 多瀏覽器測試: 3 項
- [ ] TC-PW-BROWSER-01: Chromium
- [ ] TC-PW-BROWSER-02: Firefox
- [ ] TC-PW-BROWSER-03: WebKit

### JavaScript 環境: 2 項
- [ ] TC-PW-JS-01: JavaScript 必須啟用
- [ ] TC-PW-JS-02: JavaScript 挑戰機制

### Cookie 測試: 2 項
- [ ] TC-PW-COOKIE-01: SameSite Cookie
- [ ] TC-PW-COOKIE-02: Secure 與 HttpOnly

### 使用者流程: 2 項
- [ ] TC-PW-FLOW-01: 完整表單提交流程
- [ ] TC-PW-FLOW-02: Token 失效後自動重試

**總計**: 18 項測試案例

---

## 與現有測試的關係

| 測試類型 | 工具 | 測試範圍 | 檔案 |
|---------|------|---------|------|
| API 安全測試 | cURL / PowerShell | HTTP 請求層級 | `api-protected-security-test.ps1` |
| **前端整合測試** | **Playwright** | **瀏覽器環境** | **本計畫** |
| 單元測試 | xUnit / NUnit | 後端邏輯 | (另外規劃) |

**互補關係**:
- cURL 測試: 驗證 API 本身的安全機制
- Playwright 測試: 驗證瀏覽器環境下的完整流程
- 兩者結合才能達到完整測試覆蓋

---

## 下一步行動

- [ ] 安裝 Playwright 與相依套件
- [ ] 建立測試頁面 (`test-page.html`, `attacker-page.html`)
- [ ] 實作測試案例程式碼
- [ ] 執行測試並產生報告
- [ ] 整合至 CI/CD 流程
- [ ] 建立測試文件與維護指南

---

## 參考資源

- [Playwright 官方文件](https://playwright.dev/)
- [Playwright 最佳實踐](https://playwright.dev/docs/best-practices)
- [API Testing with Playwright](https://playwright.dev/docs/api-testing)
- [Network Interception](https://playwright.dev/docs/network)

---

## 版本紀錄

| 版本 | 日期 | 變更內容 | 作者 |
|------|------|---------|------|
| 1.0 | 2026-01-11 | 初版建立 | Testing Team |
