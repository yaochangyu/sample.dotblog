#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Git 開發者技術水平分析工具
生成詳細的 Markdown 評估報告
"""

import subprocess
import re
from datetime import datetime
from collections import Counter, defaultdict
import json
import sys

class DeveloperAnalyzer:
    def __init__(self, author, start_date, end_date, repo_path):
        self.author = author
        self.start_date = start_date
        self.end_date = end_date
        self.repo_path = repo_path
        self.stats = {}
        
    def run_git_command(self, cmd):
        """執行 Git 命令並返回輸出"""
        full_cmd = f"cd {self.repo_path} && git {cmd}"
        try:
            result = subprocess.run(full_cmd, shell=True, capture_output=True, text=True)
            return result.stdout.strip()
        except Exception as e:
            return f"Error: {e}"
    
    def get_basic_stats(self):
        """1. 基礎統計"""
        # 總提交次數
        commits = self.run_git_command(
            f'log --author="{self.author}" --since="{self.start_date}" --until="{self.end_date}" --oneline'
        )
        total_commits = len(commits.split('\n')) if commits else 0
        
        # 程式碼變更統計
        numstat = self.run_git_command(
            f'log --author="{self.author}" --since="{self.start_date}" --until="{self.end_date}" --numstat --pretty=tformat:'
        )
        
        added, deleted = 0, 0
        for line in numstat.split('\n'):
            parts = line.split('\t')
            if len(parts) >= 2:
                try:
                    added += int(parts[0]) if parts[0] != '-' else 0
                    deleted += int(parts[1]) if parts[1] != '-' else 0
                except ValueError:
                    pass
        
        # 活躍天數
        dates = self.run_git_command(
            f'log --author="{self.author}" --since="{self.start_date}" --until="{self.end_date}" --date=short --format="%ad"'
        )
        active_days = len(set(dates.split('\n'))) if dates else 0
        
        # 涉及檔案數
        files = self.run_git_command(
            f'log --author="{self.author}" --since="{self.start_date}" --until="{self.end_date}" --name-only --pretty=format:'
        )
        unique_files = len(set([f for f in files.split('\n') if f])) if files else 0
        
        self.stats['basic'] = {
            'total_commits': total_commits,
            'lines_added': added,
            'lines_deleted': deleted,
            'net_lines': added - deleted,
            'active_days': active_days,
            'unique_files': unique_files,
            'avg_commits_per_day': round(total_commits / active_days, 2) if active_days > 0 else 0
        }
        
    def analyze_commit_quality(self):
        """2. Commit 品質分析"""
        # 獲取所有 commit messages
        messages = self.run_git_command(
            f'log --author="{self.author}" --since="{self.start_date}" --until="{self.end_date}" --pretty=format:"%s"'
        ).split('\n')
        
        # 分析 message 規範
        conventional_pattern = re.compile(r'^(feat|fix|docs|style|refactor|test|chore|perf|ci|build)(\(.+\))?:', re.IGNORECASE)
        conventional_count = sum(1 for msg in messages if conventional_pattern.match(msg))
        
        # Message 長度
        msg_lengths = [len(msg) for msg in messages if msg]
        avg_msg_length = sum(msg_lengths) / len(msg_lengths) if msg_lengths else 0
        
        # 分析變更規模
        shortstat = self.run_git_command(
            f'log --author="{self.author}" --since="{self.start_date}" --until="{self.end_date}" --shortstat --oneline'
        )
        
        change_sizes = []
        for line in shortstat.split('\n'):
            if 'insertion' in line or 'deletion' in line:
                insertions = re.search(r'(\d+) insertion', line)
                deletions = re.search(r'(\d+) deletion', line)
                total = 0
                if insertions:
                    total += int(insertions.group(1))
                if deletions:
                    total += int(deletions.group(1))
                if total > 0:
                    change_sizes.append(total)
        
        small_changes = sum(1 for size in change_sizes if size <= 100)
        medium_changes = sum(1 for size in change_sizes if 100 < size <= 500)
        large_changes = sum(1 for size in change_sizes if size > 500)
        
        # 修復性提交
        fix_pattern = re.compile(r'(fix|bug|hotfix|revert|修復)', re.IGNORECASE)
        fix_count = sum(1 for msg in messages if fix_pattern.search(msg))
        
        self.stats['quality'] = {
            'total_messages': len([m for m in messages if m]),
            'conventional_commits': conventional_count,
            'conventional_rate': round(conventional_count / len(messages) * 100, 1) if messages else 0,
            'avg_msg_length': round(avg_msg_length, 1),
            'small_changes': small_changes,
            'medium_changes': medium_changes,
            'large_changes': large_changes,
            'small_change_rate': round(small_changes / len(change_sizes) * 100, 1) if change_sizes else 0,
            'fix_commits': fix_count,
            'fix_rate': round(fix_count / len(messages) * 100, 1) if messages else 0
        }
        
    def analyze_tech_stack(self):
        """3. 技術棧分析"""
        # 檔案類型統計
        files = self.run_git_command(
            f'log --author="{self.author}" --since="{self.start_date}" --until="{self.end_date}" --name-only --pretty=format:'
        ).split('\n')
        
        extensions = []
        directories = []
        
        for filepath in files:
            if filepath:
                # 檔案類型
                if '.' in filepath:
                    ext = filepath.split('.')[-1]
                    extensions.append(ext)
                
                # 目錄
                if '/' in filepath:
                    directory = filepath.split('/')[0]
                    directories.append(directory)
        
        ext_counter = Counter(extensions)
        dir_counter = Counter(directories)
        
        # 技術分類
        tech_categories = {
            'Frontend': ['js', 'ts', 'jsx', 'tsx', 'vue', 'html', 'css', 'scss', 'sass', 'less'],
            'Backend': ['java', 'py', 'go', 'cs', 'rb', 'php', 'kt', 'rs'],
            'DevOps': ['yml', 'yaml', 'sh', 'bash', 'tf', 'Dockerfile'],
            'Database': ['sql', 'prisma', 'migration'],
            'Config': ['json', 'xml', 'toml', 'ini', 'env'],
            'Documentation': ['md', 'rst', 'txt', 'pdf']
        }
        
        tech_distribution = defaultdict(int)
        for ext, count in ext_counter.items():
            categorized = False
            for category, exts in tech_categories.items():
                if ext in exts:
                    tech_distribution[category] += count
                    categorized = True
                    break
            if not categorized:
                tech_distribution['Other'] += count
        
        self.stats['tech_stack'] = {
            'top_extensions': dict(ext_counter.most_common(10)),
            'top_directories': dict(dir_counter.most_common(10)),
            'tech_distribution': dict(tech_distribution),
            'language_diversity': len(ext_counter)
        }
        
    def analyze_work_pattern(self):
        """4. 工作模式分析"""
        # 星期分佈
        weekdays = self.run_git_command(
            f'log --author="{self.author}" --since="{self.start_date}" --until="{self.end_date}" --date=format:"%A" --pretty=format:"%ad"'
        ).split('\n')
        weekday_counter = Counter(weekdays)
        
        # 小時分佈
        hours = self.run_git_command(
            f'log --author="{self.author}" --since="{self.start_date}" --until="{self.end_date}" --date=format:"%H" --pretty=format:"%ad"'
        ).split('\n')
        hour_counter = Counter(hours)
        
        # 工作時間分析
        work_hours = sum(hour_counter.get(str(h).zfill(2), 0) for h in range(9, 18))
        total_hours = sum(hour_counter.values())
        
        self.stats['work_pattern'] = {
            'weekday_distribution': dict(weekday_counter),
            'hour_distribution': dict(sorted(hour_counter.items())),
            'work_hours_rate': round(work_hours / total_hours * 100, 1) if total_hours > 0 else 0
        }
        
    def calculate_score(self):
        """計算綜合評分"""
        scores = {}
        
        # 1. 貢獻量得分 (15%)
        commits = self.stats['basic']['total_commits']
        if commits > 200:
            scores['contribution'] = 10
        elif commits > 100:
            scores['contribution'] = 8
        elif commits > 50:
            scores['contribution'] = 6
        else:
            scores['contribution'] = 4
            
        # 2. Commit 品質得分 (25%)
        quality = self.stats['quality']
        quality_score = 0
        quality_score += (quality['conventional_rate'] / 10)  # 最高 10 分
        quality_score += (quality['small_change_rate'] / 10)  # 最高 10 分
        quality_score -= (quality['fix_rate'] / 10)  # 扣分
        scores['quality'] = max(0, min(10, quality_score))
        
        # 3. 技術廣度得分 (20%)
        diversity = self.stats['tech_stack']['language_diversity']
        if diversity > 10:
            scores['tech_breadth'] = 10
        elif diversity > 5:
            scores['tech_breadth'] = 8
        elif diversity > 3:
            scores['tech_breadth'] = 6
        else:
            scores['tech_breadth'] = 4
            
        # 4. 工作模式得分 (10%)
        work_rate = self.stats['work_pattern']['work_hours_rate']
        scores['work_pattern'] = min(10, work_rate / 10)
        
        # 總分計算
        total_score = (
            scores['contribution'] * 0.15 +
            scores['quality'] * 0.25 +
            scores['tech_breadth'] * 0.20 +
            scores['work_pattern'] * 0.10 +
            7.0 * 0.30  # 其他維度預設 7 分
        )
        
        self.stats['scores'] = {
            'detail': scores,
            'total': round(total_score, 1)
        }
        
    def generate_report(self):
        """生成 Markdown 報告"""
        self.get_basic_stats()
        self.analyze_commit_quality()
        self.analyze_tech_stack()
        self.analyze_work_pattern()
        self.calculate_score()
        
        report = f"""# 開發者技術評估報告

