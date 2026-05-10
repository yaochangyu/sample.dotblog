# Import 1111 Jobs to Qdrant Plan

- [x] **確認資料模型與匯入範圍**
  - 明確定義這次匯入的資料來源是 `1111_jobs_page3_top100.enriched.json`，並採用「**1 筆職缺 = 1 筆向量文件**」的方式建模。
  - 這一步需要先做，因為向量資料庫一旦定義 `id`、`text`、`metadata` 結構，後面的 embedding、upsert 與查詢方式都會跟著固定下來。
  - 預計主鍵使用 `jobId`，保留原始欄位與中文化欄位作為 metadata。

- [x] **用 Docker Compose 建立 Qdrant 開發環境**
  - 新增 `docker-compose.yml`（或等價檔名），用容器啟動 Qdrant，至少包含 API port `6333`、gRPC port `6334` 與持久化 volume。
  - 這一步需要做，因為目前環境沒有 Qdrant；先用 Compose 建立本機開發環境，才能讓後續匯入與查詢流程可重現、可自動化、可驗證。
  - 第一版先以單機 Qdrant 為主，匯入腳本可先在本機執行，連到 `http://localhost:6333`，不強制一開始就把匯入腳本容器化。

- [x] **定義要進行 embedding 的文字內容**
  - 設計每筆職缺要組成哪一段文字，例如：`title`、`companyName`、`description`、`enumMeaning.roleLabels`、`enumMeaning.jobTypeLabels`、`jobPage.work_location_text`、`require.experienceText`、`require.gradesDecoded`。
  - 這一步需要做，因為向量品質很大程度取決於輸入文字是否完整且一致；若只丟 `description`，搜尋結果通常不如有結構整理過的文字。
  - 目標是讓搜尋時能同時命中職稱、技能、地點、工作型態與學經歷需求。

- [x] **定義 Qdrant payload / metadata 結構**
  - 規劃需要保留在 payload 的欄位，例如：`jobId`、`companyName`、`industry.name`、`workCity.name`、`salary`、`jobUrl`、`enumMeaning.roleLabels`、`enumMeaning.jobTypeLabels`、`require.experienceText`。
  - 這一步需要做，因為日後查詢很可能需要搭配過濾條件，例如依城市、產業、工作性質或薪資範圍過濾，而這些都依賴 metadata。
  - 原則是 payload 只保留查詢與顯示需要的欄位，不把整份原始 JSON 原封不動塞進向量庫。

- [x] **決定 embedding 模型與向量維度**
  - 選定用哪個 embedding 模型，並確認輸出的向量維度，作為 Qdrant collection 建立時的 schema 依據。
  - 這一步需要做，因為 Qdrant collection 建立後，向量維度不能隨意變更；若先建錯，後面整批資料都會無法匯入。
  - 同時也要決定距離函式，例如 `Cosine`。

- [x] **設計 collection 與索引設定**
  - 規劃 Qdrant collection 名稱、distance metric、是否需要 payload index，以及後續查詢常用欄位的索引策略。
  - 這一步需要做，因為正確的 collection 設定會直接影響查詢效能與過濾能力，尤其當未來資料量從 100 筆擴大時更明顯。
  - 第一版可先以簡單可用為主，後續再依查詢模式調整索引。

- [x] **實作匯入腳本**
  - 新增一支腳本，讀取 `1111_jobs_page3_top100.enriched.json`，組出 `id + text + metadata`，呼叫 embedding 模型產生向量，再 upsert 到 Qdrant。
  - 這一步需要做，因為你需要可重現、可自動化的匯入流程，而不是一次性的手動操作。
  - 腳本應保留清楚的輸入/輸出參數，方便之後換資料檔或重跑。

- [x] **實作資料轉換測試**
  - 針對「從 enriched JSON 組出 `id + text + metadata`」的邏輯加上測試，確認 `jobId`、`text`、必要 payload 欄位都有正確產出。
  - 這一步需要做，因為若 text 組法錯誤、欄位遺漏或格式不一致，後面的 embedding 與搜尋品質會直接受影響。
  - 測試重點應包含：`jobId` 唯一、`text` 非空、metadata 含 `companyName`、`industry`、`city`、`salary`、`jobUrl` 等必要欄位。

