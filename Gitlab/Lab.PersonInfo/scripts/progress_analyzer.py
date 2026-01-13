#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
開發者進步趨勢分析工具
對比兩個時間段的表現，評估成長情況
"""

import subprocess
import sys
from datetime import datetime

class ProgressAnalyzer:
    def __init__(self, author, period1_start, period1_end, period2_start, period2_end, repo_path):
        self.author = author
        self.period1 = (period1_start, period1_end)
        self.period2 = (period2_start, period2_end)
        self.repo_path = repo_path
        
    def run_git_command(self, cmd):
        full_cmd = f"cd {self.repo_path} && git {cmd}"
        try:
            result = subprocess.run(full_cmd, shell=True, capture_output=True, text=True)
            return result.stdout.strip()
        except Exception as e:
            return ""
    
    def get_period_stats(self, start_date, end_date):
        """獲取特定時間段的統計"""
        stats = {}
        
        # 提交次數
        commits = self.run_git_command(
            f'log --author="{self.author}" --since="{start_date}" --until="{end_date}" --oneline'
        )
        stats['commits'] = len(commits.split('\n')) if commits else 0
        
        # 程式碼變更
        numstat = self.run_git_command(
            f'log --author="{self.author}" --since="{start_date}" --until="{end_date}" --numstat --pretty=tformat:'
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
        stats['added'] = added
        stats['deleted'] = deleted
        
        # 平均變更規模
        shortstat = self.run_git_command(
            f'log --author="{self.author}" --since="{start_date}" --until="{end_date}" --shortstat'
        )
        change_sizes = []
        for line in shortstat.split('\n'):
            if 'insertion' in line or 'deletion' in line:
                import re
                insertions = re.search(r'(\d+) insertion', line)
                deletions = re.search(r'(\d+) deletion', line)
                total = 0
                if insertions:
                    total += int(insertions.group(1))
                if deletions:
                    total += int(deletions.group(1))
                if total > 0:
                    change_sizes.append(total)
        
        stats['avg_change_size'] = sum(change_sizes) / len(change_sizes) if change_sizes else 0
        stats['small_changes'] = sum(1 for size in change_sizes if size <= 100)
        stats['total_changes'] = len(change_sizes)
        
        # Commit Message 品質
        messages = self.run_git_command(
            f'log --author="{self.author}" --since="{start_date}" --until="{end_date}" --pretty=format:"%s"'
        ).split('\n')
        
        import re
        conventional_pattern = re.compile(r'^(feat|fix|docs|style|refactor|test|chore|perf|ci|build)(\(.+\))?:', re.IGNORECASE)
        stats['conventional_commits'] = sum(1 for msg in messages if conventional_pattern.match(msg))
        stats['total_messages'] = len([m for m in messages if m])
        
        # 修復率
        fix_pattern = re.compile(r'(fix|bug|hotfix|revert|修復)', re.IGNORECASE)
        stats['fix_commits'] = sum(1 for msg in messages if fix_pattern.search(msg))
        
        return stats
    
    def calculate_improvement(self):
        """計算改進幅度"""
        print(f"分析開發者: {self.author}")
        print(f"期間 1: {self.period1[0]} ~ {self.period1[1]}")
        print(f"期間 2: {self.period2[0]} ~ {self.period2[1]}")
        print("\n正在分析...")
        
        p1_stats = self.get_period_stats(*self.period1)
        p2_stats = self.get_period_stats(*self.period2)
        
        report = f"""# 開發者進步分析報告

**開發者：** {self.author}  
**報告生成時間：** {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}

---

## 📅 時間段對比

