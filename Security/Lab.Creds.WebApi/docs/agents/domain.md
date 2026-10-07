# Domain Docs

## Layout

採 single-context：本專案目錄（`Security/Lab.Creds.WebApi`）的 `GLOSSARY.md` 與 `docs/adr/`。本文路徑均相對於本專案目錄。

## Before exploring

先讀本專案目錄的 GLOSSARY.md；若已有 GLOSSARY-MAP.md，依映射讀取相關 context 的詞彙表。讀取涉及工作範圍的 docs/adr/，多 context 時也讀取 src/<context>/docs/adr/。

上述檔案不存在時直接繼續，不將缺少文件列為阻擋，不預先建立；由 domain-modeling 在領域詞彙或決策確定時按需建立。

## Vocabulary and decisions

Issue、spec、測試名稱與分析使用詞彙表定義，不改用明確禁止的同義詞。缺少必要詞彙時，交由 domain-modeling 釐清。

若方案與既有 ADR 衝突，明確指出衝突及原因，不靜默覆寫既有決策。
