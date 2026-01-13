"""
綜合評分計算器

整合所有分析器的結果，計算最終評分。
"""

from pathlib import Path
from typing import Optional
import pandas as pd

from config.analysis_config import AnalysisWeights, GradingConfig


class ScoreCalculator:
    """綜合評分計算器"""

    def __init__(self, input_dir: Optional[str] = None, output_dir: Optional[str] = None):
        if input_dir:
            self.input_dir = Path(input_dir)
        else:
            self.input_dir = Path(__file__).parent.parent / "output" / "processed"

        if output_dir:
            self.output_dir = Path(output_dir)
        else:
            self.output_dir = Path(__file__).parent.parent / "output" / "processed"

        self.weights = AnalysisWeights()

    def calculate_final_scores(self) -> pd.DataFrame:
        """計算所有開發者的最終評分"""
        print("\n🔍 計算綜合評分...")

        # 讀取所有分析結果
        scores_data = {}

        # 1. Commit 品質 (23%)
        commit_file = self.input_dir / "commit_quality_scores.csv"
        if commit_file.exists():
            df = pd.read_csv(commit_file)
            scores_data["commit_quality"] = df.set_index("email")["commit_quality_score"]
        else:
            print("⚠️ 缺少 Commit 品質評分")

        # 2. 貢獻量 (12%)
        contribution_file = self.input_dir / "contribution_scores.csv"
        if contribution_file.exists():
            df = pd.read_csv(contribution_file)
            scores_data["contribution"] = df.set_index("email")["contribution_score"]
        else:
            print("⚠️ 缺少貢獻量評分")

        # 3. 技術廣度 (18%)
        tech_file = self.input_dir / "tech_breadth_scores.csv"
        if tech_file.exists():
            df = pd.read_csv(tech_file)
            scores_data["tech_breadth"] = df.set_index("email")["tech_breadth_score"]
        else:
            print("⚠️ 缺少技術廣度評分")

        # 讀取開發者列表
        developers_file = self.input_dir / "unified_developers.csv"
        if not developers_file.exists():
            print("❌ 找不到開發者列表")
            return pd.DataFrame()

        developers_df = pd.read_csv(developers_file)

        # 計算每位開發者的最終評分
        results = []
        for _, dev in developers_df.iterrows():
            email = dev["email"]

            # 獲取各維度分數（如果缺失則為 0）
            commit_quality = scores_data.get("commit_quality", pd.Series()).get(email, 0)
            contribution = scores_data.get("contribution", pd.Series()).get(email, 0)
            tech_breadth = scores_data.get("tech_breadth", pd.Series()).get(email, 0)

            # 簡化版本：只計算已有的維度
            # 完整版本應包含所有 7 個維度
            total_weight = 0
            total_score = 0

            if commit_quality > 0:
                total_score += commit_quality * self.weights.commit_quality
                total_weight += self.weights.commit_quality

            if contribution > 0:
                total_score += contribution * self.weights.contribution
                total_weight += self.weights.contribution

            if tech_breadth > 0:
                total_score += tech_breadth * self.weights.tech_breadth
                total_weight += self.weights.tech_breadth

            # 正規化到 10 分制
            final_score = (total_score / total_weight * 10) if total_weight > 0 else 0

            # 取得等級
            grade_title, grade_level, _ = GradingConfig.get_grade(final_score)

            results.append({
                "email": email,
                "username": dev.get("username", ""),
                "name": dev.get("name", ""),
                "commit_quality_score": commit_quality,
                "contribution_score": contribution,
                "tech_breadth_score": tech_breadth,
                "final_score": round(final_score, 2),
                "grade": grade_title,
                "grade_level": grade_level,
            })

        df = pd.DataFrame(results).sort_values("final_score", ascending=False)

        # 儲存
        output_file = self.output_dir / "final_scores.csv"
        df.to_csv(output_file, index=False, encoding="utf-8-sig")
        print(f"✅ 綜合評分已儲存: {output_file}")

        # 顯示統計
        print("\n📊 評分統計：")
        print(f"   高級工程師: {len(df[df['grade_level'] == 'senior'])} 位")
        print(f"   中級工程師: {len(df[df['grade_level'] == 'mid'])} 位")
        print(f"   初級工程師: {len(df[df['grade_level'] == 'junior'])} 位")

        return df


if __name__ == "__main__":
    calculator = ScoreCalculator()
    results = calculator.calculate_final_scores()
    if len(results) > 0:
        print("\n🏆 前 10 位開發者：")
        print(results.head(10)[["name", "final_score", "grade"]])
