# API Key 申請與驗證說明

本文件提供業務、合作夥伴窗口與內部管理人員閱讀，說明如何申請 API Key，以及系統如何確認每一
次 API 呼叫是由已核發的合作夥伴送出。這是搭配 `Lab.API.Signature` 教學 Lab 的作業說明，
不是系統本身提供的線上申請頁面。

## 文件索引

- [key-application-form-example.md](./key-application-form-example.md)：API Key 申請單的空白表格與填寫範例。
- [key-application-process.md](./key-application-process.md)：從提出申請、內部審核、Admin API 核發到安全交付的完整流程。
- [signature-generation-and-verification.md](./signature-generation-and-verification.md)：Client 產生 HMAC 簽章與 Server 驗證／比對簽章的機制。
- [protected-api-verification.md](./protected-api-verification.md)：受保護 API 請求從 Header 檢查到 Nonce 防重放的完整驗證流程。