**開發者：** {self.author}  
**評估期間：** {self.start_date} ~ {self.end_date}  
**報告生成時間：** {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}  
**綜合評分：** {self.stats['scores']['total']}/10

---

## 📊 一、貢獻統計

| 指標 | 數值 |
|------|------|
| 總提交次數 | {self.stats['basic']['total_commits']} 次 |
| 程式碼新增 | +{self.stats['basic']['lines_added']:,} 行 |
| 程式碼刪除 | -{self.stats['basic']['lines_deleted']:,} 行 |
| 淨變更 | {self.stats['basic']['net_lines']:+,} 行 |
| 活躍天數 | {self.stats['basic']['active_days']} 天 |
| 涉及檔案數 | {self.stats['basic']['unique_files']} 個 |
| 平均提交頻率 | {self.stats['basic']['avg_commits_per_day']} 次/天 |

**評分：** {self.stats['scores']['detail']['contribution']}/10

---

## ✅ 二、Commit 品質分析

### 2.1 Message 規範
- **總 Commits：** {self.stats['quality']['total_messages']} 個
- **符合規範：** {self.stats['quality']['conventional_commits']} 個 ({self.stats['quality']['conventional_rate']}%)
- **平均長度：** {self.stats['quality']['avg_msg_length']} 字元

