"""
Commit 品質分析器

評估 Commit Message 規範性、變更粒度、修復率等指標。
權重：23%（最高）
"""

from pathlib import Path
from typing import Optional, Dict
import pandas as pd
import re

from config.analysis_config import CommitQualityConfig


class CommitQualityAnalyzer:
    """Commit 品質分析器"""

    def __init__(self, input_dir: Optional[str] = None, output_dir: Optional[str] = None):
        if input_dir:
            self.input_dir = Path(input_dir)
        else:
            self.input_dir = Path(__file__).parent.parent / "output" / "processed"

        if output_dir:
            self.output_dir = Path(output_dir)
        else:
            self.output_dir = Path(__file__).parent.parent / "output" / "processed"

        self.output_dir.mkdir(parents=True, exist_ok=True)

    def analyze_developer(self, email: str, commits_df: pd.DataFrame) -> Dict:
        """
        分析單個開發者的 Commit 品質

        Args:
            email: 開發者 email
            commits_df: Commits DataFrame

        Returns:
            評分結果字典
        """
        # 篩選該開發者的 commits
        dev_commits = commits_df[
            commits_df["author_email"].str.lower() == email.lower()
        ].copy()

        if len(dev_commits) == 0:
            return self._empty_result()

        # A. Message 規範性
        message_score, message_details = self._analyze_message_quality(dev_commits)

        # B. 變更粒度
        size_score, size_details = self._analyze_change_size(dev_commits)

        # C. 修復率
        fix_score, fix_details = self._analyze_fix_rate(dev_commits)

        # 綜合評分（三個子維度平均）
        total_score = (message_score + size_score + fix_score) / 3

        return {
            "email": email,
            "total_commits": len(dev_commits),
            # Message 規範性
            "message_score": message_score,
            "conventional_count": message_details["conventional_count"],
            "conventional_rate": message_details["conventional_rate"],
            # 變更粒度
            "size_score": size_score,
            "small_changes": size_details["small_count"],
            "medium_changes": size_details["medium_count"],
            "large_changes": size_details["large_count"],
            "small_rate": size_details["small_rate"],
            # 修復率
            "fix_score": fix_score,
            "fix_count": fix_details["fix_count"],
            "fix_rate": fix_details["fix_rate"],
            # 總分
            "commit_quality_score": total_score,
        }

    def _analyze_message_quality(self, commits_df: pd.DataFrame) -> tuple:
        """分析 Commit Message 規範性"""
        total = len(commits_df)
        conventional_count = 0

        for _, row in commits_df.iterrows():
            title = str(row.get("title", ""))
            if CommitQualityConfig.CONVENTIONAL_COMMIT_PATTERN.match(title):
                conventional_count += 1

        conventional_rate = conventional_count / total if total > 0 else 0

        # 評分
        if conventional_rate >= CommitQualityConfig.MESSAGE_QUALITY_THRESHOLDS["excellent"]:
            score = 9 + (conventional_rate - 0.8) / 0.2  # 9-10分
        elif conventional_rate >= CommitQualityConfig.MESSAGE_QUALITY_THRESHOLDS["good"]:
            score = 5 + (conventional_rate - 0.4) / 0.4 * 4  # 5-9分
        else:
            score = 1 + (conventional_rate / 0.4) * 4  # 1-5分

        return score, {
            "conventional_count": conventional_count,
            "conventional_rate": conventional_rate,
        }

    def _analyze_change_size(self, commits_df: pd.DataFrame) -> tuple:
        """分析變更粒度"""
        total = len(commits_df)
        small_count = 0
        medium_count = 0
        large_count = 0

        for _, row in commits_df.iterrows():
            total_changes = row.get("total", 0)
            if total_changes <= CommitQualityConfig.CHANGE_SIZE_SMALL:
                small_count += 1
            elif total_changes <= CommitQualityConfig.CHANGE_SIZE_MEDIUM:
                medium_count += 1
            else:
                large_count += 1

        small_rate = small_count / total if total > 0 else 0

        # 評分
        if small_rate >= CommitQualityConfig.CHANGE_SIZE_THRESHOLDS["excellent"]:
            score = 9 + (small_rate - 0.6) / 0.4  # 9-10分
        elif small_rate >= CommitQualityConfig.CHANGE_SIZE_THRESHOLDS["good"]:
            score = 6 + (small_rate - 0.4) / 0.2 * 3  # 6-9分
        else:
            score = 1 + (small_rate / 0.4) * 5  # 1-6分

        return score, {
            "small_count": small_count,
            "medium_count": medium_count,
            "large_count": large_count,
            "small_rate": small_rate,
        }

    def _analyze_fix_rate(self, commits_df: pd.DataFrame) -> tuple:
        """分析修復率"""
        total = len(commits_df)
        fix_count = 0

        for _, row in commits_df.iterrows():
            title = str(row.get("title", "")).lower()
            message = str(row.get("message", "")).lower()
            combined = title + " " + message

            if any(keyword in combined for keyword in CommitQualityConfig.FIX_KEYWORDS):
                fix_count += 1

        fix_rate = fix_count / total if total > 0 else 0

        # 評分（修復率越低越好）
        if fix_rate < CommitQualityConfig.FIX_RATE_THRESHOLDS["excellent"]:
            score = 9 + (0.15 - fix_rate) / 0.15  # 9-10分
        elif fix_rate < CommitQualityConfig.FIX_RATE_THRESHOLDS["good"]:
            score = 7 + (0.3 - fix_rate) / 0.15 * 2  # 7-9分
        else:
            score = max(1, 7 - (fix_rate - 0.3) / 0.1 * 2)  # 1-7分

        return score, {"fix_count": fix_count, "fix_rate": fix_rate}

    def _empty_result(self) -> Dict:
        """空結果"""
        return {
            "total_commits": 0,
            "message_score": 0,
            "size_score": 0,
            "fix_score": 0,
            "commit_quality_score": 0,
        }

    def analyze_all_developers(self) -> pd.DataFrame:
        """分析所有開發者的 Commit 品質"""
        print("\n🔍 分析 Commit 品質...")

        # 讀取數據
        commits_file = self.input_dir / "all_commits_merged.csv"
        developers_file = self.input_dir / "unified_developers.csv"

        if not commits_file.exists():
            print("❌ 找不到 Commit 數據檔案")
            return pd.DataFrame()

        if not developers_file.exists():
            print("❌ 找不到開發者數據檔案")
            return pd.DataFrame()

        commits_df = pd.read_csv(commits_file)
        developers_df = pd.read_csv(developers_file)

        results = []
        for _, dev in developers_df.iterrows():
            email = dev["email"]
            result = self.analyze_developer(email, commits_df)
            result["username"] = dev.get("username", "")
            result["name"] = dev.get("name", "")
            results.append(result)

        # 轉換為 DataFrame
        df = pd.DataFrame(results)
        df = df.sort_values("commit_quality_score", ascending=False)

        # 儲存
        output_file = self.output_dir / "commit_quality_scores.csv"
        df.to_csv(output_file, index=False, encoding="utf-8-sig")
        print(f"✅ Commit 品質評分已儲存: {output_file}")

        return df


if __name__ == "__main__":
    analyzer = CommitQualityAnalyzer()
    results = analyzer.analyze_all_developers()
    if len(results) > 0:
        print("\n前 5 位開發者 Commit 品質：")
        print(results.head()[["name", "email", "commit_quality_score", "conventional_rate", "fix_rate"]])
