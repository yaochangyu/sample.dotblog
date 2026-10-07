# Issue tracker: Local Markdown

Issues 與 specs 存於本專案目錄（`Security/Lab.Creds.WebApi`）的 `.scratch/`，不建立遠端 issue。本文路徑均相對於本專案目錄，不是 Git repository 根目錄。

## Conventions

- 每個功能一個目錄：`.scratch/<feature-slug>/`。
- Spec：`.scratch/<feature-slug>/spec.md`。
- 實作 tickets：`.scratch/<feature-slug>/issues/<NN>-<slug>.md`，從 01 編號，每張 ticket 獨立一檔。
- 每份 issue／spec 頂部以 `Status:` 記錄 triage 角色，名稱依 `triage-labels.md`。
- 留言與對話紀錄附加於檔案末尾的 `## Comments`。

## Publish / Fetch

Skill 要求 publish 時，建立或更新上述本地檔案；要求 fetch 時，讀取指定 ticket 路徑，不使用 gh／glab。

## Wayfinding operations

- Map：`.scratch/<effort>/map.md`，記錄 Notes、Decisions-so-far、Fog。
- Child：`.scratch/<effort>/issues/NN-<slug>.md`，以 `Type:` 記錄 research／prototype／grilling／task。
- Wayfinding 執行狀態用 `Status: claimed`／`Status: resolved`；這是執行狀態，不是 triage 角色。
- 相依性：頂部 `Blocked by: NN, NN`；所有阻擋 ticket resolved 才可執行。
- Frontier：依編號選出未完成、未 claimed 且無阻擋的 ticket。
- Claim：開始前先寫入 `Status: claimed`。
- Resolve：附加 `## Answer`，更新為 resolved，並在 map 的 Decisions-so-far 加入答案位置。
