#!/usr/bin/env python3
"""
GitLab 開發者分析系統主程式

使用方式：
    # 分析單一開發者
    uv run python scripts/main.py analyze --user "user@example.com" --from 2024-01-01 --to 2024-12-31

    # 批次分析所有開發者
    uv run python scripts/main.py analyze-all --from 2024-01-01 --to 2024-12-31
"""

import click
from datetime import datetime, timedelta
from pathlib import Path

# 收集器
from collectors.data_merger import DataMerger

# 分析器
from analyzers.commit_analyzer import CommitQualityAnalyzer
from analyzers.contribution_analyzer import ContributionAnalyzer
from analyzers.tech_breadth_analyzer import TechBreadthAnalyzer
from analyzers.score_calculator import ScoreCalculator


@click.group()
def cli():
    """GitLab 開發者技術水平分析系統"""
    pass


@cli.command()
@click.option("--user", "-u", required=True, help="開發者 Email 或 Username")
@click.option("--from", "start_date", type=str, default=None, help="開始日期 (YYYY-MM-DD)")
@click.option("--to", "end_date", type=str, default=None, help="結束日期 (YYYY-MM-DD)")
def analyze(user: str, start_date: str, end_date: str):
    """分析單一開發者"""
    print("=" * 60)
    print(f"分析開發者: {user}")
    print("=" * 60)

    # TODO: 實作單一開發者分析
    print("⚠️ 此功能尚未完整實作，請使用 analyze-all 命令")


@cli.command()
@click.option("--from", "start_date", type=str, default=None, help="開始日期 (YYYY-MM-DD)")
@click.option("--to", "end_date", type=str, default=None, help="結束日期 (YYYY-MM-DD)")
def analyze_all(start_date: str, end_date: str):
    """批次分析所有開發者"""
    print("=" * 60)
    print("GitLab 開發者分析系統")
    print("=" * 60)

    if start_date:
        print(f"時間範圍: {start_date} ~ {end_date or '現在'}")

    # 步驟 1: 數據合併與清洗
    print("\n📦 步驟 1: 數據合併與清洗")
    merger = DataMerger()
    merger.process_all()

    # 步驟 2: 執行各項分析
    print("\n📊 步驟 2: 執行分析")

    print("\n2.1 Commit 品質分析 (23% 權重)")
    commit_analyzer = CommitQualityAnalyzer()
    commit_analyzer.analyze_all_developers()

    print("\n2.2 程式碼貢獻量分析 (12% 權重)")
    contribution_analyzer = ContributionAnalyzer()
    contribution_analyzer.analyze_all_developers()

    print("\n2.3 技術廣度分析 (18% 權重)")
    tech_analyzer = TechBreadthAnalyzer()
    tech_analyzer.analyze_all_developers()

    # 步驟 3: 計算綜合評分
    print("\n🎯 步驟 3: 計算綜合評分")
    calculator = ScoreCalculator()
    results = calculator.calculate_final_scores()

    # 顯示結果
    print("\n" + "=" * 60)
    print("✅ 分析完成！")
    print("=" * 60)

    if len(results) > 0:
        print("\n🏆 前 10 位開發者：")
        print(results.head(10)[["name", "email", "final_score", "grade"]].to_string(index=False))

        output_dir = Path(__file__).parent / "output" / "processed"
        print(f"\n📁 詳細報告已儲存至: {output_dir}")
        print(f"   - final_scores.csv")
        print(f"   - commit_quality_scores.csv")
        print(f"   - contribution_scores.csv")
        print(f"   - tech_breadth_scores.csv")


@cli.command()
@click.option("--from", "start_date", type=str, default=None, help="開始日期 (YYYY-MM-DD)")
@click.option("--to", "end_date", type=str, default=None, help="結束日期 (YYYY-MM-DD)")
def team_report(start_date: str, end_date: str):
    """產生團隊匯總報告"""
    print("⚠️ 此功能尚未實作")


if __name__ == "__main__":
    cli()
