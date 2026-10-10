# Issue Tracker Configuration

## Storage
- Strategy: Local markdown
- Location: `.scratch/<feature>/issues/`
- Spec Location: `.scratch/<feature>/spec.md`

## Format
Each ticket is stored as an individual Markdown file named `<number>-<short-slug>.md`.

Each ticket must contain:
1. Title and Description
2. What to build (Tracer-bullet vertical slice)
3. Blocked by (explicit dependencies or None)
4. Acceptance criteria

## Kanban Board & Lifecycle Transitions
- **開始實作時 (Start Implementation)**: 在動手前先將該張工單狀態標記為 `In Progress`（Markdown 看板標記為 `🟦 進行中`）。
- **測試通過後 (After Tests Pass)**: 實作完成並通過測試驗證全綠後，將狀態標記為 `Done`（Markdown 看板標記為 `✅ 完成`）。
