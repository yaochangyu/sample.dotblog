# 在 AI Agent 時代保護機敏性金鑰部落格文章撰寫計畫

此計畫旨在撰寫一篇介紹在 AI Agent 時代保護機敏性金鑰（如 API 金鑰與授權憑證）的技術文章，並透過 `metablog-cli` 工具發布至點部落。

## 計畫步驟

- [ ] **步驟 1：撰寫部落格文章草案 (ProtectSecretInAIAgent.md)**
  - **說明**：在 `/mnt/d/lab/sample.dotblog/Security/Lab.Secret` 底下撰寫部落格文章，融合使用者提供的防護機制與 Composio 的安全建議，使用台灣繁體中文，且排版符合 Markdown 規範。
- [ ] **步驟 2：使用 stop-slop-zh-tw 規則潤飾文章**
  - **說明**：對文章內容進行審查與修改，消除 AI 慣用語，並將可能存在的中國用語校正為台灣慣用語。
- [ ] **步驟 3：透過 metablog-cli 將文章發布至點部落**
  - **說明**：執行發布腳本將該 Markdown 發布為點部落的草稿，並回填產生的 `postId` 到 frontmatter 中。
- [ ] **步驟 4：更新 @tree.md 並清理/封存計畫**
  - **說明**：更新專案根目錄的 `@tree.md` 以反映新增的檔案，並將已完成的計畫檔案移動至 `.archive` 目錄。
