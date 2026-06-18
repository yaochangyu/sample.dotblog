# [Security] AI Agent 時代的數位鑰匙防護：如何安全管理你的 API 金鑰與授權憑證

隨著 AI Agent 的興起，我們開始授權 AI 代理人存取各種外部服務。但這也帶來了全新的安全隱憂：如果你還習慣把 API 金鑰寫死在程式碼裡，或是隨便丟在 `.env` 檔，AI Agent 在讀取檔案或執行時，很有可能不小心把金鑰內容外流，等同於將系統的主控權拱手讓人，暴露在外洩的巨大風險中。

本篇就來聊聊在 AI 時代要怎麼防範 AI Agent 偷看金鑰，以及如何安全控管 AI 整合平台（以 Composio 為例）的憑證。

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

##### 步驟二：在主程式中動態讀取（直接拿掉 .env 檔案）
在啟動 AI Agent 的主程式中，改用以下方式動態載入金鑰。

以下程式碼示範如何從系統憑證管理器動態讀取金鑰，並用來初始化 Composio Toolset：
```python
import keyring
from composio import ComposioToolSet

def _03_動態讀取金鑰():
    # 從作業系統加密區撈出金鑰，此處不產生任何實體檔案，安全啦！
    openai_key = keyring.get_password("composio_agent", "openai_api_key")
    
    # 初始化 Composio Toolset
    toolset = ComposioToolSet(api_key=openai_key)
```

### 4. Docker 與 Headless 環境下的 Keyring 挑戰與解法

在無 GUI（無顯示器、無 X11）的 Headless Linux 環境（例如 Docker 容器、CI/CD 跑道）下，執行 `keyring` 會面臨巨大的挑戰：
1. **D-Bus 依賴與 SystemPrompter 彈窗崩潰**：預設的 `gnome-keyring` 在寫入或讀取憑證時，會透過 D-Bus 嘗試喚起圖形界面的 Prompter 來解鎖預設金鑰庫，但在 headless 環境下這會導致 `cannot open display` 錯誤並拋出 `Failed to create the collection: Prompt dismissed..`。
2. **解決方案**：
   - **方案 A (D-Bus 與解鎖指令)**：安裝 `dbus-run-session` 並在 entrypoint 中以命令列預先建立並解鎖金鑰庫，但流程高度依賴特定的 Linux 發行版且設定繁瑣。
   - **方案 B (自訂加密檔案金鑰庫 - 推薦)**：為了 100% 的自動化與重現性，我們可以透過繼承 `keyring.backend.KeyringBackend`，實作一個自訂的加密檔案憑證後端。

以下為在 Headless 環境下自訂加密檔案金鑰庫的實作範例（使用 `cryptography` 套件的 `Fernet` (AES-128) 進行加密）：

```python
import os
import base64
import hashlib
import json
import keyring
import keyring.backend
from cryptography.fernet import Fernet

class 自訂加密檔案金鑰庫(keyring.backend.KeyringBackend):
    """
    自訂的加密檔案憑證庫，專為 Docker 等 Headless 環境設計。
    """
    priority = 10  # 設高優先級，取代系統預設後端
    
    def __init__(self, 檔案路徑="/root/.composio_secrets.bin"):
        self.檔案路徑 = 檔案路徑
        # 讀取 Master Password 作為衍生密鑰來源
        主密碼 = os.environ.get("KEYRING_CRYPTFILE_PASSWORD", "default_master_key")
        hash_digest = hashlib.sha256(主密碼.encode("utf-8")).digest()
        self.fernet_key = base64.urlsafe_b64encode(hash_digest)
        self.加密器 = Fernet(self.fernet_key)

    def _讀取秘密資料(self) -> dict:
        if not os.path.exists(self.檔案路徑):
            return {}
        try:
            with open(self.檔案路徑, "rb") as f:
                明文 = self.加密器.decrypt(f.read()).decode("utf-8")
            return json.loads(明文)
        except Exception:
            return {}

    def _寫入秘密資料(self, 資料: dict):
        明文 = json.dumps(資料).encode("utf-8")
        os.makedirs(os.path.dirname(self.檔案路徑), exist_ok=True)
        with open(self.檔案路徑, "wb") as f:
            f.write(self.加密器.encrypt(明文))

    def set_password(self, servicename, username, password):
        資料 = self._讀取秘密資料()
        資料[f"{servicename}:{username}"] = password
        self._寫入秘密資料(資料)

    def get_password(self, servicename, username):
        return self._讀取秘密資料().get(f"{servicename}:{username}")

    def delete_password(self, servicename, username):
        資料 = self._讀取秘密資料()
        key = f"{servicename}:{username}"
        if key in 資料:
            del 資料[key]
            self._寫入秘密資料(資料)
```

