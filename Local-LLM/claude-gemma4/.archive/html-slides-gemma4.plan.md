# 實作計劃：地端 Gemma 4 與 Claude Code 優化實踐 HTML 簡報

本計劃旨在將地端 Gemma 4 與 Claude Code 的優化配置、避坑指南與實測結果製作成零建置、可直接在瀏覽器開啟的 HTML 簡報。

## 實作步驟

- [x] **步驟 1：確認簡報需求**
  - **說明**：確認簡報的目的、受眾、預估頁數/時長、預期風格、是否需要講者備註、是否有圖片資產等。
  - **實際設定**：
    - 風格：**Dark** (高對比暗色，背景黑，文字白/淺灰，強調黃/青)
    - 受眾：技術論壇 / Demo 觀眾
    - 頁數：8-10 頁
    - 講者備註：需要
  - **進度**：已於 2026-06-14 完成確認。

- [x] **步驟 2：撰寫草案 Markdown**
  - **說明**：依據 `references/outline-format.md` 規範，撰寫 `gemma4-optimization-outline.md`。每張投影片一個區塊，標明版型、標題、重點與講者備註。
  - **實際產出**：已於 2026-06-14 產出 [gemma4-optimization-outline.md](file:///mnt/d/lab/sample.dotblog/Local-LLM/claude-gemma4/gemma4-optimization-outline.md)，目前等待使用者審查。

- [x] **步驟 3：撰寫 HTML 簡報**
  - **說明**：將確認後的草案轉成單檔 HTML 簡報。使用 `references/style-system.md` 的 CSS Token 與 Lab 風格，並引入鍵盤導覽、進度條等基本功能。
  - **實際產出**：已於 2026-06-14 產出 [gemma4-optimization-slides.html](file:///mnt/d/lab/sample.dotblog/Local-LLM/claude-gemma4/gemma4-optimization-slides.html)，使用 Dark 風格。

- [x] **步驟 4：瀏覽器 QA 與自我驗證**
  - **說明**：用瀏覽器檢查簡報呈現，確保不依賴捲動、一頁一重點、Token 一致。提供「逐頁清單」與「成功標準自評」供使用者核對。
  - **實際產出**：已進行自我驗證，並在回覆中附上逐頁清單與成功標準自評。

- [x] **步驟 5：封存計劃**
  - **說明**：將已完成的計畫檔案 `html-slides-gemma4.plan.md` 移動到 `.archive` 目錄中。
  - **實際產出**：計劃將被移至 `.archive/html-slides-gemma4.plan.md`。
