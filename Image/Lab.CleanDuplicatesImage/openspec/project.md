# Project Context

## Purpose
這是一個 .NET 9 主控台應用程式，用於掃描、管理和刪除重複的媒體檔案（圖片和影片）。

**核心目標**:
- 使用 SHA-256 雜湊值識別重複檔案
- 提供互動式介面讓使用者標記檔案（刪除/移動/保留）
- 自動歸檔系統：基於權重計算保留最重要的檔案
- 將所有資料儲存在本機 SQLite 資料庫，支援增量掃描
- 生成 HTML 和 JSON 報表供分析使用

## Tech Stack
- **.NET 9** - 主要開發框架
- **SQLite** (Microsoft.Data.Sqlite 9.0.9) - 本機資料庫
- **Microsoft.Extensions.Configuration.Json** (9.0.0) - 設定檔讀取
- **Microsoft.Extensions.Configuration.Binder** (9.0.0) - 設定綁定
- **C# record 型別** - 不可變資料模型
- **ADO.NET** - 資料庫存取層

## Project Conventions

### Code Style
- **使用 record 型別** 定義不可變資料模型（FileRecord, FileGroup, FileMoveInfo）
- **使用 `using` 陳述式** 管理資源生命週期（連線、檔案串流等）
- **參數化查詢** 防止 SQL 注入攻擊
- **檔案路徑比較** 使用 `StringComparer.OrdinalIgnoreCase`
- **錯誤處理**: 檔案操作前檢查存在性，資料庫交易失敗自動回滾
- **命名慣例**: 使用描述性方法名稱（例如: `LoadDuplicateGroupsWithDetails()`）

### Architecture Patterns
採用**輕量級三層式架構設計**:

1. **資料模型層** (Data Models)
   - `FileRecord`: 單一檔案記錄（路徑、時間戳記、大小、存在性）
   - `FileGroup`: 重複檔案群組（共享 Hash、檔案清單、統計資訊）
   - `FileMoveInfo`: 檔案移動資訊（來源/目標路徑、中繼資料）

2. **資料庫存取層** (Database Layer)
   - `DatabaseHelper`: 統一的資料庫操作介面
     - `CreateConnection()`: 建立 SQLite 連線
     - `ExecuteQuery<T>()`: 查詢並處理結果
     - `ExecuteNonQuery()`: 執行 INSERT/UPDATE/DELETE
     - `ExecuteTransaction()`: 批次交易操作

3. **業務邏輯層** (Business Logic)
   - `Program.cs`: 主要功能方法
     - 重複檔案掃描與管理
     - 互動式標記流程
     - 自動歸檔與權重計算
     - 報表生成

**核心設計原則**:
- 共用方法消除重複程式碼
- 資料庫操作統一透過 `DatabaseHelper`
- 狀態管理透過 `MarkType` 欄位實現

### Testing Strategy
目前專案以手動測試為主，透過互動式命令列介面驗證功能。

**測試重點**:
- 檔案雜湊計算正確性
- 資料庫交易完整性
- 檔案移動/刪除操作安全性
- 報表生成準確性

### Git Workflow
- **主分支**: `master`
- **Commit 訊息格式**: 使用繁體中文與 Gitmoji
  - 範例: `✨ feat(Program.cs): 新增依 Hash 查看並標記重複檔案的功能`
  - 範例: `🔧 refactor(file): 更新選單選項，簡化使用者輸入界面`
- **提交策略**: 功能完成後提交，包含清晰的變更說明

## Domain Context

### 支援的媒體格式
**圖片**: `.jpg`, `.jpeg`, `.png`, `.gif`, `.bmp`, `.webp`, `.tiff`, `.tif`, `.ico`, `.svg`, `.heic`, `.heif`, `.raw`, `.cr2`, `.nef`, `.arw`, `.dng`

**影片**: `.mp4`, `.avi`, `.mkv`, `.mov`, `.wmv`, `.flv`, `.webm`, `.m4v`, `.mpg`, `.mpeg`

### 資料庫架構
**主要資料表** (共 4 個):

