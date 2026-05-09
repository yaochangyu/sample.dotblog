# blog-token-source 計畫

- [x] 步驟 1：在 `Server Side 配置` 補上「Token 發放端點」段落  
  原因：目前文章只有 `TokenProvider` 與前端取用方式，缺少 `/api/token` 如何呼叫 provider 並透過 response header 發放 token 的關鍵程式碼，讀者無法完整理解 token 的來源。

- [x] 步驟 2：在 `前端到後端互動流程` 補上完整流程說明  
  原因：這一節目前是空的，需要把「前端申請 token → 後端建立並回傳 → 前端帶 token 呼叫 API → 後端驗證」串成完整流程，補足文章敘事斷點。

- [x] 步驟 3：更新 `tree.md` 與確認文件內容一致  
  原因：這次會新增計畫檔並修改文章內容，依專案規範需要同步維護結構文件，避免文件與實際檔案狀態不一致。
