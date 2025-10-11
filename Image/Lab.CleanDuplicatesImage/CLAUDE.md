# CLAUDE.md

這個檔案為 Claude Code (claude.ai/code) 在這個程式碼庫中工作時提供指引。

## 專案概述

這是一個 .NET 9 主控台應用程式，用於掃描、管理和刪除重複的媒體檔案（圖片和影片）。它使用 SHA-256 雜湊值來識別重複檔案，並將資料儲存在本機 SQLite 資料庫中。

## 建置與執行

```bash
# 建置專案
dotnet build

# 執行應用程式
dotnet run
```

## 設定檔

專案使用 `appsettings.json` 進行設定:

```json
{
  "AppSettings": {
    "DefaultMoveTargetBasePath": "D:\\Temp"
  }
}
```

**設定說明**:
- `DefaultMoveTargetBasePath`: 移動檔案的目標基礎路徑,檔案會根據修改日期(yyyy-MM)自動建立子資料夾
- 例如: `D:\Temp\2025-03\file.jpg`
- 此設定在標記檔案為「待移動」時使用

## 核心架構

### 資料模型（Data Models）
- **FileRecord.cs**: 表示單一檔案的記錄，包含路徑、時間戳記、檔案存在性、大小、建立/修改時間
- **FileGroup.cs**: 表示一組重複檔案，包含共享的雜湊值和檔案清單

### 資料庫層（Database Layer）
- **DatabaseHelper.cs**: 提供統一的資料庫存取介面
  - `CreateConnection()`: 建立 SQLite 連線
  - `ExecuteNonQuery()`: 執行 INSERT/UPDATE/DELETE
  - `ExecuteQuery<T>()`: 執行 SELECT 並處理結果
  - `ExecuteTransaction()`: 執行帶交易的批次操作

- **資料庫檔案**: `duplicates.db`（SQLite）

  **資料庫初始化**:
  - 使用 `InitializeDatabase()` 方法 (Program.cs:2654) 進行五階段初始化:
    1. 建立四個基本資料表 (CREATE TABLE IF NOT EXISTS)
    2. 動態檢查並新增 DuplicateFiles 欄位 (MarkType, FileLastModifiedTime)
    3. 為 FilesToDelete 新增處理狀態欄位 (IsProcessed, ProcessedAt)
    4. 為 FileToMove 新增處理狀態欄位 (IsProcessed, ProcessedAt)
    5. 建立索引以優化查詢效能 (idx_hash, idx_mark_type)
  - 使用 `PRAGMA table_info()` 檢查欄位存在性,確保向後相容
  - 所有 ALTER TABLE 操作都有 try-catch 保護

  **主要資料表結構**:

  1. **DuplicateFiles** - 儲存所有重複檔案的主表
     - `Id`: 主鍵（自動遞增）
     - `Hash`: SHA-256 雜湊值
     - `FilePath`: 完整檔案路徑
     - `FileName`: 檔案名稱
     - `FileSize`: 檔案大小（位元組）
     - `FileCreatedTime`: 檔案建立時間
     - `FileLastModifiedTime`: 檔案最後修改時間
     - `FileCount`: 該雜湊值的檔案總數
     - `CreatedAt`: 記錄建立時間
     - `MarkType`: 標記類型（**重要欄位**）
       - `0` = 無標記（未處理）
       - `1` = 刪除標記
       - `2` = 移動標記
       - `3` = 跳過標記（保留所有檔案）
     - 唯一約束: `(Hash, FilePath)`
     - 索引: `idx_hash`, `idx_mark_type`

  2. **FilesToDelete** - 標記待刪除的檔案
     - `Id`: 主鍵
     - `Hash`: 對應的雜湊值
     - `FilePath`: 檔案路徑（唯一）
     - `MarkedAt`: 標記時間
     - `IsProcessed`: 處理狀態（0=未處理, 1=已處理）
     - `ProcessedAt`: 處理時間戳記

  3. **SkippedHashes** - 標記略過的檔案群組
     - `Id`: 主鍵
     - `Hash`: 雜湊值
     - `FilePath`: 檔案路徑
     - `SkippedAt`: 略過時間
     - 唯一約束: `(Hash, FilePath)`

  4. **FileToMove** - 標記待移動的檔案及目標路徑
     - `Id`: 主鍵
     - `Hash`: 雜湊值
     - `SourcePath`: 來源路徑（唯一）
     - `TargetPath`: 目標路徑（根據檔案修改日期計算：yyyy-MM）
     - `MarkedAt`: 標記時間
     - `IsProcessed`: 處理狀態（0=未處理, 1=已處理）
     - `ProcessedAt`: 處理時間戳記

### Program.cs 主要功能

**重複檔案掃描與管理**:
- `ScanAndWriteDuplicates()`: 掃描資料夾並計算檔案雜湊值
- `InteractiveDeleteDuplicates()`: 互動式選擇要刪除的重複檔案
  - **互動指令**:
    - `d [編號]`: 標記檔案為待刪除（例如: `d 1,2,3`）
    - `m [編號]`: 標記檔案為待移動（例如: `m 1,2,3`）
    - `k`: 保留所有檔案並跳過此組（設定 MarkType = 3）
    - `a`: 自動保留最舊的檔案，標記其他為待刪除
    - `p [編號]`: 預覽指定檔案（例如: `p 1,2,3`）
    - `n`: 跳過當前群組
    - `q`: 退出互動模式
- `MarkFileForDeletion()` / `ExecuteMarkedDeletions()`: 標記和執行刪除
- `MarkHashAsSkipped()`: 標記要略過的檔案群組

