"""
程式碼貢獻量分析器

評估提交次數、活躍度、涉及檔案數等指標。
權重：12%
"""

from pathlib import Path
from typing import Optional, Dict
import pandas as pd
from datetime import datetime

from config.analysis_config import ContributionConfig


class ContributionAnalyzer:
    """程式碼貢獻量分析器"""

    def __init__(self, input_dir: Optional[str] = None, output_dir: Optional[str] = None):
        if input_dir:
            self.input_dir = Path(input_dir)
        else:
            self.input_dir = Path(__file__).parent.parent / "output" / "processed"

        if output_dir:
            self.output_dir = Path(output_dir)
        else:
            self.output_dir = Path(__file__).parent.parent / "output" / "processed"

    def analyze_developer(self, email: str, commits_df: pd.DataFrame, file_changes_df: pd.DataFrame) -> Dict:
        """分析單個開發者的貢獻量"""
        dev_commits = commits_df[commits_df["author_email"].str.lower() == email.lower()]
        dev_files = file_changes_df[file_changes_df["author_email"].str.lower() == email.lower()]

        if len(dev_commits) == 0:
            return self._empty_result(email)

        # 提交次數
        commit_count = len(dev_commits)

        # 程式碼行數
        total_additions = dev_commits["additions"].sum()
        total_deletions = dev_commits["deletions"].sum()
        total_lines = total_additions + total_deletions

        # 涉及檔案數
        unique_files = dev_files["file_path"].nunique()

        # 活躍天數
        dev_commits["date"] = pd.to_datetime(dev_commits["authored_date"], errors="coerce")
        active_days = dev_commits["date"].dt.date.nunique()

        # 評分（基於提交次數）
        if commit_count >= ContributionConfig.COMMIT_COUNT_THRESHOLDS["high"]:
            score = 10
        elif commit_count >= ContributionConfig.COMMIT_COUNT_THRESHOLDS["stable"]:
            score = 8
        elif commit_count >= ContributionConfig.COMMIT_COUNT_THRESHOLDS["medium"]:
            score = 6
        else:
            score = 4

        return {
            "email": email,
            "commit_count": commit_count,
            "total_additions": int(total_additions),
            "total_deletions": int(total_deletions),
            "total_lines": int(total_lines),
            "unique_files": unique_files,
            "active_days": active_days,
            "contribution_score": score,
        }

    def _empty_result(self, email: str) -> Dict:
        return {
            "email": email,
            "commit_count": 0,
            "total_additions": 0,
            "total_deletions": 0,
            "total_lines": 0,
            "unique_files": 0,
            "active_days": 0,
            "contribution_score": 0,
        }

    def analyze_all_developers(self) -> pd.DataFrame:
        """分析所有開發者的貢獻量"""
        print("\n🔍 分析程式碼貢獻量...")

        commits_file = self.input_dir / "all_commits_merged.csv"
        file_changes_file = self.input_dir / "file_changes_cleaned.csv"
        developers_file = self.input_dir / "unified_developers.csv"

        if not all([commits_file.exists(), developers_file.exists()]):
            print("❌ 缺少必要的數據檔案")
            return pd.DataFrame()

        commits_df = pd.read_csv(commits_file)
        developers_df = pd.read_csv(developers_file)
        file_changes_df = pd.read_csv(file_changes_file) if file_changes_file.exists() else pd.DataFrame()

        results = []
        for _, dev in developers_df.iterrows():
            result = self.analyze_developer(dev["email"], commits_df, file_changes_df)
            result["username"] = dev.get("username", "")
            result["name"] = dev.get("name", "")
            results.append(result)

        df = pd.DataFrame(results).sort_values("contribution_score", ascending=False)

        output_file = self.output_dir / "contribution_scores.csv"
        df.to_csv(output_file, index=False, encoding="utf-8-sig")
        print(f"✅ 貢獻量評分已儲存: {output_file}")

        return df


if __name__ == "__main__":
    analyzer = ContributionAnalyzer()
    results = analyzer.analyze_all_developers()
    if len(results) > 0:
        print("\n前 5 位開發者貢獻量：")
        print(results.head()[["name", "commit_count", "total_lines", "contribution_score"]])
