"""
配置模組，負責載入環境變數和提供全局配置
"""
import os

from dotenv import load_dotenv

# 載入環境變數（只會執行一次）
load_dotenv()

# 資料庫連接設定
DATABASE_URL = os.getenv("DATABASE_URL")

def parse_bool(value, default=False):
    """將字符串轉換為布林值"""
    if value is None:
        return default
    value = value.lower()
    if value in ('true', 't', 'yes', 'y', '1'):
        return True
    elif value in ('false', 'f', 'no', 'n', '0'):
        return False
    else:
        return default

USE_POSTGRES = parse_bool(os.getenv("USE_POSTGRES", "false"))
