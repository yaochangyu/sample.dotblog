"""
GitLab 資料收集基礎類別 - 提供共用功能
"""

import pandas as pd
from datetime import datetime
from typing import List, Any, Optional
import os
from tqdm import tqdm
import config
from gitlab_client import GitLabClient


class BaseGitLabCollector:
    """GitLab 資料收集基礎類別，提供共用的初始化和基本方法"""
    
    def __init__(
        self, 
        start_date: Optional[str] = None, 
        end_date: Optional[str] = None,
        project_ids: Optional[List[int]] = None,
        group_id: Optional[int] = None
    ):
        """
        初始化 GitLab 連線
        
        Args:
            start_date: 起始日期 (格式: YYYY-MM-DD)，預設使用 config.START_DATE
            end_date: 結束日期 (格式: YYYY-MM-DD)，預設使用 config.END_DATE
            project_ids: 指定專案 ID 列表，預設使用 config.TARGET_PROJECT_IDS
            group_id: 指定群組 ID，預設使用 config.TARGET_GROUP_ID
        """
        self.client = GitLabClient(config.GITLAB_URL, config.GITLAB_TOKEN, ssl_verify=False)
        self.start_date = datetime.strptime(start_date or config.START_DATE, "%Y-%m-%d")
        self.end_date = datetime.strptime(end_date or config.END_DATE, "%Y-%m-%d")
        self.target_project_ids = project_ids if project_ids is not None else config.TARGET_PROJECT_IDS
        self.target_group_id = group_id if group_id is not None else config.TARGET_GROUP_ID
        
        # 確保輸出目錄存在
        os.makedirs(config.OUTPUT_DIR, exist_ok=True)
    
    def get_all_projects(self) -> List[Any]:
        """
        取得所有專案
        
        Returns:
            專案列表
        """
        print("正在取得專案列表...")
        projects = self.client.get_projects(
            group_id=self.target_group_id,
            project_ids=self.target_project_ids
        )
        print(f"找到 {len(projects)} 個專案")
        return projects
    
    def save_dataframe(self, df: pd.DataFrame, filename: str) -> str:
        """
        儲存 DataFrame 到 CSV 檔案
        
        Args:
            df: 要儲存的 DataFrame
            filename: 檔案名稱
            
        Returns:
            完整檔案路徑
        """
        output_file = os.path.join(config.OUTPUT_DIR, filename)
        df.to_csv(output_file, index=False, encoding='utf-8-sig')
        return output_file
    
    def get_date_range_str(self) -> str:
        """
        取得日期範圍字串
        
        Returns:
            格式化的日期範圍字串
        """
        return f"{self.start_date.strftime('%Y-%m-%d')} 至 {self.end_date.strftime('%Y-%m-%d')}"