1. **DuplicateFiles** - 重複檔案主表
   - 唯一約束: `(Hash, FilePath)`
   - 索引: `idx_hash`, `idx_mark_type`
   - 關鍵欄位: `MarkType` (0=未標記, 1=待刪除, 2=待移動, 3=已跳過)

2. **FilesToDelete** - 待刪除檔案追蹤
   - 包含處理狀態: `IsProcessed`, `ProcessedAt`

3. **SkippedHashes** - 已跳過的檔案群組
   - 唯一約束: `(Hash, FilePath)`

4. **FileToMove** - 待移動檔案及目標路徑
   - 目標路徑根據檔案修改日期計算（`yyyy-MM` 格式）
   - 包含處理狀態: `IsProcessed`, `ProcessedAt`

### MarkType 狀態機制
`MarkType` 欄位追蹤檔案處理狀態，確保已處理的檔案不會重複出現在互動選單中：

- **0** = 未標記（未處理）
- **1** = 待刪除標記
- **2** = 待移動標記
- **3** = 已跳過保留（保留所有檔案）

**自動排除機制**:
- `LoadDuplicateGroupsWithDetails()` 使用 `WHERE MarkType = 0`
- 已標記的檔案不會出現在互動式選單中

### 權重計算系統（自動歸檔功能）
用於判斷重複檔案中要保留哪一個。

**計算公式**: `最終權重 = 基礎權重 + 加分項 - 減分項`

- **基礎權重**: 80 分
- **加分項**: 完整目錄路徑長度（每 5 字元 +1 分）
  - 原理: 描述性長路徑的檔案通常更有組織、更重要
- **減分項**:
  - 檔名包含 `(1)`: -10 分（瀏覽器重複下載）
  - 檔名包含 `-DESKTOP-0M8E5B6`: -10 分（系統生成臨時檔案）

**排序規則** (保留優先順序):
1. 權重最高的檔案
2. 權重相同時，保留修改時間最舊的檔案
3. 修改時間也相同時，保留路徑最長的檔案

### 檔案組織結構
- **Templates/**: HTML 報表範本（編譯時複製到輸出目錄）
- **Reports/**: 自動生成的報表輸出目錄（執行時建立）
- **appsettings.json**: 設定檔（包含 `DefaultMoveTargetBasePath`）
- **duplicates.db**: SQLite 資料庫檔案（首次執行時自動建立）

### 典型工作流程

**初次使用**:
1. 修改 `appsettings.json` 設定目標路徑
2. 執行「選項 1」掃描重複檔案
3. 執行「選項 12」自動標記（基於權重計算）
4. 執行「選項 2」檢視並調整標記
5. 執行「選項 7」實際刪除標記的檔案

**手動標記流程**:
1. 執行「選項 2」進入互動模式
2. 使用指令: `d` (刪除) / `m` (移動) / `k` (保留) / `p` (預覽)
3. 完成後執行「選項 7」或「選項 8」

**依 Hash 精確標記**:
1. 執行「選項 3」進入依 Hash 標記模式
2. 輸入完整 SHA-256 雜湊值（64 字元）
3. 使用互動指令進行標記

## Important Constraints

### 技術限制
- **本機檔案系統**: 僅處理本機可存取的檔案
- **I/O 密集**: 計算 SHA-256 需要讀取完整檔案內容
- **單執行緒**: 主控台應用程式，互動式命令列介面
- **SQLite 限制**: 單一檔案資料庫，不支援高併發

### 業務約束
- **增量掃描**: 支援多次掃描同一資料夾，僅處理新檔案或已變更的檔案
- **安全機制**:
  - 刪除前需使用者確認
  - 標記與實際執行分離（兩階段操作）
  - 檔案移動時檢查 Hash 衝突

### 設定檔依賴
- **appsettings.json** 必須存在於執行目錄
- `DefaultMoveTargetBasePath` 必須為有效路徑

## External Dependencies
**無外部 API 或服務依賴**

所有功能皆在本機執行:
- 資料存儲: SQLite 本機資料庫 (`duplicates.db`)
- 檔案操作: .NET 檔案系統 API
- 設定管理: JSON 設定檔 (`appsettings.json`)
