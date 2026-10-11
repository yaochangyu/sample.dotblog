# 21: 申請者驗證的整體驗收與文件

**What to build:** 以申請者驗證為主題，端到端驗證 AC-24 到 AC-30 全部情境，並更新領域詞彙與 ADR，讓「申請端點只接受已登錄 Client 並需持有證明」的決定與尚未決定的項目有明確紀錄。這張票是收尾驗收，不新增功能；若驗收發現缺漏，回到對應票修正。

**Blocked by:** 20 — 被拒絕申請嘗試的稽核。

**Status:** resolved

- [x] AC-24 到 AC-30 每一條都有對應的 BDD Scenario 並實際通過，完整測試套件 0 失敗。
- [x] 有一條端到端 Scenario 串起：已登錄 Client 申請新憑證並證明持有、管理員核准、新憑證取得 Token 並呼叫；同時攻擊者的匿名申請、替他人申請、無持有證明、超過上限都被擋下。
- [x] `GLOSSARY.md` 補上本需求新增的詞彙（例如申請者、持有證明），並與既有「Client 身分」「已驗證呼叫者」區分。
- [x] `docs/adr/0001-lab-poc-exceptions.md` 移除「申請端點未驗證身分」這個已知缺口，補記本需求帶來的限制（全新 Client 首次登錄不在範圍、單程序儲存等）。
- [x] 列出所有「待使用者確認」的 lab 暫定值：待核准數上限的數值、持有證明的表示方式、申請內容的簽章涵蓋範圍、逾期處理。
- [x] 不實作 Gateway 相關項目；沿用已確認的 lab 範圍決定。

## AC-24 至 AC-30 對應表

每列指向一個在 `AuthSpike.Tests/Features/` 內的 Scenario，該 Scenario 隨完整測試套件執行並通過。

| 編號 | 情境 |
|---|---|
| AC-24 | 未驗證者送出登錄申請被拒絕且不建立待核准紀錄 |
| AC-25 | 已登錄 Client 替其他 Client 提出申請被拒絕且對方的待核准清單不變 |
| AC-26 | 無法證明持有新私鑰的申請被拒絕且不建立待核准紀錄 |
| AC-27 | 已登錄 Client 以現有憑證為自己申請並證明持有新私鑰時建立帶已驗證身分的待核准紀錄 |
| AC-28 | 同一 Client 達到上限後新申請被明確拒絕且既有待核准申請不受影響 |
| AC-29 | 原始憑證已撤銷的 Client 不能提出申請 |
| AC-30 | 身分未驗證的申請被拒絕並留下標為未驗證的紀錄 |

## 實作紀錄（21）

**範圍與暫定值（lab 暫定、待使用者確認）**：
- 待核准數上限的數值：每個 Client 的待核准申請（憑證與簽章金鑰合計）上限為 5 筆，lab 暫定、待使用者確認。
- 持有證明的表示方式：憑證申請以新憑證私鑰對申請內容簽章（RSA PKCS#1 v1.5 SHA-256），簽章金鑰申請以新金鑰私鑰簽章（ECDSA P-256 SHA-256），以 Base64 放入 `possessionProof`；表示方式為 lab 暫定值，待使用者確認。
- 簽章涵蓋範圍：憑證申請的簽章內容只含 `clientId` 與新憑證指紋；簽章金鑰申請只含 `clientId`、`keyId` 與新公鑰的 SHA-256。不涵蓋 Token、時間戳或 nonce，因此同一份持有證明可被用於同一申請內容的重送，範圍是否足夠待使用者確認。
- 逾期處理：待核准申請暫定 24 小時逾期（21 單完成後的修正已實作；逾期後不能被核准或拒絕、不再佔用額度）。24 小時與逾期規則為 lab 暫定值，待使用者確認。
- 申請管道（工單、人工轉交或自助入口）與全新 Client 首次登錄不在本票範圍，待使用者確認。

**驗證範圍**：
- 本票新增 `AuthSpike.Tests/Features/ApplicantVerificationAcceptance.feature` 與 `Steps/ApplicantVerificationAcceptanceSteps.cs`。端到端情境與攻擊情境都經真實 mTLS 與 HTTP 端點，不直接呼叫內部登錄方法。
- 本票沒有改動正式程式；既有 AC-24 至 AC-30 的行為由 17、18、19、20 票的 Scenario 提供，本票以對應表確認每列 Scenario 存在。
- 記錄型 Scenario（`@record`）只檢查本紀錄、GLOSSARY 與 ADR 的文字，不證明系統行為。

**不實作 Gateway**：Gateway 相關項目沿用既有決定，略過（非完成），不納入本票驗收。

**驗證方式（可重現）**：
```
cd poc
dotnet build AuthSpike.Tests/AuthSpike.Tests.csproj
dotnet test AuthSpike.Tests/AuthSpike.Tests.csproj
```
（端到端 Scenario 名稱為「申請、證明持有、核准、取得 Token 呼叫與攻擊者被擋下串成一條端到端流程」，Feature 為 `ApplicantVerificationAcceptance`。）

**實際執行結果（續作）**：前次中斷已留下本票的測試與文件草稿，本次續作確認其全部通過，未改動正式程式。`dotnet build AuthSpike.Tests/AuthSpike.Tests.csproj`：0 錯誤、0 警告。`dotnet test AuthSpike.Tests/AuthSpike.Tests.csproj`：312 通過、0 失敗、2 略過（皆為 Gateway 項目，本 lab 不實作）；其中本票 `ApplicantVerificationAcceptance` 的 9 個測試案例全部通過。

**範圍限制**：全新 Client 首次登錄不在本票；申請與稽核仍為程序內儲存，只在單程序內驗證。