### 2.2 變更粒度分佈
| 規模 | 數量 | 佔比 |
|------|------|------|
| 小型 (≤100行) | {self.stats['quality']['small_changes']} | {self.stats['quality']['small_change_rate']}% |
| 中型 (100-500行) | {self.stats['quality']['medium_changes']} | - |
| 大型 (>500行) | {self.stats['quality']['large_changes']} | - |

### 2.3 修復性提交
- **修復相關：** {self.stats['quality']['fix_commits']} 次 ({self.stats['quality']['fix_rate']}%)

**評分：** {self.stats['scores']['detail']['quality']:.1f}/10

---

## 🔧 三、技術棧分析

### 3.1 檔案類型分佈 (Top 10)
"""
        
        # 檔案類型表格
        for ext, count in list(self.stats['tech_stack']['top_extensions'].items())[:10]:
            report += f"- `.{ext}`: {count} 次\n"
        
        report += f"\n### 3.2 技術領域分佈\n"
        for category, count in sorted(self.stats['tech_stack']['tech_distribution'].items(), key=lambda x: x[1], reverse=True):
            total = sum(self.stats['tech_stack']['tech_distribution'].values())
            percentage = round(count / total * 100, 1) if total > 0 else 0
            report += f"- **{category}:** {percentage}% ({count} 個檔案)\n"
        
        report += f"\n### 3.3 主要工作目錄 (Top 5)\n"
        for directory, count in list(self.stats['tech_stack']['top_directories'].items())[:5]:
            report += f"- `{directory}/`: {count} 次\n"
        
        report += f"""
**語言多樣性：** {self.stats['tech_stack']['language_diversity']} 種  
**評分：** {self.stats['scores']['detail']['tech_breadth']}/10

---

## ⏰ 四、工作模式分析

### 4.1 每週提交分佈
"""
        
        # 星期分佈
        for day in ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday']:
            count = self.stats['work_pattern']['weekday_distribution'].get(day, 0)
            bar = '█' * (count // 5) if count > 0 else ''
            report += f"- {day}: {count} {bar}\n"
        
        report += f"\n### 4.2 工作時間分析\n"
        report += f"- **工作時段 (9-18點) 提交率：** {self.stats['work_pattern']['work_hours_rate']}%\n"
        
        report += f"\n**評分：** {self.stats['scores']['detail']['work_pattern']:.1f}/10\n"
        
        report += f"""
