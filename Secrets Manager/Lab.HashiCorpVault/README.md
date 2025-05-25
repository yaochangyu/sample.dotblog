# 使用 C# VaultSharp 訪問 HashiCorp Vault 入門指南

## 專案簡介

本專案展示如何使用 C# 和 VaultSharp 庫來安全地存取和管理 HashiCorp Vault 中的機敏資料。HashiCorp Vault 是一個安全的密鑰管理工具，可用於保護應用程序中的機敏資料，如密碼、API 金鑰和憑證等。

## 前置需求

- .NET 6.0 或更高版本
- HashiCorp Vault 伺服器 (本地或遠程)
- Visual Studio 2022 或其他 .NET 開發環境

## 安裝步驟

### 安裝 HashiCorp Vault

#### Windows
```powershell
scoop install vault
choco install vault
```

### 本機環境啟動開發用的 Vault Server
```powershell
vault server -dev
```


### 本機環境啟動開發用的 Vault Server
```powershell
vault server -dev
```
# 此範例文章位置
https://dotblogs.com.tw/yc421206/2024/10/05/protect_secret_data_via_csharp_vaultSharp_access_hashicorp_vault_getting_start