# Lab.HashiCorpVault.Agent Solution

本專案為 HashiCorp Vault AppRole 與 Vault Agent 的 .NET 8.0 範例，包含管理端、Agent 端與 Vault API Client，展示如何自動化機敏資料管理與安全存取。

## 專案結構

- `Lab.HashiCorpVault.Admin/`  
  Vault 管理端，負責初始化 Vault、建立 AppRole、Policy、寫入秘密等。VaultAppRoleSetup.cs 使用 Vault Cli，Vault Web API 使用 HttpClient
- `Lab.HashiCorpVault.Agent/`  
  Vault Agent 範例，使用 C# 直接呼叫 Vault Cli 取得 AppRole 認證資訊並啟動 Vault Agent。
- `Lab.HashiCorpVault.Agent2/`  
  Vault Agent 範例，使用 C# 直接呼叫 Vault Web API 取得 AppRole 認證資訊並啟動 Vault Agent。
- `Lab.HashiCorpVault.Client/`  
  Vault API Client，封裝 Vault Web API 操作。

## 需求

- .NET 8.0 SDK
- [HashiCorp Vault](https://www.vaultproject.io/) (需本機安裝並啟動)
- Vault CLI (`vault` 指令需在 PATH)

## 快速開始

### 1. 啟動 Vault Server

```sh
vault server -dev
```

### 2. 管理端初始化 Vault

進入 `Lab.HashiCorpVault.Admin` 目錄，執行：

```sh
dotnet run
```

依提示輸入 Vault Root Token（預設 dev server 為 `root`），將自動建立 KV v2、AppRole、Policy 並寫入測試秘密。

### 3. 啟動 Vault Agent 範例

進入 `Lab.HashiCorpVault.Agent` 目錄，執行：

```sh
dotnet run
```

依提示輸入 Vault Token（可用管理端產生的 app-admin token），將自動取得 AppRole 認證資訊並啟動 Vault Agent，並驗證秘密渲染。

### 4. Vault Agent 進階範例

進入 `Lab.HashiCorpVault.Agent2` 目錄，執行：

```sh
dotnet run
```

依提示輸入 Vault Token，程式會自動取得 AppRole 認證資訊、啟動 Vault Agent，並直接透過 Vault Agent API 讀取秘密。

## 範本與設定檔

- `vault-agent-config.hcl`  
  Vault Agent 設定檔，定義 auto_auth、template、listener 等。
- `dev-db-config.ctmpl` / `prod-db-config.ctmpl`  
  Vault Agent 範本，渲染秘密到 JSON 檔案。

## 主要程式碼

- Vault API Client: [`Lab.HashiCorpVault.Client.VaultApiClient`](Lab.HashiCorpVault.Client/VaultApiClient.cs)
- 管理端流程: [`Lab.HashiCorpVault.Admin.Program`](Lab.HashiCorpVault.Admin/Program.cs)
- Agent 啟動流程: [`Lab.HashiCorpVault.Agent.Program`](Lab.HashiCorpVault.Agent/Program.cs)
- Agent2 進階流程: [`Lab.HashiCorpVault.Agent2.Program`](Lab.HashiCorpVault.Agent2/Program.cs)

## 注意事項

- 範例預設使用本機 Vault Dev Server，請勿用於生產環境。
- 請確保 `vault` CLI 可在命令列執行。
- 秘密、Token 僅供教學用途，請勿外洩。

---

如需更多說明，請參考各專案資料夾內程式碼與註解。