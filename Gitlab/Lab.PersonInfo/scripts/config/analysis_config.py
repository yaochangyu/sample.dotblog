"""
分析參數配置模組

根據 analysis-spec.md 定義的評估標準，提供所有分析參數的配置。
"""

from typing import Dict, List, Tuple
from dataclasses import dataclass
import re


# ==================== 評估維度權重 ====================

@dataclass
class AnalysisWeights:
    """評估維度權重配置（總和必須為 1.0）"""

    contribution: float = 0.12  # 程式碼貢獻量 (12%)
    commit_quality: float = 0.23  # Commit 品質 (23%) - 最高權重
    tech_breadth: float = 0.18  # 技術廣度 (18%)
    collaboration: float = 0.12  # 協作能力 (12%)
    code_review: float = 0.10  # Code Review 品質 (10%)
    work_pattern: float = 0.10  # 工作模式 (10%)
    progress: float = 0.15  # 進步趨勢 (15%)

    def validate(self) -> bool:
        """驗證權重總和是否為 1.0"""
        total = (
            self.contribution
            + self.commit_quality
            + self.tech_breadth
            + self.collaboration
            + self.code_review
            + self.work_pattern
            + self.progress
        )
        return abs(total - 1.0) < 0.001  # 允許浮點數誤差

    def to_dict(self) -> Dict[str, float]:
        """轉換為字典格式"""
        return {
            "contribution": self.contribution,
            "commit_quality": self.commit_quality,
            "tech_breadth": self.tech_breadth,
            "collaboration": self.collaboration,
            "code_review": self.code_review,
            "work_pattern": self.work_pattern,
            "progress": self.progress,
        }


# 預設權重配置
WEIGHTS = AnalysisWeights()


# ==================== Commit 品質評分標準 ====================

class CommitQualityConfig:
    """Commit 品質評分配置"""

    # A. Message 規範性（Conventional Commits）
    CONVENTIONAL_COMMIT_PATTERN = re.compile(
        r"^(feat|fix|docs|refactor|test|chore|style|perf|ci|build|revert)"
        r"(\(.+\))?: .+"
    )

    # Message 規範率評分標準
    MESSAGE_QUALITY_THRESHOLDS = {
        "excellent": 0.80,  # >80% 符合規範：優秀 (9-10分)
        "good": 0.40,  # 40-80%：中等 (5-8分)
        # <40%：需改進 (1-4分)
    }

    # B. 變更粒度（單次 commit 大小）
    CHANGE_SIZE_SMALL = 100  # ≤100 行：小型變更
    CHANGE_SIZE_MEDIUM = 500  # 100-500 行：中型變更
    # >500 行：大型變更

    # 變更粒度評分標準（小型變更佔比）
    CHANGE_SIZE_THRESHOLDS = {
        "excellent": 0.60,  # >60% 小型變更：優秀 (9-10分)
        "good": 0.40,  # 40-60%：良好 (6-8分)
        # <40%：需改進 (1-5分)
    }

    # C. 修復性提交比例
    FIX_KEYWORDS = ["fix", "bug", "hotfix", "revert", "修復", "修正"]

    FIX_RATE_THRESHOLDS = {
        "excellent": 0.15,  # <15%：優秀
        "good": 0.30,  # 15-30%：正常
        # >30%：需改進
    }


# ==================== Code Review 品質評分標準 ====================

