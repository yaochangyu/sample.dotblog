## Implementation baseline

本專案採 ASP.NET Core 10（.NET 10）。實作遵循 [api.template 的 CLAUDE.md](https://github.com/yaochangyu/api.template/blob/main/CLAUDE.md)，並於實作前閱讀其中指定的 `.claude/development-rules.md`、`.claude/decision-framework.md` 及工作項相關指南。

開發新 API 端點前，須確認全專案採 API First 或 Code First，不得混用。採用上述開發規則不代表自動選定 OAuth 授權伺服器、Gateway 或其他依賴產品。

實作與程式碼審查規範：
- **實作流程（使用 `/implement-spec`）**：依提供之流程上下文執行（非依賴技能註冊表）。
  - **分支與工作區**：整個 spec 於單一 integration branch 完成。各 implementer 於各自的 worktree / branch 作業，開始前確認基準基於 integration branch，作業完成前合入 integration 最新 tip。
  - **任務圖結構**：Tickets 規劃為具備 blocking edges 的 task graph，依相依關係推進。
  - **開發與驗證**：實作採用 TDD。各完成切片經審查後再整合回 integration branch。
  - **收尾與結案**：全部完成後執行整體 code-review、修正、依本地 tracker 結案，並清理各 implementer worktrees。
  - **職責與約束**：專案既有五個角色及主 session 負責 git commit/push 之權責保持不變。不自動啟動 implement-spec，不自行提交推送，不清理待命 tabs。若派工角色未涵蓋 merger，由主 session 詢問使用者決定，不得自行指派新角色。
- **程式碼審查（使用 `/code-review`）**：
  - 正式審查前必須先固定比較基準（commit、branch 或 tag）。
  - 審查嚴格區分 **Standards**（編碼標準與規範符合度）與 **Spec**（需求規格與 issue 一致性）雙軸並行評估。


## Agent skills

以下設定只適用於 `Security/Lab.Creds.WebApi`；所有路徑均相對於此專案目錄。

### Issue tracker

Issues 與 specs 使用本地 Markdown，存於 `.scratch/<feature>/`。見 `docs/agents/issue-tracker.md`。

### Triage labels

採用五個預設 triage 角色，以 `Status:` 記錄。見 `docs/agents/triage-labels.md`。

### Domain docs

採 single-context：本專案目錄的 `GLOSSARY.md` 與 `docs/adr/`。見 `docs/agents/domain.md`。
