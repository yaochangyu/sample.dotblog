"""
Git 本地數據收集器

使用本地 Git 命令收集 Commit、檔案變更等數據。
"""

import os
import re
import subprocess
from pathlib import Path
from typing import List, Dict, Optional, Tuple
from datetime import datetime
from tqdm import tqdm
import pandas as pd

from config.analysis_config import ExclusionConfig


class GitLocalCollector:
    """Git 本地數據收集器"""

    def __init__(self, repo_path: str, output_dir: Optional[str] = None):
        """
        初始化收集器

        Args:
            repo_path: Git Repository 路徑
            output_dir: 輸出目錄路徑，預設為 scripts/output/raw/
        """
        self.repo_path = Path(repo_path)
        if not self._is_git_repo():
            raise ValueError(f"{repo_path} 不是有效的 Git Repository")

        # 設定輸出目錄
        if output_dir:
            self.output_dir = Path(output_dir)
        else:
            self.output_dir = Path(__file__).parent.parent / "output" / "raw"

        self.output_dir.mkdir(parents=True, exist_ok=True)

    def _is_git_repo(self) -> bool:
        """檢查是否為 Git Repository"""
        return (self.repo_path / ".git").exists()

    def _run_git_command(
        self, args: List[str], check: bool = True
    ) -> subprocess.CompletedProcess:
        """
        執行 Git 命令

        Args:
            args: Git 命令參數
            check: 是否檢查返回碼

        Returns:
            subprocess.CompletedProcess
        """
        cmd = ["git", "-C", str(self.repo_path)] + args
        result = subprocess.run(
            cmd, capture_output=True, text=True, encoding="utf-8", check=check
        )
        return result

    # ==================== Commit 收集 ====================

    def collect_commits(
        self,
        author: Optional[str] = None,
        since: Optional[str] = None,
        until: Optional[str] = None,
        branch: Optional[str] = None,
    ) -> pd.DataFrame:
        """
        收集 Commit 數據

        Args:
            author: 作者名稱或 Email（支援部分匹配）
            since: 開始日期 (格式: YYYY-MM-DD)
            until: 結束日期 (格式: YYYY-MM-DD)
            branch: 分支名稱（預設為當前分支）

        Returns:
            Commit 列表 DataFrame
        """
        print(f"\n🔍 收集 Commit 數據（本地 Git）...")
        if author:
            print(f"   作者篩選: {author}")
        if since or until:
            print(f"   時間範圍: {since or '最早'} ~ {until or '現在'}")

        commit_data = []

        # 建立 git log 命令
        # 格式: SHA|作者名稱|作者Email|提交時間|Committer名稱|Committer Email|提交時間|標題|完整訊息
        log_format = "%H|%an|%ae|%ad|%cn|%ce|%cd|%s|%B"
        args = [
            "log",
            f"--format={log_format}",
            "--date=iso",
            "--numstat",  # 顯示變更統計
        ]

        if author:
            args.append(f"--author={author}")
        if since:
            args.append(f"--since={since}")
        if until:
            args.append(f"--until={until}")
        if branch:
            args.append(branch)

        result = self._run_git_command(args)
        output = result.stdout

        # 解析輸出
        commits = self._parse_commit_log(output)
        commit_data.extend(commits)

        # 轉換為 DataFrame
        df = pd.DataFrame(commit_data)

        # 儲存到 CSV
        output_file = self.output_dir / "git_commits.csv"
        df.to_csv(output_file, index=False, encoding="utf-8-sig")
        print(f"✅ Commit 數據已儲存: {output_file} (共 {len(df)} 筆)")

        return df

    def _parse_commit_log(self, output: str) -> List[Dict]:
        """解析 git log 輸出"""
        commits = []
        lines = output.split("\n")

        i = 0
        while i < len(lines):
            line = lines[i].strip()

            # 解析 commit 基本資訊行
            if "|" in line:
                parts = line.split("|")
                if len(parts) >= 9:
                    commit_info = {
                        "commit_sha": parts[0],
                        "author_name": parts[1],
                        "author_email": parts[2],
                        "authored_date": parts[3],
                        "committer_name": parts[4],
                        "committer_email": parts[5],
                        "committed_date": parts[6],
                        "title": parts[7],
                        "message": parts[8],
                    }

                    # 統計變更（下一行開始）
                    additions = 0
                    deletions = 0
                    changed_files = 0

                    i += 1
                    while i < len(lines):
                        stat_line = lines[i].strip()
                        if not stat_line:  # 空行表示下一個 commit
                            break
                        if "|" in stat_line:  # 遇到下一個 commit
                            i -= 1
                            break

                        # 解析 numstat: additions  deletions  filename
                        parts = stat_line.split("\t")
                        if len(parts) >= 3:
                            try:
                                add = int(parts[0]) if parts[0] != "-" else 0
                                delete = int(parts[1]) if parts[1] != "-" else 0
                                additions += add
                                deletions += delete
                                changed_files += 1
                            except ValueError:
                                pass

                        i += 1

                    commit_info["additions"] = additions
                    commit_info["deletions"] = deletions
                    commit_info["total"] = additions + deletions
                    commit_info["changed_files"] = changed_files

                    commits.append(commit_info)

            i += 1

        return commits

    # ==================== 檔案變更統計 ====================

    def collect_file_changes(
        self,
        author: Optional[str] = None,
        since: Optional[str] = None,
        until: Optional[str] = None,
    ) -> pd.DataFrame:
        """
        收集檔案變更統計

        Args:
            author: 作者名稱或 Email
            since: 開始日期
            until: 結束日期

        Returns:
            檔案變更統計 DataFrame
        """
        print(f"\n🔍 收集檔案變更統計...")

        # 建立 git log 命令（只顯示檔案名稱）
        args = ["log", "--name-only", "--pretty=format:%H|%an|%ae|%ad", "--date=iso"]

        if author:
            args.append(f"--author={author}")
        if since:
            args.append(f"--since={since}")
        if until:
            args.append(f"--until={until}")

        result = self._run_git_command(args)
        output = result.stdout

        # 解析輸出
        file_changes = self._parse_file_changes(output)

        # 轉換為 DataFrame
        df = pd.DataFrame(file_changes)

        # 儲存到 CSV
        output_file = self.output_dir / "git_file_changes.csv"
        df.to_csv(output_file, index=False, encoding="utf-8-sig")
        print(f"✅ 檔案變更統計已儲存: {output_file} (共 {len(df)} 筆)")

        return df

    def _parse_file_changes(self, output: str) -> List[Dict]:
        """解析檔案變更輸出"""
        changes = []
        lines = output.split("\n")

        current_commit = None
        current_author_name = None
        current_author_email = None
        current_date = None

        for line in lines:
            line = line.strip()
            if not line:
                continue

            # 解析 commit 資訊行
            if "|" in line:
                parts = line.split("|")
                if len(parts) >= 4:
                    current_commit = parts[0]
                    current_author_name = parts[1]
                    current_author_email = parts[2]
                    current_date = parts[3]
            else:
                # 檔案名稱
                if current_commit:
                    changes.append(
                        {
                            "commit_sha": current_commit,
                            "author_name": current_author_name,
                            "author_email": current_author_email,
                            "date": current_date,
                            "file_path": line,
                            "file_extension": self._get_file_extension(line),
                        }
                    )

        return changes

    def _get_file_extension(self, file_path: str) -> str:
        """取得檔案副檔名"""
        if "." in file_path:
            return "." + file_path.split(".")[-1]
        return ""

    # ==================== 開發者列表 ====================

    def collect_developers(
        self, since: Optional[str] = None, until: Optional[str] = None
    ) -> pd.DataFrame:
        """
        收集開發者列表（去重）

        Args:
            since: 開始日期
            until: 結束日期

        Returns:
            開發者列表 DataFrame
        """
        print(f"\n🔍 收集開發者列表...")

        # 使用 git shortlog 取得開發者列表
        args = ["shortlog", "-sne", "--all"]

        if since:
            args.append(f"--since={since}")
        if until:
            args.append(f"--until={until}")

        result = self._run_git_command(args)
        output = result.stdout

        developers = []
        for line in output.split("\n"):
            line = line.strip()
            if line:
                # 格式: "提交次數\t作者名稱 <email>"
                match = re.match(r"^\s*(\d+)\s+(.+?)\s+<(.+?)>$", line)
                if match:
                    commit_count = int(match.group(1))
                    name = match.group(2)
                    email = match.group(3)

                    # 排除 Bot 賬號
                    is_bot = any(
                        bot.lower() in name.lower() or bot.lower() in email.lower()
                        for bot in ExclusionConfig.EXCLUDED_BOTS
                    )

                    if not is_bot:
                        developers.append(
                            {
                                "name": name,
                                "email": email,
                                "commit_count": commit_count,
                            }
                        )

        # 轉換為 DataFrame
        df = pd.DataFrame(developers)

        # 按提交次數排序
        df = df.sort_values("commit_count", ascending=False)

        # 儲存到 CSV
        output_file = self.output_dir / "git_developers.csv"
        df.to_csv(output_file, index=False, encoding="utf-8-sig")
        print(f"✅ 開發者列表已儲存: {output_file} (共 {len(df)} 位)")

        return df

    # ==================== Commit Message 分析 ====================

    def analyze_commit_messages(
        self,
        author: Optional[str] = None,
        since: Optional[str] = None,
        until: Optional[str] = None,
    ) -> Dict:
        """
        分析 Commit Message 品質

        Args:
            author: 作者名稱或 Email
            since: 開始日期
            until: 結束日期

        Returns:
            分析結果字典
        """
        print(f"\n🔍 分析 Commit Message 品質...")

        # 取得所有 commit messages
        args = ["log", "--pretty=format:%s", "--no-merges"]

        if author:
            args.append(f"--author={author}")
        if since:
            args.append(f"--since={since}")
        if until:
            args.append(f"--until={until}")

        result = self._run_git_command(args)
        messages = [line.strip() for line in result.stdout.split("\n") if line.strip()]

        if not messages:
            return {"total": 0}

        # 分析規範性
        from config.analysis_config import CommitQualityConfig

        conventional_count = 0
        fix_count = 0

        for msg in messages:
            # 檢查 Conventional Commits
            if CommitQualityConfig.CONVENTIONAL_COMMIT_PATTERN.match(msg):
                conventional_count += 1

            # 檢查修復性關鍵字
            msg_lower = msg.lower()
            if any(keyword in msg_lower for keyword in CommitQualityConfig.FIX_KEYWORDS):
                fix_count += 1

        conventional_rate = conventional_count / len(messages)
        fix_rate = fix_count / len(messages)

        result = {
            "total": len(messages),
            "conventional_count": conventional_count,
            "conventional_rate": conventional_rate,
            "fix_count": fix_count,
            "fix_rate": fix_rate,
        }

        print(f"   總 Commit 數: {result['total']}")
        print(f"   符合 Conventional Commits: {conventional_count} ({conventional_rate*100:.1f}%)")
        print(f"   修復性提交: {fix_count} ({fix_rate*100:.1f}%)")

        return result

    # ==================== 時間分佈分析 ====================

    def analyze_time_distribution(
        self,
        author: Optional[str] = None,
        since: Optional[str] = None,
        until: Optional[str] = None,
    ) -> pd.DataFrame:
        """
        分析提交時間分佈

        Args:
            author: 作者名稱或 Email
            since: 開始日期
            until: 結束日期

        Returns:
            時間分佈 DataFrame
        """
        print(f"\n🔍 分析提交時間分佈...")

        # 取得 commit 時間（星期幾、小時）
        args = [
            "log",
            "--pretty=format:%ad",
            "--date=format:%A %H",  # 星期幾 小時
            "--no-merges",
        ]

        if author:
            args.append(f"--author={author}")
        if since:
            args.append(f"--since={since}")
        if until:
            args.append(f"--until={until}")

        result = self._run_git_command(args)
        times = [line.strip() for line in result.stdout.split("\n") if line.strip()]

        time_data = []
        for time_str in times:
            parts = time_str.split()
            if len(parts) >= 2:
                weekday = parts[0]
                hour = int(parts[1])
                time_data.append({"weekday": weekday, "hour": hour})

        df = pd.DataFrame(time_data)

        # 儲存到 CSV
        output_file = self.output_dir / "git_time_distribution.csv"
        df.to_csv(output_file, index=False, encoding="utf-8-sig")
        print(f"✅ 時間分佈已儲存: {output_file}")

        return df

    # ==================== 完整收集 ====================

    def collect_all(
        self,
        author: Optional[str] = None,
        since: Optional[str] = None,
        until: Optional[str] = None,
    ) -> Dict[str, pd.DataFrame]:
        """
        收集所有本地 Git 數據

        Args:
            author: 作者名稱或 Email
            since: 開始日期
            until: 結束日期

        Returns:
            包含所有數據的字典
        """
        print("=" * 60)
        print(f"開始收集本地 Git 數據: {self.repo_path}")
        print("=" * 60)

        results = {}

        # 1. 收集開發者列表
        results["developers"] = self.collect_developers(since=since, until=until)

        # 2. 收集 Commits
        results["commits"] = self.collect_commits(
            author=author, since=since, until=until
        )

        # 3. 收集檔案變更
        results["file_changes"] = self.collect_file_changes(
            author=author, since=since, until=until
        )

        # 4. 分析 Commit Messages
        results["message_analysis"] = self.analyze_commit_messages(
            author=author, since=since, until=until
        )

        # 5. 分析時間分佈
        results["time_distribution"] = self.analyze_time_distribution(
            author=author, since=since, until=until
        )

        print("\n" + "=" * 60)
        print("✅ 所有本地 Git 數據收集完成！")
        print("=" * 60)
        print(f"開發者數量: {len(results['developers'])}")
        print(f"Commits 數量: {len(results['commits'])}")
        print(f"檔案變更記錄: {len(results['file_changes'])}")
        print("=" * 60)

        return results


# 測試功能
if __name__ == "__main__":
    import sys

    print("=" * 60)
    print("Git 本地數據收集器測試")
    print("=" * 60)

    # 使用當前目錄的 Git Repository
    repo_path = os.getcwd()

    try:
        collector = GitLocalCollector(repo_path)

        # 測試：收集開發者列表
        print("\n測試 1：收集開發者列表")
        developers_df = collector.collect_developers()
        print(f"\n收集到 {len(developers_df)} 位開發者")
        if len(developers_df) > 0:
            print("\n前 5 位開發者：")
            print(developers_df.head()[["name", "email", "commit_count"]])

        print("\n" + "=" * 60)
        print("✅ 測試完成")
        print("=" * 60)

    except ValueError as e:
        print(f"❌ 錯誤: {e}")
        sys.exit(1)
    except Exception as e:
        print(f"❌ 測試失敗: {e}")
        sys.exit(1)