class CodeReviewConfig:
    """Code Review 品質評分配置"""

    # A. Review 參與度評分標準（與團隊平均比較）
    PARTICIPATION_THRESHOLDS = {
        "excellent": 1.5,  # >團隊平均 1.5 倍：優秀 (9-10分)
        "good": 1.0,  # =團隊平均：良好 (7-8分)
        "fair": 0.5,  # 50-100% 團隊平均：中等 (5-6分)
        # <50%：需改進 (1-4分)
    }

    # B. Review 深度評分標準
    # LGTM-only 關鍵字（表示無實質建議）
    LGTM_KEYWORDS = [
        "lgtm",
        "looks good",
        "看起來不錯",
        "沒問題",
        "👍",
        "✅",
        "approve",
        "approved",
    ]

    # 有建議的 Review 比例評分標準
    DEPTH_THRESHOLDS = {
        "excellent": 0.80,  # >80% 有具體建議：優秀 (9-10分)
        "good": 0.50,  # 50-80%：中等 (5-8分)
        # <50%：需改進 (1-4分)
    }

    # 發現問題的嚴重等級分數
    ISSUE_SEVERITY_SCORES = {
        "critical": 5,  # Critical Issues（安全漏洞、資料遺失）
        "major": 3,  # Major Issues（效能問題、邏輯錯誤）
        "minor": 1,  # Minor Issues（程式碼風格、命名）
    }

    # Critical Issues 關鍵字
    CRITICAL_KEYWORDS = [
        "sql injection",
        "xss",
        "cross-site scripting",
        "security",
        "vulnerability",
        "data loss",
        "資料遺失",
        "安全漏洞",
    ]

    # Major Issues 關鍵字
    MAJOR_KEYWORDS = [
        "n+1",
        "performance",
        "memory leak",
        "race condition",
        "deadlock",
        "logic error",
        "邏輯錯誤",
        "效能問題",
        "記憶體洩漏",
    ]

    # C. Review 時效性評分標準（首次回應時間，單位：小時）
    TIMELINESS_THRESHOLDS = {
        "excellent": 4,  # <4 小時：優秀 (9-10分)
        "good": 24,  # 4-24 小時：良好 (7-8分)
        "fair": 72,  # 24-72 小時：普通 (5-6分)
        # >72 小時：阻礙開發 (1-4分)
    }

    # D. 被 Review 接受度評分標準
    REQUEST_CHANGES_THRESHOLDS = {
        "excellent": 0.15,  # <15%：優秀 (9-10分)
        "good": 0.30,  # 15-30%：正常 (7-8分)
        # >30%：需改進 (1-6分)
    }


# ==================== 程式碼貢獻量評分標準 ====================

class ContributionConfig:
    """程式碼貢獻量評分配置"""

    # 提交次數評分標準
    COMMIT_COUNT_THRESHOLDS = {
        "high": 200,  # 200+ 次：高活躍度 (10分)
        "stable": 100,  # 100-200 次：穩定貢獻 (8分)
        "medium": 50,  # 50-100 次：中等參與 (6分)
        # <50 次：參與度低 (4分)
    }


# ==================== 技術廣度評分標準 ====================

class TechBreadthConfig:
    """技術廣度評分配置"""

    # 技術棧評分標準（不同語言/技術數量）
    TECH_STACK_THRESHOLDS = {
        "excellent": 5,  # 5+ 種：技術廣度優秀 (10分)
        "fullstack": 3,  # 3-5 種：全棧能力 (8分)
        # 1-2 種：專精型 (6分)
    }

    # 檔案類型分類
    FILE_TYPE_CATEGORIES = {
        "frontend": [
            ".js",
            ".ts",
            ".jsx",
            ".tsx",
            ".vue",
            ".svelte",
            ".css",
            ".scss",
            ".sass",
            ".less",
            ".html",
            ".htm",
        ],
        "backend": [
            ".cs",
            ".java",
            ".py",
            ".go",
            ".rb",
            ".php",
            ".rs",
            ".scala",
            ".kt",
            ".swift",
        ],
        "database": [".sql", ".prisma", ".graphql"],
        "devops": [
            ".yml",
            ".yaml",
            ".sh",
            ".bash",
            ".ps1",
            ".tf",
            ".dockerfile",
        ],
        "config": [".json", ".toml", ".xml", ".ini", ".env"],
        "documentation": [".md", ".rst", ".txt", ".adoc"],
    }