| 指標 | 期間 1<br>({self.period1[0]} ~ {self.period1[1]}) | 期間 2<br>({self.period2[0]} ~ {self.period2[1]}) | 變化 | 趨勢 |
|------|------------|------------|------|------|
"""
        
        # 計算各項指標變化
        metrics = [
            ('提交次數', 'commits', '次'),
            ('程式碼新增', 'added', '行'),
            ('程式碼刪除', 'deleted', '行'),
            ('平均變更規模', 'avg_change_size', '行/commit'),
        ]
        
        for label, key, unit in metrics:
            v1 = p1_stats.get(key, 0)
            v2 = p2_stats.get(key, 0)
            
            if v1 == 0:
                change = "N/A"
                trend = "➖"
            else:
                change_pct = ((v2 - v1) / v1) * 100
                change = f"{change_pct:+.1f}%"
                
                if abs(change_pct) < 5:
                    trend = "➖ 持平"
                elif change_pct > 0:
                    trend = "📈 上升"
                else:
                    trend = "📉 下降"
            
            if isinstance(v1, float):
                v1_str = f"{v1:.1f}"
                v2_str = f"{v2:.1f}"
            else:
                v1_str = f"{v1:,}"
                v2_str = f"{v2:,}"
                
            report += f"| {label} | {v1_str} {unit} | {v2_str} {unit} | {change} | {trend} |\n"
        
        # 品質指標
        report += "\n---\n\n## ✅ 程式碼品質指標\n\n"
        report += "| 指標 | 期間 1 | 期間 2 | 變化 | 評價 |\n"
        report += "|------|--------|--------|------|------|\n"
        
        # Commit 規範率
        conv_rate_p1 = (p1_stats['conventional_commits'] / p1_stats['total_messages'] * 100) if p1_stats['total_messages'] > 0 else 0
        conv_rate_p2 = (p2_stats['conventional_commits'] / p2_stats['total_messages'] * 100) if p2_stats['total_messages'] > 0 else 0
        conv_change = conv_rate_p2 - conv_rate_p1
        conv_eval = "✅ 進步" if conv_change > 5 else ("⚠️ 退步" if conv_change < -5 else "➖ 持平")
        
        report += f"| Commit 規範率 | {conv_rate_p1:.1f}% | {conv_rate_p2:.1f}% | {conv_change:+.1f}% | {conv_eval} |\n"
        
        # 小型變更佔比
        small_rate_p1 = (p1_stats['small_changes'] / p1_stats['total_changes'] * 100) if p1_stats['total_changes'] > 0 else 0
        small_rate_p2 = (p2_stats['small_changes'] / p2_stats['total_changes'] * 100) if p2_stats['total_changes'] > 0 else 0
        small_change = small_rate_p2 - small_rate_p1
        small_eval = "✅ 進步" if small_change > 5 else ("⚠️ 退步" if small_change < -5 else "➖ 持平")
        
        report += f"| 小型變更佔比 | {small_rate_p1:.1f}% | {small_rate_p2:.1f}% | {small_change:+.1f}% | {small_eval} |\n"
        
        # 修復率
        fix_rate_p1 = (p1_stats['fix_commits'] / p1_stats['total_messages'] * 100) if p1_stats['total_messages'] > 0 else 0
        fix_rate_p2 = (p2_stats['fix_commits'] / p2_stats['total_messages'] * 100) if p2_stats['total_messages'] > 0 else 0
        fix_change = fix_rate_p2 - fix_rate_p1
        fix_eval = "✅ 改善" if fix_change < -5 else ("⚠️ 增加" if fix_change > 5 else "➖ 持平")
        
        report += f"| 修復性提交率 | {fix_rate_p1:.1f}% | {fix_rate_p2:.1f}% | {fix_change:+.1f}% | {fix_eval} |\n"
        
        # 綜合評估
        report += "\n---\n\n## 🎯 綜合評估\n\n"
        
        improvements = []
        regressions = []
        
        if conv_change > 5:
            improvements.append("✅ **Commit Message 規範性提升** - 顯示對程式碼協作規範的重視")
        elif conv_change < -5:
            regressions.append("⚠️ **Commit Message 規範性下降** - 建議重新關注提交訊息品質")
        
        if small_change > 5:
            improvements.append("✅ **變更粒度改善** - 更好的模組化和提交拆分能力")
        elif small_change < -5:
            regressions.append("⚠️ **變更粒度變大** - 建議將大型變更拆分為小型提交")
        
        if fix_change < -5:
            improvements.append("✅ **Bug 修復率降低** - 表示程式碼品質提升或測試覆蓋改善")
        elif fix_change > 5:
            regressions.append("⚠️ **Bug 修復率上升** - 可能需要加強測試或 Code Review")
        
        # 活躍度變化
        commit_change_pct = ((p2_stats['commits'] - p1_stats['commits']) / p1_stats['commits'] * 100) if p1_stats['commits'] > 0 else 0
        if commit_change_pct > 20:
            improvements.append("📈 **貢獻度大幅提升** - 參與度和產出明顯增加")
        elif commit_change_pct < -20:
            regressions.append("📉 **貢獻度明顯下降** - 可能需要關注工作負載或動力")
        
        if improvements:
            report += "### 🌟 進步之處\n\n"
            for item in improvements:
                report += f"{item}\n\n"
        
        if regressions:
            report += "### 🔴 需要改進\n\n"
            for item in regressions:
                report += f"{item}\n\n"
        
        if not improvements and not regressions:
            report += "### ➖ 表現穩定\n\n整體表現保持一致，沒有明顯變化。\n\n"
        
        # 總結與建議
        report += "---\n\n## 💡 發展建議\n\n"
        
        if len(improvements) > len(regressions):
            report += "**總體評價：** 📈 **持續進步中**\n\n"
            report += "保持目前的良好趨勢，建議：\n"
            report += "1. 繼續維持已改善的良好習慣\n"
            report += "2. 分享經驗給團隊其他成員\n"
            if regressions:
                report += "3. 關注以下待改進項目\n"
        elif len(regressions) > len(improvements):
            report += "**總體評價：** ⚠️ **需要關注**\n\n"
            report += "發現一些需要改進的地方，建議：\n"
            report += "1. 重新檢視程式碼提交流程\n"
            report += "2. 加強測試覆蓋率\n"
            report += "3. 定期 Code Review 和知識分享\n"
        else:
            report += "**總體評價：** ➖ **穩定維持**\n\n"
            report += "表現穩定，建議：\n"
            report += "1. 嘗試挑戰更複雜的任務\n"
            report += "2. 學習新的技術棧擴展能力\n"
            report += "3. 參與架構設計和技術決策\n"
        
        report += "\n---\n\n"
        report += "**分析工具版本：** v1.0  \n"
        report += "**數據來源：** Git Repository\n"
        
        return report

def main():
    if len(sys.argv) < 6:
        print("用法: python3 progress_analyzer.py <author> <p1_start> <p1_end> <p2_start> <p2_end>")
        print("範例: python3 progress_analyzer.py 'yao' '2024-01-01' '2024-06-30' '2024-07-01' '2024-12-31'")
        print("\n說明:")
        print("  author   - 開發者名稱")
        print("  p1_start - 期間 1 起始日期")
        print("  p1_end   - 期間 1 結束日期")
        print("  p2_start - 期間 2 起始日期")
        print("  p2_end   - 期間 2 結束日期")
        sys.exit(1)
    
    author = sys.argv[1]
    p1_start = sys.argv[2]
    p1_end = sys.argv[3]
    p2_start = sys.argv[4]
    p2_end = sys.argv[5]
    repo_path = "/mnt/d/lab/sample.dotblog"
    
    analyzer = ProgressAnalyzer(author, p1_start, p1_end, p2_start, p2_end, repo_path)
    report = analyzer.calculate_improvement()
    
    output_file = f"./output/{author}_progress_{datetime.now().strftime('%Y%m%d_%H%M%S')}.md"
    with open(output_file, 'w', encoding='utf-8') as f:
        f.write(report)
    
    print(f"\n✅ 進步分析報告已生成: {output_file}")

if __name__ == "__main__":
    main()
