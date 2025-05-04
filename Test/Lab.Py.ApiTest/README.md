# Member RESTful API with Memory DB

這是一個使用 FastAPI 和 Memory DB 實作的 Member RESTful API 專案。本專案實現了一個完整的會員管理系統，使用記憶體資料庫模擬資料存儲，提供標準的 RESTful API 接口。

## 專案結構

```
lab-py-memory-apitest/
├── app/
│   ├── api/
│   │   ├── __init__.py   # API 路由配置
│   │   └── members.py    # 會員 API 實現
│   ├── db/
│   │   └── memory_db.py  # 記憶體資料庫實現
│   ├── models/
│   │   └── member.py     # 會員資料模型
│   ├── openapi.yml       # OpenAPI 規範文件
│   └── main.py          # 應用程式入口
├── tests/
│   ├── conftest.py      # 測試配置
│   └── test_members_api.py # 會員 API 測試
├── main.py              # 程式啟動入口
├── pyproject.toml       # 專案配置和依賴
└── README.md            # 專案說明文件
```

## 技術棧

- Python 3.13+
- FastAPI - 高性能 Web 框架
- Pydantic - 資料驗證和設定管理
- Uvicorn - ASGI 伺服器
- Memory DB - 記憶體資料庫模擬

## 功能特點

- 完整的 CRUD 操作：創建、讀取、更新和刪除會員資料
- 符合 RESTful API 設計原則
- 自動生成 API 文檔 (Swagger UI 和 ReDoc)
- 資料驗證和類型檢查
- 記憶體資料庫模擬，無需外部資料庫

## 資料模型

會員（Member）模型包含以下欄位：

- `id`: 唯一識別碼 (自動生成)
- `first_name`: 名字
- `last_name`: 姓氏
- `age`: 年齡 (可選)
- `address`: 地址 (可選)
- `birthday`: 生日
- `created_by`: 創建者 (預設為 "system")
- `created_at`: 創建時間 (自動生成)

## 安裝

### 前置需求

- Python 3.13 或更高版本
- uv 0.6.10 或更高版本

### 使用 uv 安裝依賴

```bash
# 建立虛擬環境
uv venv --seed --link-mode=copy

# 安裝所有依賴
uv pip install -e . --link-mode=copy

# 或者單獨安裝依賴
uv add "dependency-injector>=4.46.0" "dotenv>=0.9.9" "fastapi>=0.115.12" "httpx>=0.28.1" "psycopg2>=2.9.10" "pydantic>=2.11.2" "pytest>=8.3.5" "python-dateutil>=2.9.0.post0" "sqlalchemy>=2.0.40" "testcontainers-postgres>=0.0.1rc1" "testcontainers-redis>=0.0.1rc1" "uvicorn>=0.34.0"
```

## 執行

### 方法一：使用 Python 直接執行

```bash
python main.py
```

### 方法二：使用 Uvicorn 執行

```bash
uvicorn app.main:app --reload --host 0.0.0.0 --port 8000
```

## API 文檔

啟動應用程式後，可以在以下網址查看 API 文檔：

- Swagger UI: http://localhost:8000/docs
- ReDoc: http://localhost:8000/redoc

## API 端點

| 方法 | 端點 | 描述 |
|------|------|------|
| GET | /api/v1/members | 獲取所有會員 |
| POST | /api/v1/members | 創建新會員 |
| GET | /api/v1/members/{member_id} | 通過 ID 獲取會員 |
| PUT | /api/v1/members/{member_id} | 更新會員資料 |
| DELETE | /api/v1/members/{member_id} | 刪除會員 |

## API 使用範例

### 創建會員

```bash
curl -X 'POST' \
  'http://localhost:8000/api/v1/members' \
  -H 'Content-Type: application/json' \
  -d '{
  "first_name": "John",
  "last_name": "Doe",
  "age": 30,
  "address": "123 Main St",
  "birthday": "1993-01-15"
}'
```

### 獲取所有會員

```bash
curl -X 'GET' 'http://localhost:8000/api/v1/members'
```

### 獲取特定會員

```bash
curl -X 'GET' 'http://localhost:8000/api/v1/members/{member_id}'
```

### 更新會員

```bash
curl -X 'PUT' \
  'http://localhost:8000/api/v1/members/{member_id}' \
  -H 'Content-Type: application/json' \
  -d '{
  "address": "456 New Address"
}'
```

### 刪除會員

```bash
curl -X 'DELETE' 'http://localhost:8000/api/v1/members/{member_id}'
```

## 開發

### 添加新的 API 端點

1. 在 `app/api/` 目錄下創建新的路由文件
2. 在 `app/api/__init__.py` 中註冊新路由
3. 在 `app/models/` 中添加相應的資料模型
4. 在 `app/db/` 中實現資料存取邏輯

### 運行測試

```bash
# 未來將添加測試功能
```

## 未來改進

- 添加單元測試和整合測試
- 實現持久化存儲選項
- 添加用戶認證和授權
- 實現分頁和過濾功能
- 添加日誌記錄功能

## 授權

[MIT License](LICENSE)