# ==================== 協作能力評分標準 ====================

class CollaborationConfig:
    """協作能力評分配置"""

    # Revert 率評分標準
    REVERT_RATE_THRESHOLDS = {
        "excellent": 0.02,  # <2%：優秀
        "good": 0.05,  # 2-5%：正常
        # >5%：需改進
    }


# ==================== 工作模式評分標準 ====================

class WorkPatternConfig:
    """工作模式評分配置"""

    # 工作時段定義（小時）
    WORK_HOURS_START = 9
    WORK_HOURS_END = 18

    # 深夜時段定義（小時）
    LATE_NIGHT_START = 22
    LATE_NIGHT_END = 6

    # 工作時段提交率評分標準
    WORK_HOURS_THRESHOLDS = {
        "excellent": 0.60,  # >60% 在工作時段：優秀
        "good": 0.40,  # 40-60%：良好
        # <40%：可能有時間管理問題
    }


# ==================== 排除規則 ====================

class ExclusionConfig:
    """排除規則配置"""

    # 排除的 Bot 賬號（username 或 email 關鍵字）
    EXCLUDED_BOTS = [
        "renovate",
        "dependabot",
        "gitlab-bot",
        "github-bot",
        "bot",
        "ci",
        "automation",
        "jenkins",
        "travis",
    ]

    # 排除的檔案模式（使用 glob 模式）
    EXCLUDED_FILE_PATTERNS = [
        "package-lock.json",
        "yarn.lock",
        "pnpm-lock.yaml",
        "Gemfile.lock",
        "poetry.lock",
        "composer.lock",
        "Cargo.lock",
        "dist/*",
        "build/*",
        "node_modules/*",
        "vendor/*",
        ".next/*",
        "out/*",
        "target/*",
        "*.min.js",
        "*.min.css",
        "*.map",
        "*.bundle.js",
    ]

    # 排除的 Commit Message 模式（正則表達式）
    EXCLUDED_COMMIT_PATTERNS = [
        r"^Merge branch",
        r"^Merge pull request",
        r"^Merge remote-tracking",
        r"^Initial commit",
        r"^WIP",
        r"^wip",
    ]


# ==================== 分級標準 ====================

class GradingConfig:
    """開發者分級標準"""

    # 分數區間與等級對應
    GRADE_THRESHOLDS = [
        (8.0, "🏆 高級工程師", "senior"),
        (5.0, "⭐ 中級工程師", "mid"),
        (0.0, "🌱 初級工程師", "junior"),
    ]

    @staticmethod
    def get_grade(score: float) -> Tuple[str, str, str]:
        """
        根據分數取得等級

        Args:
            score: 總分（0-10）

        Returns:
            Tuple[emoji_title, english_level, description]
        """
        for threshold, title, level in GradingConfig.GRADE_THRESHOLDS:
            if score >= threshold:
                return (title, level, GradingConfig._get_description(level))
        return ("🌱 初級工程師", "junior", GradingConfig._get_description("junior"))

    @staticmethod
    def _get_description(level: str) -> str:
        """取得等級描述"""
        descriptions = {
            "senior": (
                "• Message 規範率 90%+\n"
                "• 小型變更佔比 80%+\n"
                "• 涉及 3+ 技術棧\n"
                "• 修復率 <15%\n"
                "• 有架構級別變更\n"
                "• Review 參與度高，能發現 Critical Issues"
            ),
            "mid": (
                "• Message 規範率 60-90%\n"
                "• 變更粒度合理\n"
                "• 2-3 種技術棧\n"
                "• 修復率 15-30%\n"
                "• 功能開發為主\n"
                "• 有 Code Review 參與但深度一般"
            ),
            "junior": (
                "• Message 不規範\n"
                "• 大量修復性提交\n"
                "• 單一技術棧\n"
                "• 變更集中於小範圍\n"
                "• Code Review 參與少或僅 LGTM"
            ),
        }
        return descriptions.get(level, "")


