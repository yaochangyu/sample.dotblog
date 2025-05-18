# 計算機專案

這是一個使用 Python 實作的簡單計算機，支援基本的四則運算功能。

## 環境設定

1. 使用 uv 建立虛擬環境：
```bash
uv venv
```

2. 啟動虛擬環境：
- Windows:
```bash
.venv/Scripts/activate
```
- Linux/Mac:
```bash
source .venv/bin/activate
```

3. 安裝依賴套件：
```bash
uv pip install -r requirements.txt
```

## 執行測試

執行 BDD 測試：
```bash
pytest test_calculator_step.py -v
```

## 功能說明

- 支援加法（+）
- 支援減法（-）
- 支援乘法（*）
- 支援除法（/）
- 包含除以零的錯誤處理 