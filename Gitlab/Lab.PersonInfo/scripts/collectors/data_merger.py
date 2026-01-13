"""
數據合併與清洗模組

整合 GitLab API 和 Git 本地數據，建立開發者統一身份映射，並進行數據清洗。
"""

import re
from pathlib import Path
from typing import Dict, List, Optional, Set, Tuple
import pandas as pd
from tqdm import tqdm

from config.analysis_config import ExclusionConfig


class DataMerger:
    """數據合併與清洗器"""

    def __init__(self, input_dir: Optional[str] = None, output_dir: Optional[str] = None):
        """
        初始化合併器

        Args:
            input_dir: 輸入目錄（raw 數據），預設為 scripts/output/raw/
            output_dir: 輸出目錄（processed 數據），預設為 scripts/output/processed/
        """
        if input_dir:
            self.input_dir = Path(input_dir)
        else:
            self.input_dir = Path(__file__).parent.parent / "output" / "raw"

        if output_dir:
            self.output_dir = Path(output_dir)
        else:
            self.output_dir = Path(__file__).parent.parent / "output" / "processed"

        self.output_dir.mkdir(parents=True, exist_ok=True)

    # ==================== 開發者身份統一 ====================

    def unify_developers(
        self, manual_mapping: Optional[Dict[str, str]] = None
    ) -> pd.DataFrame:
        """
        統一開發者身份（建立 username <-> email <-> name 映射表）

        Args:
            manual_mapping: 手動映射（email -> canonical_email）

        Returns:
            統一開發者身份 DataFrame
        """
        print("\n🔍 統一開發者身份...")

        developers_map = {}  # email -> {username, name, commit_count}

        # 1. 從 Git 本地數據取得開發者
        git_devs_file = self.input_dir / "git_developers.csv"
        if git_devs_file.exists():
            git_devs = pd.read_csv(git_devs_file)
            for _, row in git_devs.iterrows():
                email = row["email"].lower()
                if not self._is_bot(row["name"], email):
                    if email not in developers_map:
                        developers_map[email] = {
                            "username": "",
                            "name": row["name"],
                            "email": email,
                            "commit_count": row["commit_count"],
                            "source": "git",
                        }

        # 2. 從 GitLab MR 數據補充 username
        mr_file = self.input_dir / "gitlab_merge_requests.csv"
        if mr_file.exists():
            mrs = pd.read_csv(mr_file)
            for _, row in mrs.iterrows():
                email = str(row.get("author_email", "")).lower()
                username = row.get("author_username", "")
                name = row.get("author_name", "")

                if email and not self._is_bot(name, email):
                    if email in developers_map:
                        if not developers_map[email]["username"]:
                            developers_map[email]["username"] = username
                    else:
                        developers_map[email] = {
                            "username": username,
                            "name": name,
                            "email": email,
                            "commit_count": 0,
                            "source": "gitlab_mr",
                        }

        # 3. 從 GitLab Commits 數據補充
        gitlab_commits_file = self.input_dir / "gitlab_commits.csv"
        if gitlab_commits_file.exists():
            commits = pd.read_csv(gitlab_commits_file)
            for _, row in commits.iterrows():
                email = str(row.get("author_email", "")).lower()
                name = row.get("author_name", "")

                if email and email in developers_map and not self._is_bot(name, email):
                    developers_map[email]["commit_count"] = (
                        developers_map[email].get("commit_count", 0) + 1
                    )

        # 4. 應用手動映射
        if manual_mapping:
            print(f"   應用 {len(manual_mapping)} 個手動映射")
            merged_map = {}
            for email, canonical_email in manual_mapping.items():
                email = email.lower()
                canonical_email = canonical_email.lower()

                if email in developers_map:
                    if canonical_email not in merged_map:
                        merged_map[canonical_email] = developers_map[email]
                    else:
                        # 合併 commit_count
                        merged_map[canonical_email]["commit_count"] += developers_map[
                            email
                        ]["commit_count"]

            developers_map.update(merged_map)

        # 轉換為 DataFrame
        developers = []
        for email, info in developers_map.items():
            developers.append(
                {
                    "email": email,
                    "username": info.get("username", ""),
                    "name": info.get("name", ""),
                    "commit_count": info.get("commit_count", 0),
                    "source": info.get("source", ""),
                }
            )

        df = pd.DataFrame(developers)
        df = df.sort_values("commit_count", ascending=False)

        # 儲存
        output_file = self.output_dir / "unified_developers.csv"
        df.to_csv(output_file, index=False, encoding="utf-8-sig")
        print(f"✅ 統一開發者身份已儲存: {output_file} (共 {len(df)} 位)")

        return df

    def _is_bot(self, name: str, email: str) -> bool:
        """判斷是否為 Bot 賬號"""
        name_lower = name.lower() if name else ""
        email_lower = email.lower() if email else ""

        return any(
            bot.lower() in name_lower or bot.lower() in email_lower
            for bot in ExclusionConfig.EXCLUDED_BOTS
        )

    # ==================== Commit 數據合併 ====================

    def merge_commits(self) -> pd.DataFrame:
        """
        合併 GitLab API 和 Git 本地的 Commit 數據

        Returns:
            合併後的 Commit DataFrame
        """
        print("\n🔍 合併 Commit 數據...")

        commits_list = []

        # 1. 讀取 Git 本地數據（更詳細）
        git_commits_file = self.input_dir / "git_commits.csv"
        if git_commits_file.exists():
            git_commits = pd.read_csv(git_commits_file)
            git_commits["source"] = "git_local"
            commits_list.append(git_commits)
            print(f"   Git 本地 Commits: {len(git_commits)}")

        # 2. 讀取 GitLab API 數據（補充）
        gitlab_commits_file = self.input_dir / "gitlab_commits.csv"
        if gitlab_commits_file.exists():
            gitlab_commits = pd.read_csv(gitlab_commits_file)
            gitlab_commits["source"] = "gitlab_api"
            commits_list.append(gitlab_commits)
            print(f"   GitLab API Commits: {len(gitlab_commits)}")

        if not commits_list:
            print("⚠️ 沒有找到 Commit 數據檔案")
            return pd.DataFrame()

        # 合併（以 commit_sha 為主鍵去重，優先使用 git_local）
        df = pd.concat(commits_list, ignore_index=True)
        df = df.drop_duplicates(subset=["commit_sha"], keep="first")

        # 清洗：排除特定模式的 Commit
        df = self._clean_commits(df)

        # 儲存
        output_file = self.output_dir / "all_commits_merged.csv"
        df.to_csv(output_file, index=False, encoding="utf-8-sig")
        print(f"✅ 合併 Commit 數據已儲存: {output_file} (共 {len(df)} 筆)")

        return df

    def _clean_commits(self, df: pd.DataFrame) -> pd.DataFrame:
        """清洗 Commit 數據"""
        original_count = len(df)

        # 排除特定模式的 Commit Message
        for pattern in ExclusionConfig.EXCLUDED_COMMIT_PATTERNS:
            df = df[~df["title"].str.match(pattern, na=False)]

        cleaned_count = len(df)
        if cleaned_count < original_count:
            print(f"   排除 {original_count - cleaned_count} 個 Commit（Merge、WIP 等）")

        return df

    # ==================== Review Comments 合併 ====================

    def merge_review_comments(self) -> pd.DataFrame:
        """
        合併 Review Comments 數據

        Returns:
            合併後的 Review Comments DataFrame
        """
        print("\n🔍 合併 Review Comments 數據...")

        comments_file = self.input_dir / "gitlab_review_comments.csv"
        if not comments_file.exists():
            print("⚠️ 沒有找到 Review Comments 數據檔案")
            return pd.DataFrame()

        df = pd.read_csv(comments_file)

        # 清洗：排除系統訊息（已在收集時處理）
        # 分類 Comment 類型（LGTM-only vs 有建議）
        df = self._classify_comments(df)

        # 儲存
        output_file = self.output_dir / "all_reviews_merged.csv"
        df.to_csv(output_file, index=False, encoding="utf-8-sig")
        print(f"✅ 合併 Review Comments 數據已儲存: {output_file} (共 {len(df)} 筆)")

        return df

    def _classify_comments(self, df: pd.DataFrame) -> pd.DataFrame:
        """分類 Review Comment 類型"""
        from config.analysis_config import CodeReviewConfig

        def is_lgtm_only(body: str) -> bool:
            """判斷是否為 LGTM-only 評論"""
            if not body or len(body.strip()) < 5:
                return True

            body_lower = body.lower()
            return any(keyword in body_lower for keyword in CodeReviewConfig.LGTM_KEYWORDS)

        df["is_lgtm_only"] = df["body"].apply(is_lgtm_only)
        df["has_suggestion"] = ~df["is_lgtm_only"]

        return df

    # ==================== 檔案變更清洗 ====================

    def clean_file_changes(self) -> pd.DataFrame:
        """
        清洗檔案變更數據（排除自動生成檔案）

        Returns:
            清洗後的檔案變更 DataFrame
        """
        print("\n🔍 清洗檔案變更數據...")

        file_changes_file = self.input_dir / "git_file_changes.csv"
        if not file_changes_file.exists():
            print("⚠️ 沒有找到檔案變更數據檔案")
            return pd.DataFrame()

        df = pd.read_csv(file_changes_file)
        original_count = len(df)

        # 排除自動生成的檔案
        def should_exclude(file_path: str) -> bool:
            """判斷檔案是否應該被排除"""
            if not file_path:
                return True

            for pattern in ExclusionConfig.EXCLUDED_FILE_PATTERNS:
                # 簡單的 glob 模式匹配
                if "*" in pattern:
                    # 轉換為正則表達式
                    regex_pattern = pattern.replace("*", ".*")
                    if re.match(regex_pattern, file_path):
                        return True
                else:
                    # 精確匹配
                    if pattern in file_path:
                        return True

            return False

        df = df[~df["file_path"].apply(should_exclude)]

        cleaned_count = len(df)
        if cleaned_count < original_count:
            print(
                f"   排除 {original_count - cleaned_count} 個檔案變更（lock 檔案、dist/ 等）"
            )

        # 儲存
        output_file = self.output_dir / "file_changes_cleaned.csv"
        df.to_csv(output_file, index=False, encoding="utf-8-sig")
        print(f"✅ 清洗檔案變更數據已儲存: {output_file} (共 {len(df)} 筆)")

        return df

    # ==================== 完整合併與清洗 ====================

    def process_all(
        self, manual_developer_mapping: Optional[Dict[str, str]] = None
    ) -> Dict[str, pd.DataFrame]:
        """
        執行完整的數據合併與清洗流程

        Args:
            manual_developer_mapping: 手動開發者映射

        Returns:
            包含所有處理後數據的字典
        """
        print("=" * 60)
        print("開始數據合併與清洗")
        print("=" * 60)

        results = {}

        # 1. 統一開發者身份
        results["developers"] = self.unify_developers(manual_developer_mapping)

        # 2. 合併 Commits
        results["commits"] = self.merge_commits()

        # 3. 合併 Review Comments
        results["reviews"] = self.merge_review_comments()

        # 4. 清洗檔案變更
        results["file_changes"] = self.clean_file_changes()

        print("\n" + "=" * 60)
        print("✅ 所有數據合併與清洗完成！")
        print("=" * 60)
        print(f"統一開發者: {len(results['developers'])} 位")
        print(f"合併 Commits: {len(results['commits'])} 筆")
        print(f"合併 Reviews: {len(results['reviews'])} 筆")
        print(f"清洗檔案變更: {len(results['file_changes'])} 筆")
        print("=" * 60)

        return results


# 測試功能
if __name__ == "__main__":
    print("=" * 60)
    print("數據合併與清洗器測試")
    print("=" * 60)

    merger = DataMerger()

    # 測試：統一開發者身份
    print("\n測試：統一開發者身份")
    try:
        developers_df = merger.unify_developers()
        print(f"\n統一後開發者數量: {len(developers_df)}")
        if len(developers_df) > 0:
            print("\n前 5 位開發者：")
            print(developers_df.head()[["email", "username", "name", "commit_count"]])
    except Exception as e:
        print(f"⚠️ 測試失敗: {e}")

    print("\n" + "=" * 60)
    print("✅ 測試完成")
    print("=" * 60)
