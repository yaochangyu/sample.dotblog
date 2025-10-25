# Tasks: 實作虛擬滾動功能

## Task Breakdown

### 1. 實作 VirtualScroller 核心類別
**優先級**: 高
**預估時間**: 2 小時
**依賴項**: 無

- [ ] 在 `ComprehensiveFileStatusReport.html` 中新增 `VirtualScroller` 類別定義
- [ ] 實作建構函式，接受 `items`、`renderItemFn`、`containerId` 參數
- [ ] 實作 `init()` 方法，建立虛擬滾動 DOM 結構
  - [ ] 建立 `.virtual-scroll-wrapper` 容器（設定 `max-height: 600px`）
  - [ ] 建立 `.spacer-top` 佔位元素
  - [ ] 建立 `.visible-items` 可見項目容器
  - [ ] 建立 `.spacer-bottom` 佔位元素
- [ ] 實作 `updateVisibleRange()` 方法，計算當前可見範圍
  - [ ] 根據 `scrollTop` 計算 `startIndex`
  - [ ] 加入上方 5 筆緩衝區
  - [ ] 計算 `endIndex`，加入下方 5 筆緩衝區
  - [ ] 邊界檢查，確保索引不越界
- [ ] 實作 `render()` 方法，渲染可見項目
  - [ ] 清空 `.visible-items` 容器
  - [ ] 遍歷 `startIndex` 到 `endIndex`，呼叫 `renderItemFn` 生成 DOM
  - [ ] 更新 `.spacer-top` 高度為 `startIndex * estimatedItemHeight`
  - [ ] 更新 `.spacer-bottom` 高度為 `(totalItems - endIndex) * estimatedItemHeight`
- [ ] 實作 `onScroll()` 方法，處理滾動事件
  - [ ] 加入 100ms 節流機制
  - [ ] 呼叫 `updateVisibleRange()` 和 `render()`
- [ ] 實作 `scrollToTop()` 方法，重置滾動位置

**驗證標準**:
- ✅ `VirtualScroller` 類別可成功初始化
- ✅ 初次渲染僅顯示 20 筆資料
- ✅ 滾動時正確更新可見項目
- ✅ 佔位元素高度正確維持滾動條總高度

---

### 2. 修改報表渲染邏輯，整合虛擬滾動器
**優先級**: 高
**預估時間**: 1.5 小時
**依賴項**: Task 1

- [ ] 修改 `renderGroupList()` 函式
  - [ ] 移除原有的 DOM 渲染邏輯
  - [ ] 改為初始化 `VirtualScroller` 實例
  - [ ] 傳入 `groups` 資料、`renderGroupItem` 函式、容器 ID
- [ ] 修改 `renderFileList()` 函式
  - [ ] 移除原有的 DOM 渲染邏輯
  - [ ] 改為初始化 `VirtualScroller` 實例
  - [ ] 傳入 `files` 資料、`renderFileItem` 函式、容器 ID
- [ ] 修改 `renderGroupItem()` 函式
  - [ ] 確保接受第二個參數 `index`
  - [ ] 使用 `index` 生成唯一的群組 ID（`idPrefix + '-' + index`）
  - [ ] 維持現有的展開/收合邏輯
- [ ] 修改 `renderFileItem()` 函式
  - [ ] 確保接受第二個參數 `index`（即使不使用，也需接受以保持介面一致）
  - [ ] 維持現有的渲染邏輯

**驗證標準**:
- ✅ 報表載入時正確初始化 6 個虛擬滾動器
- ✅ 每個標籤頁僅渲染前 20 筆資料
- ✅ 現有的檔案項目樣式維持不變

---

### 3. 建立全域虛擬滾動器管理物件
**優先級**: 中
**預估時間**: 30 分鐘
**依賴項**: Task 1, Task 2

- [ ] 在 `renderReport()` 函式中建立 `virtualScrollers` 物件
  - [ ] 為 `DuplicateGroups` 建立虛擬滾動器（key: `'groups'`）
  - [ ] 為 `DuplicateGroupsByCount` 建立虛擬滾動器（key: `'groupsByCount'`）
  - [ ] 為 `UnmarkedFiles` 建立虛擬滾動器（key: `'unmarked'`）
  - [ ] 為 `MarkedForDeletion` 建立虛擬滾動器（key: `'deletion'`）
  - [ ] 為 `MarkedForMove` 建立虛擬滾動器（key: `'move'`）
  - [ ] 為 `SkippedFiles` 建立虛擬滾動器（key: `'skipped'`）
- [ ] 將 `virtualScrollers` 儲存為全域變數，供標籤頁切換使用

**驗證標準**:
- ✅ `virtualScrollers` 物件包含 6 個虛擬滾動器實例
- ✅ 每個虛擬滾動器正確綁定對應的資料集

---

