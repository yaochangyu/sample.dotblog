#!/usr/bin/env python3
"""
GitLab 連線快速測試腳本

使用方式：
    uv run python scripts/test_connection.py
"""

from config.gitlab_config import get_gitlab_config

if __name__ == "__main__":
    config = get_gitlab_config()
    config.test_connection()
