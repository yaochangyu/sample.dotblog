"""
技術廣度分析器

評估開發者涉及的語言種類、技術棧覆蓋範圍。
權重：18%
"""

from pathlib import Path
from typing import Optional, Dict, Set
import pandas as pd

from config.analysis_config import TechBreadthConfig


class TechBreadthAnalyzer:
    """技術廣度分析器"""

    def __init__(self, input_dir: Optional[str] = None, output_dir: Optional[str] = None):
        if input_dir:
            self.input_dir = Path(input_dir)
        else:
            self.input_dir = Path(__file__).parent.parent / "output" / "processed"

        if output_dir:
            self.output_dir = Path(output_dir)
        else:
            self.output_dir = Path(__file__).parent.parent / "output" / "processed"

    def analyze_developer(self, email: str, file_changes_df: pd.DataFrame) -> Dict:
        """分析單個開發者的技術廣度"""
        dev_files = file_changes_df[file_changes_df["author_email"].str.lower() == email.lower()]

        if len(dev_files) == 0:
            return self._empty_result(email)

        # 取得所有檔案副檔名
        extensions = dev_files["file_extension"].unique()
        extensions = [ext for ext in extensions if ext]  # 移除空值

        # 分類到技術棧
        tech_stacks = self._classify_tech_stacks(extensions)

        # 計算技術種類數量
        tech_count = len(tech_stacks)

        # 評分
        if tech_count >= TechBreadthConfig.TECH_STACK_THRESHOLDS["excellent"]:
            score = 10
        elif tech_count >= TechBreadthConfig.TECH_STACK_THRESHOLDS["fullstack"]:
            score = 8
        else:
            score = 6

        # 判斷開發者類型
        dev_type = self._determine_developer_type(tech_stacks)

        return {
            "email": email,
            "tech_count": tech_count,
            "tech_stacks": ",".join(sorted(tech_stacks)),
            "extensions": ",".join(sorted(extensions)),
            "developer_type": dev_type,
            "tech_breadth_score": score,
        }

    def _classify_tech_stacks(self, extensions: list) -> Set[str]:
        """將副檔名分類到技術棧"""
        tech_stacks = set()

        for ext in extensions:
            for category, exts in TechBreadthConfig.FILE_TYPE_CATEGORIES.items():
                if ext in exts:
                    tech_stacks.add(category)
                    break

        return tech_stacks

    def _determine_developer_type(self, tech_stacks: Set[str]) -> str:
        """判斷開發者類型"""
        if "frontend" in tech_stacks and "backend" in tech_stacks:
            return "全棧開發者"
        elif "devops" in tech_stacks:
            return "DevOps/SRE"
        elif "frontend" in tech_stacks:
            return "前端開發者"
        elif "backend" in tech_stacks:
            return "後端開發者"
        else:
            return "一般開發者"

    def _empty_result(self, email: str) -> Dict:
        return {
            "email": email,
            "tech_count": 0,
            "tech_stacks": "",
            "extensions": "",
            "developer_type": "未知",
            "tech_breadth_score": 0,
        }

    def analyze_all_developers(self) -> pd.DataFrame:
        """分析所有開發者的技術廣度"""
        print("\n🔍 分析技術廣度...")

        file_changes_file = self.input_dir / "file_changes_cleaned.csv"
        developers_file = self.input_dir / "unified_developers.csv"

        if not all([file_changes_file.exists(), developers_file.exists()]):
            print("❌ 缺少必要的數據檔案")
            return pd.DataFrame()

        file_changes_df = pd.read_csv(file_changes_file)
        developers_df = pd.read_csv(developers_file)

        results = []
        for _, dev in developers_df.iterrows():
            result = self.analyze_developer(dev["email"], file_changes_df)
            result["username"] = dev.get("username", "")
            result["name"] = dev.get("name", "")
            results.append(result)

        df = pd.DataFrame(results).sort_values("tech_breadth_score", ascending=False)

        output_file = self.output_dir / "tech_breadth_scores.csv"
        df.to_csv(output_file, index=False, encoding="utf-8-sig")
        print(f"✅ 技術廣度評分已儲存: {output_file}")

        return df


if __name__ == "__main__":
    analyzer = TechBreadthAnalyzer()
    results = analyzer.analyze_all_developers()
    if len(results) > 0:
        print("\n前 5 位開發者技術廣度：")
        print(results.head()[["name", "tech_count", "developer_type", "tech_breadth_score"]])
