# 問題紀錄：InstallMetablogSkill

## 失敗的方法
在技能目錄下直接執行 `git clone`。

## 步驟
1. 執行 `rm -rf /home/yao/.gemini/skills/metablog-cli` 清理目錄。
2. 執行命令：`git clone https://github.com/yaochangyu/metablog-cli.git /home/yao/.gemini/skills/metablog-cli`。

## 原因
雖然執行了刪除，但由於背景程序或系統監控會立即在 `/home/yao/.gemini/skills/metablog-cli` 下重新建立空的 `.env` 檔案，導致目的地目錄非空，使得 `git clone` 出現 `fatal: destination path ... already exists and is not an empty directory.` 錯誤。

## 失敗的方法 2
執行 `git checkout -f master`。

## 步驟
1. 執行 `git init && git remote add origin https://github.com/yaochangyu/metablog-cli.git && git fetch`。
2. 執行 `git checkout -f master`。

## 原因
遠端 repository 只有 `main` 分支（`* [new branch] main -> origin/main`），沒有 `master` 分支，導致 checkout 時出錯：`error: pathspec 'master' did not match any file(s) known to git`。

## 後續對策
執行 `git checkout -f main` 即可。
