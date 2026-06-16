# OpenDesign 專案資料夾結構

> 此檔案隨檔案異動同步更新；`.gitignore` 排除的檔案、`bin/`、`obj/` 不記錄於此。

```
OpenDesign/
├── .archive/                       # 已完成（或被取代）計畫的封存資料夾
│   ├── docker-ai-cli.plan.md       # Docker + 四個 AI CLI 安裝計畫（已全部完成）
│   ├── docker-cli-ephemeral.plan.md   # （已被取代，併入 od-cli-consolidation.plan.md）
│   ├── od-cli-consolidation.plan.md   # 整併 start.sh + docker-cli.sh 為 od-cli.sh（已全部完成）
│   ├── start-stop-scripts.plan.md     # （已被取代，併入 od-cli-consolidation.plan.md）
│   └── sync-mirror-dest.plan.md       # sync-file.sh 改成 mirror 同步（先清空 DEST 再複製，已全部完成）
├── .devcontainer/                  # Docker 模式：AI CLI 容器設定（image 定義，管理入口在根目錄 od-cli.sh）
│   ├── Dockerfile                  # Ubuntu base + Node 24 + Claude/Copilot/Codex/Antigravity CLI
│   ├── README.md                   # Docker 模式說明：指令、CLI 清單、登入步驟、已知限制
│   ├── devcontainer.json           # VS Code Dev Containers 設定，四組憑證 named volume
│   └── start-keyring.sh            # 容器內模擬桌面 keyring（dbus + gnome-keyring）；實測後確認非必要，保留無壞處
├── .issues/                        # 問題紀錄資料夾
│   ├── docker-ai-cli.issues.md     # docker-ai-cli 相關的失敗方法與解法紀錄（共 6 個 issue）
│   └── od-cli.issues.md            # od-cli.sh 合併過程中的 set -e/pipefail bug 紀錄（2 個 issue）
├── INSTALL.md                      # 安裝手冊（Dev 模式 + Docker 模式）
├── README.md                       # 專案總覽：兩種模式的快速指令、指向 INSTALL.md / tree.md
├── od-cli.sh                       # 統一入口：Dev 模式啟停（start/stop）+ AI CLI 一次性容器（build/shell/run）
└── sync-file.sh                    # 把這個專案同步複製到 /mnt/d/lab/sample.dotblog/OpenDesign
```