**報表生成**:
- `GenerateReport()`: 統一的報表生成方法（JSON 和 HTML）
- `GenerateDuplicateAnalysisReport()`: 生成包含移動標記功能的互動式 HTML 報表
- **Templates 資料夾**: 包含 HTML 範本檔案
  - `DuplicateAnalysisReport.html`: 重複檔案分析報表範本
  - `MarkedForDeletionReport.html`: 待刪除檔案報表範本
  - `SkippedFilesReport.html`: 已略過檔案報表範本
- **Reports 資料夾**: 自動生成的報表輸出目錄（JSON 和 HTML 格式）

**API 服務器**:
- 內建 HTTP 伺服器（使用 `HttpListener`）用於提供互動式報表
- API 端點:
  - `POST /api/mark-for-deletion`: 標記檔案為待刪除
  - `POST /api/unmark-file`: 取消標記檔案
  - `POST /api/mark-for-move`: 標記檔案為待移動
  - `POST /api/unmark-move`: 取消移動標記
  - `GET /api/duplicate-analysis`: 取得重複檔案分析資料

### 共用工具方法

**預覽與顯示**:
- `HandlePreviewCommand()`: 處理預覽指令（例如 "p 1,2,3"）
- `DisplayMenu()`: 顯示選單選項
- `DisplayFileInfo()` / `DisplayFileGroup()`: 顯示檔案資訊

**資料處理**:
- `CreateReportData()` / `SerializeReportData()`: 報表資料序列化
- `ParseIndices()` / `ParsePreviewIndices()`: 解析使用者輸入的編號

**使用者互動**:
- `ConfirmAction()`: 統一的確認對話框（Y/n 格式）

## 支援的檔案格式

**圖片**: `.jpg`, `.jpeg`, `.png`, `.gif`, `.bmp`, `.webp`, `.tiff`, `.tif`, `.ico`, `.svg`, `.heic`, `.heif`, `.raw`, `.cr2`, `.nef`, `.arw`, `.dng`

**影片**: `.mp4`, `.avi`, `.mkv`, `.mov`, `.wmv`, `.flv`, `.webm`, `.m4v`, `.mpg`, `.mpeg`

## 專案架構說明

此專案經過多階段重構,採用輕量級架構設計。核心特點:
- 使用 `DatabaseHelper` 統一資料庫存取層
- 採用 `record` 型別實現不可變資料模型
- 共用方法消除重複程式碼
- 詳細的重構歷史請參閱 `重構計畫.md`

## 開發注意事項

### 資料庫操作
- 所有新的資料庫操作都應使用 `DatabaseHelper` 的方法
- 簡單查詢使用 `ExecuteQuery()`
- INSERT/UPDATE/DELETE 使用 `ExecuteNonQuery()`
- 批次操作使用 `ExecuteTransaction()`

### MarkType 欄位處理邏輯

**核心原則**: `MarkType` 欄位用於追蹤檔案的處理狀態,確保已處理的檔案不會重複出現在互動式選單中。

**自動排除機制**:
- `LoadDuplicateGroupsWithDetails()`: 查詢時使用 `WHERE MarkType = 0`,自動排除已標記的檔案
- `LoadExistingHashes()`: 掃描檔案時也排除 `MarkType > 0` 的資料
- `AnalyzeDuplicateFiles()`: 分析報表時僅包含 `MarkType = 0` 的重複檔案

**標記操作流程**:
1. **標記為刪除** (`d` 命令):
   - 寫入 `FilesToDelete` 資料表
   - 更新 `DuplicateFiles.MarkType = 1`
   - 下次執行「選項 2」時自動排除

2. **標記為移動** (`m` 命令):
   - 寫入 `FileToMove` 資料表（含來源/目標路徑）
   - 更新 `DuplicateFiles.MarkType = 2`
   - 目標路徑根據檔案修改日期計算（yyyy-MM 格式）
   - 下次執行「選項 2」時自動排除

3. **保留檔案並跳過** (`k` 命令):
   - 寫入 `SkippedHashes` 資料表
   - 更新 `DuplicateFiles.MarkType = 3`
   - 表示該組檔案已確認保留，不再顯示
   - 下次執行「選項 2」時自動排除

4. **取消標記**:
   - 刪除對應的 `FilesToDelete`、`FileToMove` 或 `SkippedHashes` 記錄
   - 重置 `DuplicateFiles.MarkType = 0`
   - 檔案重新出現在互動式選單中

**查詢範例**:
```sql
-- 僅顯示未處理的重複檔案
SELECT * FROM DuplicateFiles WHERE MarkType = 0

-- 查詢所有已標記的檔案
SELECT * FROM DuplicateFiles WHERE MarkType > 0

-- 查詢待刪除的檔案
SELECT * FROM DuplicateFiles WHERE MarkType = 1

-- 查詢待移動的檔案
SELECT * FROM DuplicateFiles WHERE MarkType = 2

-- 查詢已跳過保留的檔案
SELECT * FROM DuplicateFiles WHERE MarkType = 3
```

### 程式碼風格
- 使用 record 型別定義不可變資料模型
- 使用 `using` 陳述式管理資源（連線、檔案等）
- 參數化查詢防止 SQL 注入
- 使用 `StringComparer.OrdinalIgnoreCase` 進行檔案路徑比較

### 錯誤處理
- 檔案操作應檢查檔案存在性
- 資料庫交易在失敗時自動回滾
- 提供清晰的錯誤訊息給使用者
