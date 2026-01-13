#!/usr/bin/env python3
"""
GitLab 數據收集腳本

使用方式：
    # 收集所有數據（過去一年）
    uv run python scripts/collect_data.py

    # 收集指定時間範圍的數據
    uv run python scripts/collect_data.py --from 2024-01-01 --to 2024-12-31

    # 只收集特定專案的數據
    uv run python scripts/collect_data.py --projects 12345,67890

    # 只收集 MR 數據
    uv run python scripts/collect_data.py --only-mr
"""

import click
from datetime import datetime, timedelta
from collectors.gitlab_api_collector import GitLabAPICollector


@click.command()
@click.option(
    "--from",
    "start_date",
    type=str,
    default=None,
    help="開始日期 (格式: YYYY-MM-DD)，預設為一年前",
)
@click.option(
    "--to",
    "end_date",
    type=str,
    default=None,
    help="結束日期 (格式: YYYY-MM-DD)，預設為今天",
)
@click.option(
    "--projects",
    type=str,
    default=None,
    help="專案 ID 列表（逗號分隔），例如: 12345,67890",
)
@click.option(
    "--only-projects", is_flag=True, help="只收集專案列表"
)
@click.option(
    "--only-mr", is_flag=True, help="只收集 Merge Request 數據"
)
@click.option(
    "--only-comments", is_flag=True, help="只收集 Review Comments 數據"
)
@click.option(
    "--only-commits", is_flag=True, help="只收集 Commit 數據"
)
def main(
    start_date: str,
    end_date: str,
    projects: str,
    only_projects: bool,
    only_mr: bool,
    only_comments: bool,
    only_commits: bool,
):
    """GitLab 數據收集主程式"""

    # 處理日期
    if start_date is None:
        start_date = (datetime.now() - timedelta(days=365)).strftime("%Y-%m-%d")
    if end_date is None:
        end_date = datetime.now().strftime("%Y-%m-%d")

    # 處理專案 ID
    project_ids = None
    if projects:
        project_ids = [int(p.strip()) for p in projects.split(",")]

    print(f"📅 時間範圍: {start_date} 至 {end_date}")
    if project_ids:
        print(f"📦 指定專案: {project_ids}")
    else:
        print(f"📦 範圍: 所有可訪問的專案")

    # 建立收集器
    collector = GitLabAPICollector()

    # 根據選項收集數據
    if only_projects:
        collector.collect_projects()
    elif only_mr:
        collector.collect_merge_requests(
            project_ids=project_ids, start_date=start_date, end_date=end_date
        )
    elif only_comments:
        collector.collect_review_comments(
            project_ids=project_ids, start_date=start_date, end_date=end_date
        )
    elif only_commits:
        collector.collect_commits(
            project_ids=project_ids, start_date=start_date, end_date=end_date
        )
    else:
        # 收集所有數據
        collector.collect_all(
            project_ids=project_ids, start_date=start_date, end_date=end_date
        )


if __name__ == "__main__":
    main()