---

## 🎯 五、綜合評估

### 評分明細
| 維度 | 得分 | 權重 | 加權得分 |
|------|------|------|----------|
| 程式碼貢獻量 | {self.stats['scores']['detail']['contribution']}/10 | 15% | {self.stats['scores']['detail']['contribution'] * 0.15:.2f} |
| Commit 品質 | {self.stats['scores']['detail']['quality']:.1f}/10 | 25% | {self.stats['scores']['detail']['quality'] * 0.25:.2f} |
| 技術廣度 | {self.stats['scores']['detail']['tech_breadth']}/10 | 20% | {self.stats['scores']['detail']['tech_breadth'] * 0.20:.2f} |
| 工作模式 | {self.stats['scores']['detail']['work_pattern']:.1f}/10 | 10% | {self.stats['scores']['detail']['work_pattern'] * 0.10:.2f} |
| 其他維度* | 7.0/10 | 30% | 2.10 |
| **總分** | | **100%** | **{self.stats['scores']['total']}/10** |

*其他維度（協作能力、進步趨勢）需人工評估

### 技術等級判定
"""
        
        total = self.stats['scores']['total']
        if total >= 8:
            level = "🏆 **高級工程師**"
            desc = "具備優秀的程式碼品質意識，技術廣度足夠，工作模式專業。"
        elif total >= 5:
            level = "⭐ **中級工程師**"
            desc = "程式碼貢獻穩定，具備一定技術能力，仍有提升空間。"
        else:
            level = "🌱 **初級工程師**"
            desc = "處於成長階段，需加強程式碼規範與技術深度。"
        
        report += f"{level}\n\n{desc}\n"
        
        report += """
---

## 💡 六、改進建議

"""
        
        # 智能建議
        suggestions = []
        
        if self.stats['quality']['conventional_rate'] < 60:
            suggestions.append("1. **提升 Commit Message 規範**：建議採用 Conventional Commits 格式 (feat/fix/docs 等)")
        
        if self.stats['quality']['small_change_rate'] < 50:
            suggestions.append("2. **優化變更粒度**：建議將大型 commit 拆分為多個小型 commit，提升可讀性")
        
        if self.stats['quality']['fix_rate'] > 30:
            suggestions.append("3. **減少修復性提交**：加強測試覆蓋率，降低 bug 修復頻率")
        
        if self.stats['tech_stack']['language_diversity'] < 3:
            suggestions.append("4. **擴展技術棧**：建議學習更多技術領域，提升全棧能力")
        
        if self.stats['work_pattern']['work_hours_rate'] < 60:
            suggestions.append("5. **優化工作時間**：非工作時段提交較多，建議調整工作節奏")
        
        if not suggestions:
            suggestions.append("✅ 目前表現優秀，繼續保持！")
        
        for suggestion in suggestions:
            report += f"{suggestion}\n\n"
        
        report += """
---

## 📌 附註

- 本報告基於 Git 提交記錄自動生成，僅供參考
- 無法評估：程式碼邏輯品質、演算法效率、安全意識等
- 建議結合 Code Review、效能測試等其他評估方式

**分析工具版本：** v1.0  
**數據來源：** Git Repository
"""
        
        return report

def main():
    if len(sys.argv) < 2:
        print("用法: python3 developer_analyzer.py <author> [start_date] [end_date]")
        print("範例: python3 developer_analyzer.py 'yaochangyu' '2024-01-01' '2024-12-31'")
        sys.exit(1)
    
    author = sys.argv[1]
    start_date = sys.argv[2] if len(sys.argv) > 2 else "2020-01-01"
    end_date = sys.argv[3] if len(sys.argv) > 3 else "2026-12-31"
    repo_path = "/mnt/d/lab/sample.dotblog"
    
    print(f"正在分析開發者: {author}")
    print(f"時間範圍: {start_date} ~ {end_date}")
    print("分析中...\n")
    
    analyzer = DeveloperAnalyzer(author, start_date, end_date, repo_path)
    report = analyzer.generate_report()
    
    # 輸出到檔案
    output_file = f"./output/{author}_{datetime.now().strftime('%Y%m%d_%H%M%S')}.md"
    with open(output_file, 'w', encoding='utf-8') as f:
        f.write(report)
    
    print(f"✅ 報告已生成: {output_file}")
    print(f"📊 綜合評分: {analyzer.stats['scores']['total']}/10")

if __name__ == "__main__":
    main()
