# [Security] AI Agent 時代的數位鑰匙防護：如何安全管理你的 API 金鑰與授權憑證

隨著 AI Agent 的興起，我們開始授權 AI 代理人存取各種外部服務。但這也帶來了全新的安全隱憂：如果你還習慣把 API 金鑰寫死在程式碼裡，或是隨便丟在 `.env` 檔，AI Agent 在讀取檔案或執行時，很有可能不小心把金鑰內容外流，等同於將系統的主控權拱手讓人，暴露在外洩的巨大風險中。

本篇就來聊聊在 AI 時代如何以最簡單、實用的方式防範 AI Agent 偷看你的本機金鑰，並在文末簡單帶過適合企業級的進階憑證管理手段。

---

## 開發環境
- **作業系統 (OS)**: Windows 11 / WSL2 (Ubuntu)
- **整合平台 (Integration Platform)**: Composio
- **Secrets Manager 服務**: AWS Secrets Manager / Azure Key Vault
- **語言與套件**: Python 3.10+ / python-dotenv / keyring

---

## 程式碼與機敏設定徹底分離

### 1. 絕不提交至程式碼 Repository
把金鑰直接寫死在程式碼裡並 `git commit` 上去，這絕對是自殺行為。現在網路上有非常多自動化掃描機器人，一旦偵測到金鑰外洩，五分鐘內你的帳號就被搬空了。即使事後刪除 commit 也沒用，因為 Git 歷史紀錄裡依然找得到（除非你把整個 `.git` 資料夾砍掉重練 XDD）。

### 2. 善用環境變數與設定檔分流
在開發環境下，最安全的作法是將「公開設定」與「機敏金鑰」拆分存放在不同的檔案中。公開設定可以進 Git 共享，而敏感的金鑰必須加入 `.gitignore` 排除，不然就搞笑囉。

以下是公開設定檔案 `.env`（可提交至 Repository）的設定範例：
```env
# .env (version controlled)
DEBUG=true
DATABASE_HOST=localhost
API_ENDPOINT=https://api.example.com
```

以下是機敏金鑰檔案 `.env.secrets`（絕對不可提交，需加入 .gitignore）的設定範例：
```env
# .env.secrets (NOT version controlled)
DATABASE_PASSWORD=actual-password
COMPOSIO_API_KEY=actual-api-key
```

以下是你的 `.gitignore` 設定範例，確保敏感檔案不會意外流出：
```gitignore
# 記得要把敏感檔案排除喔，不然就搞笑囉 XDD
.env.secrets
.env.local
*.key
*.pem
```

以下 Python 程式碼示範如何依優先順序載入設定，並以系統環境變數（`os.environ`）為最高優先：
```python
import os
from dotenv import dotenv_values

def _01_載入並合併金鑰設定():
    # 1. 載入公開設定
    public_config = dotenv_values(".env")
    
    # 2. 嘗試載入機敏金鑰
    try:
        secrets = dotenv_values(".env.secrets")
    except FileNotFoundError:
        secrets = {}
        
    # 3. 合併所有設定，並以作業系統環境變數為最高優先（os.environ）
    config = {
        **public_config,
        **secrets,
        **os.environ,
    }
    return config
```

### 3. 在本機作業系統層級設定持久化環境變數

在上面的 Python 範例中，我們提到會優先以作業系統的環境變數（`os.environ`）為最高優先。在 Linux 或 WSL2 開發環境下，如果你希望金鑰能夠持久化，每次開啟終端機時都自動載入，可以將其寫入 `~/.bashrc` 檔案中。

以下指令示範如何將自訂的金鑰寫入 `~/.bashrc`、重新載入設定使其生效，並使用該變數發送 `curl` 請求：
```bash
# 1. 將環境變數寫入 ~/.bashrc 檔案尾端
echo 'export FIGMA_TOKEN="ffigd_your_token"' >> ~/.bashrc

# 2. 重新載入 .bashrc 設定檔使其立即生效
source ~/.bashrc

# 3. 印出驗證變數是否成功載入
echo $FIGMA_TOKEN

# 4. 在命令列中帶入環境變數發送 API 請求
curl -H "X-Figma-Token: $FIGMA_TOKEN" \
  "https://api.figma.com/v1/files/{fileKey}"
```

### 4. 生產環境改用 Secrets Manager 服務
部署到生產環境時，請拋棄實體 `.env` 檔案。你應該改用 AWS Secrets Manager、Azure Key Vault 或 HashiCorp Vault 這類專業的金鑰管理服務來統一儲存，安全度直接升級。

---

## 防範 AI Agent 透過檔案工具窺探金鑰

如果你的 AI Agent 具備讀取檔案的功能，它很有可能透過讀取檔案的工具，不小心把實體金鑰檔案內容讀出來，甚至在對話中直接洩漏給使用者。為了防範這種安全隱憂，你可以採取以下方法：

