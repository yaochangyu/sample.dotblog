# OpenDesign 專案資料夾結構

> 此檔案隨檔案異動同步更新；`.gitignore` 排除的檔案、`bin/`、`obj/` 不記錄於此。

```
OpenDesign/
├── .archive/                       # 已完成（或被取代）計畫的封存資料夾
│   ├── docker-ai-cli.plan.md       # Docker + 四個 AI CLI 安裝計畫（已全部完成，功能已移除）
│   ├── docker-cli-ephemeral.plan.md   # （已被取代，併入 od-cli-consolidation.plan.md）
│   ├── od-cli-consolidation.plan.md   # 整併 start.sh + docker-cli.sh 為 od-cli.sh（已全部完成）
│   ├── start-stop-scripts.plan.md     # （已被取代，併入 od-cli-consolidation.plan.md）
│   └── sync-mirror-dest.plan.md       # sync-file.sh 改成 mirror 同步（已完成，sync-file.sh 已移除）
├── .issues/                        # 問題紀錄資料夾
│   ├── docker-ai-cli.issues.md     # docker-ai-cli 相關的失敗方法與解法紀錄（共 6 個 issue）
│   └── od-cli.issues.md            # od-cli.sh 合併過程中的 set -e/pipefail bug 紀錄（2 個 issue）
├── INSTALL.md                      # 安裝手冊（一次性安裝 + 日常 start/stop/update 指令）
├── README.md                       # 專案總覽：快速指令、指向 INSTALL.md / tree.md
├── blog.md                         # 點部落文章
├── callback-logs.html              # 本地 UI 測試與 callback 紀錄檔
├── od-cli.py                       # 統一入口：start / stop / update / status / version / help（跨平台，uv run）
├── src/                            # AI 產出的 UI 設計稿與 HTML 頁面
└── tree.md                         # 專案資料夾結構（本檔案）
```
