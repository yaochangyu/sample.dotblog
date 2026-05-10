# 1111 Jobs Enrichment Plan

- [x] **盤點欄位與對應來源**
  - 先確認 `1111_jobs_page3_top100.json` 內哪些欄位是列舉數字，例如 `role`、`benefits`、`companyTags`、`internship`、`jobType`、`require.drivingLicense`、`require.grades`、`require.majors`。
  - 這一步需要先做，因為不同欄位可能要用不同方式轉換：有些可直接從職缺頁拿中文，有些需要建立對照表，有些可能只能保留原值。

- [x] **分析職缺頁可補出的中文資訊**
  - 針對 `https://www.1111.com.tw/job/{jobId}` 解析可讀欄位，例如職務類別、工作性質、工作時間、休假制度、學歷要求、工作經驗、產業類別等。
  - 這一步需要做，因為職缺頁本身已經把部分列舉值轉成中文，可直接作為較可靠的補充資料來源。

- [x] **實作 enrich 腳本**
  - 新增一支腳本，讀取現有 JSON、逐筆抓職缺頁、補上中文欄位，並盡量把列舉數字轉成有意義的中文說明。
  - 這一步需要做，因為你希望留下可重跑的工具，而不是只做一次性手動整理。

- [x] **輸出新的 enriched JSON**
  - 產生新的 JSON 檔，保留原始資料，同時新增中文化欄位，避免破壞原始 API 回傳內容。
  - 這一步需要做，因為你已指定採用新增檔案的方式，方便比對原始值與轉換結果。

- [x] **更新 tree.md**
  - 把新增的腳本、計畫檔與 enriched JSON 記錄到 `tree.md`。
  - 這一步需要做，因為專案規則要求新增檔案後同步更新目錄樹。

## 產出檔案

- `enrich_1111_jobs.py`
- `1111_jobs_page3_top100.enriched.json`

## 目前盤點結果

- 可直接從職缺頁補中文的欄位：
  - `role` / `jobType`：職缺頁有「職務類別」、「工作性質」
  - `require.experience`：職缺頁有「工作經驗」
  - `require.grades`：職缺頁有「學歷要求」
  - `industry.id`：職缺頁有「產業類別」
- 可保留數值但補中文說明的欄位：
  - `mrtId` / `mrtTime` / `mrtNear`：頁面通常會顯示捷運資訊，但不一定每筆都有
- 需要建立對照或保留原值的欄位：
  - `benefits`
  - `companyTags`
  - `internship`
  - `require.drivingLicense`
  - `require.majors`
  - `require.certificates`
  - `remind`

## 本批資料觀察

- `role` 與 `jobType` 在這批資料中的值分布一致：`1`、`2`、`4`、`16`
- `require.experience` 常見值：`0`、`3`、`5`、`7`
- `require.grades` 常見值：`2`、`8`、`16`、`32`、`64`
- `benefits`、`companyTags`、`internship` 明顯是站方內部列舉碼，單看 API 無法直接判讀
- `require.majors` 有大量 `0`，也有少量科系代碼，較可能需要額外對照

## 職缺頁分析補充

- 職缺頁可穩定抓到的欄位：
  - `職務類別`
  - `工作性質`
  - `工作時間`
  - `休假制度`
  - `工作地點`
  - `學歷要求`
  - `工作經驗`
  - `外語能力`
  - `工作技能`
  - `歡迎身份`
  - `產業類別`
  - `科系要求`
  - `具備駕照`
  - `需求人數`
  - `電腦專長`

- 已確認可推估的對照：
  - `require.experience`：`0 -> 不拘`，其餘多數可依頁面推成 `n 年以上經驗`
  - `require.grades`：可推為 `高中職 / 專科 / 大學 / 碩士 / 博士`
  - `require.drivingLicense`：屬位元組合，可由職缺頁駕照清單反推常見位元值
  - `require.majors`：可直接補上頁面顯示的科系中文