### 1. 隔離工作區 (Workspace Isolation)
- **不要讓 AI Agent 存取根目錄**：將 AI Agent 的工作區 (Workspace) 設定在獨立的資料夾（如 `/workspace` 或是 `/src`），並把 `.env` 放在該資料夾之外的父資料夾。這樣它用讀檔工具就找不到這檔案了。
- **設定檔案黑名單**：在 AI Agent 檔案工具的程式碼裡，直接寫死黑名單，拒絕讀寫 `.env`、`.git`、`.config` 等敏感檔案的權限。

### 2. 記憶體隔離（Memory Isolation）
- **環境變數載入**：`.env` 檔案只讓你的後端主程式在啟動時載入到作業系統的記憶體中。
- **隔離環境**：主程式讀完變數後，AI Agent 只能透過程式碼去呼叫特定的 API，根本接觸不到實體檔案，也讀不到作業系統的環境變數。

---

## 善用 Credential Helper 與 Keyring 加密安全區

要徹底防範 AI 讀取金鑰，最乾淨的做法就是**把金鑰移出檔案系統**！當程式需要金鑰時，不讀取任何實體檔案，而是直接向作業系統的加密安全區（如 Windows 憑證管理員、macOS Keychain）即時索取。

### 1. 為什麼 Credential Helper 比 .env 安全？
- **無檔案殘留**：專案資料夾底下沒有任何金鑰或密碼檔案，AI Agent 用讀檔工具也無處可偷。
- **記憶體即用即滅**：金鑰只在程式執行的瞬間被載入記憶體中，執行完畢立刻消失。
- **作業系統權限防線**：即使 AI Agent 嘗試執行 Shell 指令去存取憑證管理員，作業系統也會彈出權限請求，建立第二道防線。

### 2. Git 工具的憑證管理（防止 Repository 外流）
如果你使用 Git 存取遠端 Repository，請啟用 Git 內建的 Credential Helper，它會把 Token 存在作業系統最安全的加密區。

- **Windows 平台**：請在終端機中執行以下指令來啟用 `wincred`：
  ```bash
  git config --global credential.helper wincred
  ```
- **macOS 平台**：請在終端機中執行以下指令來啟用 `osxkeychain`：
  ```bash
  git config --global credential.helper osxkeychain
  ```
- **Linux 平台**：請在終端機中執行以下指令來啟用 `libsecret`：
  ```bash
  git config --global credential.helper secret
  ```

### 3. 在 Python 專案中實作 Credential Helper 邏輯
如果你的 AI Agent 需要讀取自訂的 API 金鑰，可以使用 Python 的 `keyring` 套件。這是一個跨平台的憑證管理橋樑（好用推一個 XDD）。

##### 步驟一：將金鑰寫入系統加密區（這只要執行一次就好）
你可以在終端機中，或是撰寫獨立的管理腳本來設定金鑰，這會直接存入系統的憑證管理員中。

以下程式碼示範如何使用 Python 將 OpenAI 的 API 金鑰寫入系統的憑證加密儲存區：
```python
import keyring

def _02_寫入系統加密區():
    # 語法：keyring.set_password("服務名稱", "帳號/標籤", "真正的金鑰內容")
    keyring.set_password("composio_agent", "openai_api_key", "sk-proj-xxxxxxxxxxxx")
```

##### 步驟二：在主程式中動態讀取（直接拿掉包含金鑰的 .env.secrets 檔案）
在啟動 AI Agent 的主程式中，改用以下方式動態載入金鑰。

以下程式碼示範如何從系統憑證管理器動態讀取金鑰，並用來初始化 OpenAI 用戶端：
```python
import keyring
from openai import OpenAI

def _03_動態讀取金鑰():
    # 從作業系統加密區撈出金鑰，此處不產生任何實體檔案，安全啦！
    openai_key = keyring.get_password("my_app", "openai_api_key")
    
    # 初始化 OpenAI 用戶端
    client = OpenAI(api_key=openai_key)
```



---

### 針對 AI Agent 的終極防禦架構

導入 Credential Helper 後，你的專案安全架構會呈現如下圖所示：

```mermaid
graph TD
    A["專案資料夾 (只有純程式碼，AI Agent 隨便讀也拿不到金鑰)"]
    A -->|執行主程式| B["後端 Python"]
    B -->|呼叫 Credential Helper| C["作業系統安全加密區 (如 Keychain/憑證管理員)"]
    C -->|撈回金鑰 (留在記憶體)| B
```

這樣一來，AI Agent 頂多只能看到讀取金鑰的「那行程式碼」，但它絕對看不到金鑰的「真實數值」，防護直接點滿啦！