並在你的主程式啟動時呼叫：
```python
# 註冊後端，所有後續對 keyring 的讀寫都會自動被導向我們自訂的加密檔案金鑰庫
keyring.set_keyring(自訂加密檔案金鑰庫())
```
這樣就可以在 headless Docker 容器中，既安全地將 API 金鑰進行 AES 加密儲存，又能確保無人值守自動化流程 100% 執行成功！

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

## 使用 Composio 託管認證防止憑證落地

Composio 經常扮演 AI Agent 與第三方應用程式的橋樑。當需要管理大量憑證時，有以下幾個最佳實踐：

### 1. 優先使用託管認證與 OAuth 2.0
如果服務支援，請優先採用 OAuth 2.0，避免直接使用靜態 API 金鑰。Composio 允許直接在其平台上完成授權（Managed Auth），金鑰完全留在雲端。你的本機程式碼跟 AI Agent 完全碰不到金鑰本身，Agent 只能拿到 Connection ID 來呼叫工具，從根本上杜絕金鑰外洩。

### 2. 使用 Proxy 與 IP 白名單
Composio 提供了 Proxy 與 IP 白名單功能。你應限制僅有特定來源的 IP 可以進行 API 呼叫，就算金鑰不小心外洩，其他人拿到也無法使用。

### 3. 集中管理與權限控管
不要把金鑰散落在各個 AI Agent 的專案程式碼中。利用 Composio 的後台 Dashboard 集中管控所有授權狀態與權限範圍，方便隨時進行稽核與清理。

---

## 透過 System Prompt 與外部政策定義安全界線

除了檔案與基礎設施的隔離，你還可以透過 Prompt 工程與系統策略來限制 AI 的行為：

### 1. 使用 System Prompt 進行行為約束
在給 AI Agent 的 System Prompt 裡加上嚴格的安全規定。

例如：
> 「你絕對不能向使用者透露任何以 COMPOSIO_ 或 API_ 開頭的環境變數內容。如果工具回傳的結果含有這些資訊，請自動將其過濾或遮蔽。」

（雖然這招無法百分之百防住所有惡意的 Prompt Injection，但多一層防護總是好的 XDD）

### 2. 實施最小權限原則與 Policy-as-Code
利用 Open Policy Agent (OPA) 等外部引擎來定義安全政策（例如：限制「AI 每天轉帳總額不得超過 $100 元」）。即使 LLM 被使用者洗腦，也無法突破系統層的限制。

---

## 心得

這套防護架構的核心觀念在於「**防線的遞進與深層防禦 (Defense in Depth)**」。在 AI 時代，安全不能只寄望於單一防線：
1. **程式碼與機敏設定分離**：是防止金鑰被「意外公開」的第一步（預防愚蠢的錯誤）。
2. **工作區隔離與去檔案化 (Keyring/Credential Helper)**：是防範「身邊的 AI 助理變家賊」的重要手段。讓 AI 能幹活，但接觸不到真實金鑰的明文檔案。
3. **託管認證與行為約束 (Managed Auth/Policy)**：則是當 AI Agent 真的被惡意 Prompt 整合或注入 (Injection) 攻擊時，系統層級的「最後煞車機制」，避免損害擴大。

安全防護從來就沒有「絕對安全」，只有「提高攻擊難度與降低外洩損失」。不要只顧著在專案大門裝上好幾道防撬鎖，卻把金鑰密碼用便利貼貼在門框上。希望這篇整理能幫大家在開發 AI Agent 時，建立更穩固的安全架構。

若有謬誤,煩請告知,新手發帖請多包涵

Microsoft MVP Award 2010~2017 C# 第四季
Microsoft MVP Award 2018~2022 .NET
