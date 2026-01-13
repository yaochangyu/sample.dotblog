"""
GitLab 連線配置模組

提供 GitLab API 連線管理、Token 驗證、請求節流等功能。
"""

import os
import sys
import time
from pathlib import Path
from typing import Optional
import gitlab
from dotenv import load_dotenv


class GitLabConfig:
    """GitLab 連線配置類別"""

    def __init__(self):
        """初始化配置，載入環境變數"""
        # 載入 .env 檔案
        env_path = Path(__file__).parent.parent.parent / ".env"
        load_dotenv(dotenv_path=env_path)

        # GitLab 連線設定
        self.gitlab_url = os.getenv("GITLAB_URL", "https://gitlab.com")
        self.gitlab_token = os.getenv("GITLAB_TOKEN")

        # API 請求設定
        self.api_request_delay = float(os.getenv("API_REQUEST_DELAY", "0.3"))
        self.api_max_retries = int(os.getenv("API_MAX_RETRIES", "3"))
        self.api_timeout = int(os.getenv("API_TIMEOUT", "30"))

        # 驗證必要設定
        self._validate_config()

        # GitLab 客戶端（延遲初始化）
        self._gl_client: Optional[gitlab.Gitlab] = None

    def _validate_config(self) -> None:
        """驗證配置的完整性"""
        if not self.gitlab_token:
            print("❌ 錯誤：未設定 GITLAB_TOKEN 環境變數")
            print("\n請執行以下步驟：")
            print("1. 前往 GitLab → Settings → Access Tokens")
            print("2. 建立 Token，勾選權限：read_api, read_repository, read_user")
            print("3. 複製 .env.example 為 .env")
            print("4. 將 Token 填入 .env 檔案的 GITLAB_TOKEN 欄位")
            sys.exit(1)

        if not self.gitlab_url:
            print("❌ 錯誤：未設定 GITLAB_URL 環境變數")
            sys.exit(1)

    def get_client(self) -> gitlab.Gitlab:
        """
        取得 GitLab 客戶端實例（單例模式）

        Returns:
            gitlab.Gitlab: GitLab API 客戶端

        Raises:
            gitlab.exceptions.GitlabAuthenticationError: Token 認證失敗
            gitlab.exceptions.GitlabError: 其他 GitLab API 錯誤
        """
        if self._gl_client is None:
            try:
                print(f"🔗 連線到 GitLab: {self.gitlab_url}")
                self._gl_client = gitlab.Gitlab(
                    url=self.gitlab_url,
                    private_token=self.gitlab_token,
                    timeout=self.api_timeout,
                )

                # 驗證 Token 是否有效
                self._gl_client.auth()
                current_user = self._gl_client.user
                print(f"✅ 認證成功！使用者: {current_user.username}")

            except gitlab.exceptions.GitlabAuthenticationError as e:
                print(f"❌ GitLab Token 認證失敗: {e}")
                print("\n請檢查：")
                print("1. Token 是否正確")
                print("2. Token 權限是否包含：read_api, read_repository, read_user")
                print("3. Token 是否已過期")
                sys.exit(1)

            except gitlab.exceptions.GitlabError as e:
                print(f"❌ GitLab 連線失敗: {e}")
                sys.exit(1)

            except Exception as e:
                print(f"❌ 未預期的錯誤: {e}")
                sys.exit(1)

        return self._gl_client

    def test_connection(self) -> bool:
        """
        測試 GitLab 連線是否正常

        Returns:
            bool: 連線成功返回 True，失敗返回 False
        """
        try:
            gl = self.get_client()
            user = gl.user
            print(f"✅ 連線測試成功！")
            print(f"   使用者: {user.username}")
            print(f"   Email: {user.email}")
            print(f"   ID: {user.id}")
            return True
        except Exception as e:
            print(f"❌ 連線測試失敗: {e}")
            return False

    def rate_limit_wait(self) -> None:
        """API 請求節流：在每次請求後等待，避免觸發 Rate Limiting"""
        time.sleep(self.api_request_delay)

    def get_projects(
        self,
        per_page: int = 100,
        all: bool = True,
        visibility: Optional[str] = None,
        owned: bool = False,
    ) -> list:
        """
        取得可訪問的專案列表

        Args:
            per_page: 每頁結果數（最大 100）
            all: 是否取得所有專案（自動分頁）
            visibility: 可見性過濾 ('public', 'internal', 'private')
            owned: 只取得自己擁有的專案

        Returns:
            list: 專案列表
        """
        gl = self.get_client()

        params = {"per_page": per_page}
        if visibility:
            params["visibility"] = visibility
        if owned:
            params["owned"] = True

        if all:
            return gl.projects.list(all=True, **params)
        else:
            return gl.projects.list(**params)


# 全域配置實例（單例）
_config_instance: Optional[GitLabConfig] = None


def get_gitlab_config() -> GitLabConfig:
    """
    取得 GitLab 配置實例（單例模式）

    Returns:
        GitLabConfig: GitLab 配置實例
    """
    global _config_instance
    if _config_instance is None:
        _config_instance = GitLabConfig()
    return _config_instance


def get_gitlab_client() -> gitlab.Gitlab:
    """
    取得 GitLab 客戶端實例（快捷方法）

    Returns:
        gitlab.Gitlab: GitLab API 客戶端
    """
    config = get_gitlab_config()
    return config.get_client()


# 測試連線功能（可獨立執行此模組測試）
if __name__ == "__main__":
    print("=" * 60)
    print("GitLab 連線配置測試")
    print("=" * 60)

    config = get_gitlab_config()

    # 測試連線
    if config.test_connection():
        print("\n" + "=" * 60)
        print("測試可訪問的專案（前 5 個）")
        print("=" * 60)

        try:
            gl = config.get_client()
            projects = gl.projects.list(per_page=5)

            print(f"\n找到 {len(projects)} 個專案：\n")
            for project in projects:
                print(f"  📦 {project.name}")
                print(f"     ID: {project.id}")
                print(f"     Path: {project.path_with_namespace}")
                print(f"     URL: {project.web_url}")
                print()

        except Exception as e:
            print(f"❌ 取得專案列表失敗: {e}")

    print("=" * 60)
    print("測試完成")
    print("=" * 60)