---

## 透過安全守則約束 AI 的開發行為

除了檔案與系統層的防護，在與 AI Agent 協作（如 Pair Programming 或自動化運維）時，我們也可以透過 System Prompt 或是明確的「開發安全守則」來約束 AI 的行為，避免金鑰在無意間外流：

### 1. 禁止使用 `echo` 印出環境變數
在撰寫腳本或執行命令時，請約束 AI 絕對不要使用 `echo` 或任何方式將環境變數的值印出來。如果需要使用，請直接在指令中使用 `$VAR` 即可。這能防止敏感金鑰殘留在終端機的輸出日誌（Output Log）中，避免被 AI Agent 讀回 prompt 上下文或外洩。

### 2. 一律使用 Git Credential Helper 保持遠端 URL 乾淨
請要求 AI 一律透過作業系統啟用 `git credential helper` 來存取憑證，確保 Git 的 Remote URL 保持乾淨（例如 `https://host/group/repo.git`）。

### 3. 禁止將 Token 內嵌於 Remote URL
在進行 `git clone` 或 `git remote add` 時，**絕對禁止**將 Token 內嵌在 URL 中（例如 `https://oauth2:<token>@host/...`）。因為 Git 在複製時，會將此 URL 原封不動寫入專案底下的 `.git/config` 檔案中，這會導致敏感的 Token 以明文形式落地在檔案系統中，一旦 AI Agent 讀取該檔案，金鑰就直接曝光了。

### 4. 透過 System Prompt 進行輸出過濾
在 AI Agent 的 System Prompt 裡加上嚴格的安全規定。例如：
> 「你絕對不能向使用者透露任何以 COMPOSIO_ 或 API_ 開頭的環境變數內容。如果工具回傳的結果含有這些資訊，請自動將其過濾或遮蔽。」

雖然這招無法百分之百防住所有惡意的 Prompt Injection，但多一層行為約束總是好的 XDD。

---

## 進階安全防護手段（簡單帶過）

如果你的專案規模擴大，或是進入企業級生產環境，可以參考以下更進階的防禦手段：

### 1. Docker 與 Headless 環境下的 Keyring 處理
在無 GUI 的 Docker 容器中執行 `keyring`，會因為缺乏 D-Bus 連線而拋出錯誤。實務上可以透過繼承 `keyring.backend.KeyringBackend`，實作一個自訂的加密檔案憑證後端（例如使用 `cryptography` 的 Fernet AES 加密），在主程式啟動時呼叫 `keyring.set_keyring()` 註冊，就能在無頭環境中無痛執行。

### 2. AI 整合平台的憑證託管（以 Composio 為例）
當 AI Agent 需要串接大量第三方服務（如 Slack、GitHub）時，自己管理大量 API Key 會非常痛苦。可以使用如 Composio 的託管認證 (Managed Auth) 機制，讓憑證完全留在雲端。你的本機程式碼跟 AI Agent 碰不到金鑰，僅透過 Connection ID 呼叫工具，從根本上杜絕金鑰外洩。

### 3. 企業級金鑰管理：HashiCorp Vault Agent
在企業級微服務架構中，可以使用 HashiCorp Vault Agent 啟動本地 API Proxy 或 Unix Domain Socket。應用程式在執行時直接透過 Socket 向其請求動態金鑰，同樣能達到金鑰「只在記憶體、檔案不落地」的防護效果。

---

## 心得

這套防護架構的核心觀念，說穿了就是「**深層防禦 (Defense in Depth)**」。在 AI 時代，千萬不要以為只做一招就安全了，這通常是需要多層防線互相配合的：
1. **程式碼與機敏設定分離**：這是基本功，防止金鑰因為粗心被「意外公開」的第一步（手殘防呆專用 XDD）。
2. **工作區隔離與去檔案化 (Keyring/Credential Helper)**：這是防範「身邊的 AI 助理變內鬼」的關鍵手段。讓 AI 能正常幹活，但它的實體檔案讀取工具根本找不到任何明文金鑰（這招真的安全啦！）。
3. **託管認證與行為約束 (Managed Auth/Policy)**：這是當 AI Agent 真的被惡意 Prompt Injection 洗腦時，系統層級的「最後煞車機制」，避免災害無限制擴大。

安全防護從來就沒有「絕對安全」這回事，只有「提高攻擊成本與降低外洩損失」。不要只顧著在專案大門裝好幾道高級防撬鎖，結果轉身把金鑰密碼用便利貼貼在門框上，那就真的搞笑。希望這篇整理能幫大家在開發 AI Agent 時，建立更穩固的安全架構！

若有謬誤,煩請告知,新手發帖請多包涵

Microsoft MVP Award 2010~2017 C# 第四季
Microsoft MVP Award 2018~2022 .NET