# ==================== 時間範圍配置 ====================

class TimeRangeConfig:
    """時間範圍配置"""

    # 預設分析時間範圍（相對於當前日期）
    DEFAULT_DAYS_BACK = 365  # 預設分析過去一年

    # 進步趨勢分析：早期 vs 近期時間切分比例
    PROGRESS_SPLIT_RATIO = 0.5  # 前 50% 時間 vs 後 50% 時間


# ==================== 配置驗證 ====================

def validate_all_configs() -> bool:
    """驗證所有配置的合理性"""
    print("🔍 驗證分析參數配置...")

    # 1. 驗證權重總和
    if not WEIGHTS.validate():
        print("❌ 權重總和不等於 1.0")
        return False
    print(f"✅ 權重配置正確（總和 = 1.0）")

    # 2. 驗證閾值合理性
    if CommitQualityConfig.CHANGE_SIZE_SMALL >= CommitQualityConfig.CHANGE_SIZE_MEDIUM:
        print("❌ 變更粒度閾值設定不合理")
        return False
    print("✅ 變更粒度閾值設定合理")

    # 3. 驗證工作時段設定
    if (
        WorkPatternConfig.WORK_HOURS_START >= WorkPatternConfig.WORK_HOURS_END
        or WorkPatternConfig.LATE_NIGHT_START <= WorkPatternConfig.LATE_NIGHT_END
    ):
        print("❌ 工作時段設定不合理")
        return False
    print("✅ 工作時段設定合理")

    print("✅ 所有配置驗證通過！")
    return True


# ==================== 配置匯出 ====================

def export_config_summary() -> Dict:
    """匯出配置摘要（用於文檔生成）"""
    return {
        "weights": WEIGHTS.to_dict(),
        "commit_quality": {
            "message_quality_excellent": CommitQualityConfig.MESSAGE_QUALITY_THRESHOLDS[
                "excellent"
            ],
            "change_size_small": CommitQualityConfig.CHANGE_SIZE_SMALL,
            "fix_rate_excellent": CommitQualityConfig.FIX_RATE_THRESHOLDS["excellent"],
        },
        "code_review": {
            "participation_excellent": CodeReviewConfig.PARTICIPATION_THRESHOLDS[
                "excellent"
            ],
            "depth_excellent": CodeReviewConfig.DEPTH_THRESHOLDS["excellent"],
            "timeliness_excellent": CodeReviewConfig.TIMELINESS_THRESHOLDS["excellent"],
        },
        "contribution": {
            "commit_count_high": ContributionConfig.COMMIT_COUNT_THRESHOLDS["high"],
        },
        "tech_breadth": {
            "tech_stack_excellent": TechBreadthConfig.TECH_STACK_THRESHOLDS["excellent"],
        },
        "grading": {
            "senior_threshold": 8.0,
            "mid_threshold": 5.0,
        },
    }


# 測試配置功能（可獨立執行此模組測試）
if __name__ == "__main__":
    print("=" * 60)
    print("分析參數配置測試")
    print("=" * 60)

    # 驗證配置
    validate_all_configs()

    print("\n" + "=" * 60)
    print("評估維度權重")
    print("=" * 60)
    for dimension, weight in WEIGHTS.to_dict().items():
        print(f"  {dimension:20s}: {weight*100:5.1f}%")

    print("\n" + "=" * 60)
    print("分級標準")
    print("=" * 60)
    test_scores = [9.5, 7.2, 6.0, 4.5, 2.0]
    for score in test_scores:
        title, level, desc = GradingConfig.get_grade(score)
        print(f"\n分數 {score:.1f} → {title} ({level})")

    print("\n" + "=" * 60)
    print("配置摘要")
    print("=" * 60)
    import json

    summary = export_config_summary()
    print(json.dumps(summary, indent=2, ensure_ascii=False))