### 4. 修改標籤頁切換邏輯，支援滾動位置重置
**優先級**: 中
**預估時間**: 30 分鐘
**依賴項**: Task 3

- [ ] 修改 `switchTab()` 函式
  - [ ] 在切換標籤頁時，取得對應的虛擬滾動器實例
  - [ ] 呼叫 `scrollToTop()` 重置滾動位置
  - [ ] 呼叫 `render()` 重新渲染前 20 筆資料
- [ ] 確保原有的標籤頁切換邏輯（移除/添加 `active` 類別）維持不變

**驗證標準**:
- ✅ 切換標籤頁時，滾動位置重置為頂部
- ✅ 新標籤頁渲染前 20 筆資料
- ✅ 標籤頁切換動畫流暢

---

### 5. 處理空資料狀態
**優先級**: 中
**預估時間**: 15 分鐘
**依賴項**: Task 1

- [ ] 在 `VirtualScroller.init()` 中檢查 `items.length`
- [ ] 如果資料為空（`items.length === 0`），顯示空狀態訊息
  - [ ] 設定容器 `innerHTML` 為 `<div class="empty-state">目前沒有檔案</div>`
  - [ ] 不初始化虛擬滾動 DOM 結構
- [ ] 確保空狀態使用現有的 `.empty-state` CSS 樣式

**驗證標準**:
- ✅ 當標籤頁無資料時，顯示「目前沒有檔案」訊息
- ✅ 不顯示滾動條或佔位元素

---

### 6. 效能優化與測試
**優先級**: 中
**預估時間**: 1 小時
**依賴項**: Task 1-5

- [ ] 測試大資料集效能（10,000 筆資料）
  - [ ] 測試初次載入時間（目標 < 1 秒）
  - [ ] 測試滾動流暢度（目標 60 FPS）
  - [ ] 使用瀏覽器開發者工具監控 DOM 元素數量
- [ ] 優化 `estimatedItemHeight` 參數
  - [ ] 測量實際項目高度
  - [ ] 調整預估值以減少滾動偏移
- [ ] 測試快速滾動情境
  - [ ] 確認緩衝區機制正常運作
  - [ ] 確認不出現空白區域
- [ ] 調整節流延遲參數（如需要）
  - [ ] 測試 50ms、100ms、150ms 的差異
  - [ ] 選擇最佳平衡點

**驗證標準**:
- ✅ 10,000 筆資料初次載入 < 1 秒
- ✅ 滾動時無明顯卡頓
- ✅ DOM 元素數量維持在 30 個以內

---

### 7. 瀏覽器相容性測試
**優先級**: 低
**預估時間**: 30 分鐘
**依賴項**: Task 6

- [ ] 在 Chrome 瀏覽器測試
  - [ ] 測試版本：Chrome 90+
  - [ ] 確認無 JavaScript 錯誤
  - [ ] 確認滾動流暢
- [ ] 在 Firefox 瀏覽器測試
  - [ ] 測試版本：Firefox 88+
  - [ ] 確認無 JavaScript 錯誤
  - [ ] 確認滾動流暢
- [ ] 在 Edge 瀏覽器測試
  - [ ] 測試版本：Edge 90+
  - [ ] 確認無 JavaScript 錯誤
  - [ ] 確認滾動流暢

**驗證標準**:
- ✅ 在 Chrome、Firefox、Edge 均正常運作
- ✅ 無控制台錯誤或警告

---

### 8. 整合測試與文件更新
**優先級**: 低
**預估時間**: 30 分鐘
**依賴項**: Task 7

- [ ] 執行端對端測試
  - [ ] 測試所有 6 個標籤頁的虛擬滾動
  - [ ] 測試展開/收合重複檔案群組功能
  - [ ] 測試標籤頁切換
  - [ ] 測試空資料狀態
- [ ] 更新 `CLAUDE.md` 文件（如需要）
  - [ ] 記錄虛擬滾動功能的實作細節
  - [ ] 更新報表生成相關的說明
- [ ] 建立測試報表資料（如需要）
  - [ ] 產生包含 10,000+ 筆資料的 JSON 檔案
  - [ ] 用於效能測試

**驗證標準**:
- ✅ 所有功能正常運作
- ✅ 文件已更新
- ✅ 測試資料已準備

---

## Summary

**總計任務**: 8 個
**預估總時間**: 6.5 小時

### 關鍵里程碑
1. ✅ Task 1-2 完成後，虛擬滾動核心功能可運作
2. ✅ Task 3-5 完成後，所有標籤頁整合完成
3. ✅ Task 6-7 完成後，效能與相容性驗證通過
4. ✅ Task 8 完成後，功能正式上線

### 並行化建議
- Task 1 可獨立完成
- Task 2-3 依賴 Task 1，可同步開發
- Task 4-5 依賴 Task 2-3，可同步開發
- Task 6-8 為驗證與測試階段，需依序執行