- [x] **實作 Qdrant 匯入驗證**
  - 匯入後驗證 Qdrant collection 中的 point 數量、指定 `jobId` 的 payload 是否存在、向量與 payload schema 是否符合預期。
  - 這一步需要做，因為匯入成功不代表資料真的可用；可能發生 id 重複覆寫、欄位缺漏、collection schema 不一致等問題。
  - 第一版可用整合測試或驗證腳本方式實作。

- [x] **驗證匯入結果**
  - 匯入完成後，檢查 Qdrant 中的 point 數量、抽查單筆 payload，以及確認 `jobId` 是否能正確對應回原始職缺資料。
  - 這一步需要做，因為匯入成功不代表資料結構正確；若 `text` 組錯、payload 少欄位或 id 重複，後續搜尋結果會不可靠。

- [x] **實作搜尋與過濾測試**
  - 建立幾組查詢測試案例，例如「會計 全職 彰化」、「兼職 門市 台中」，並驗證向量搜尋與 metadata filter 都能回傳合理結果。
  - 這一步需要做，因為最終目標是能搜尋出可用結果，而不是只有資料成功寫入。
  - 若搜尋結果不理想，可回頭調整 text 組法、payload 欄位或 collection 設定。

- [x] **驗證搜尋流程**
  - 用幾個實際查詢詞測試，例如「會計 全職 彰化」、「兼職 門市 台中」，確認能找到合理的職缺結果。
  - 這一步需要做，因為向量化流程的目標不是只有「存進去」，而是要能被有效搜尋與過濾。
  - 若結果不理想，再回頭調整 embedding text 組法或 metadata 欄位。

- [x] **更新 tree.md**
  - 若後續新增匯入腳本、設定檔或測試輸出檔，要同步更新 `tree.md`。
  - 這一步需要做，因為專案規則要求新增、刪除、移動檔案時同步維護目錄樹。

## 目前假設

- 目標向量資料庫：`Qdrant`
- 啟動方式：`Docker Compose`
- 匯入來源：`1111_jobs_page3_top100.enriched.json`
- 建模方式：`1 職缺 = 1 point`
- 主鍵：`jobId`
- 第一版目標：先完成可匯入、可查詢、可驗證的最小流程

## 第 1 步確認結果

- 匯入來源固定為：`1111_jobs_page3_top100.enriched.json`
- 匯入粒度固定為：`1 筆職缺 = 1 個 Qdrant point`
- point id 固定使用：`jobId`
- 第一版匯入範圍固定為：目前 100 筆職缺，不先處理分 chunk
- 保留兩層資料：
  - `embedding text`：後續步驟再明確定義組字規則
  - `payload / metadata`：保留查詢與顯示需要的原始欄位與中文化欄位
- 第一版不追求把所有原始欄位完整塞入 payload，而是只保留搜尋、過濾、顯示必需欄位

## 實作結果

- Docker Compose：
  - 使用 `docker-compose.yml` 啟動單機 `Qdrant`
  - 對外開放 `6333` / `6334`
  - 使用 named volume `qdrant_storage`
- Embedding：
  - 模型：`sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2`
  - 維度：`384`
  - 距離函式：`Cosine`
- Collection：
  - 名稱：`jobs_1111_page3_top100`
  - payload index：`job_id`、`company_name`、`industry`、`work_city`、`role_labels`、`job_type_labels`、`salary_min`、`salary_max`
- 匯入腳本：
  - `import_1111_jobs_to_qdrant.py`
  - 支援 `prepare`、`import`、`verify`、`search`
- 測試：
  - `test_import_1111_jobs_to_qdrant.py`
  - 已覆蓋資料轉換與 Qdrant live integration

## 已驗證結果

- 成功匯入 `100` 筆職缺
- `verify` 結果：`expected_count = 100`、`actual_count = 100`
- 搜尋示例：
  - 查詢：`會計 全職 彰化`
  - filter：`work_city = 彰化縣二林鎮`
  - 已回傳合理職缺結果

## 失敗方法紀錄

- 步驟：建立 Docker Compose 的 Qdrant 開發環境
- 失敗方法：使用 image `qdrant/qdrant:v1.14.2`
- 原因：Docker Hub 無此 manifest
- 重試方式：改用 `qdrant/qdrant:latest`
- 結果：成功啟動並可正常連線
