### OpenLDAP 環境建置計畫

#### 步驟 1：設計 LDAP 目錄資訊樹 (DIT) 結構

為了反映您的公司組織架構，我建議採用以下階層式 DIT 結構：

-   **基礎 DN (Base DN)**：`dc=1111,dc=com`
    -   這將是整個目錄的根節點，代表「1111 全球華人集團」。

-   **組織單位 (Organizational Units)**：
    -   `ou=Subsidiaries,dc=1111,dc=com`：一個專門存放所有子公司的 OU。
        -   `ou=21stCentury,ou=Subsidiaries,dc=1111,dc=com` (21 世紀)
        -   `ou=iHunter,ou=Subsidiaries,dc=1111,dc=com` (愛客獵)
        -   `ou=1111Tech,ou=Subsidiaries,dc=1111,dc=com` (1111 科技)
    -   在每個子公司底下，為其建立各部門的 OU。例如，在 `21stCentury`底下：
        -   `ou=TechRD,ou=21stCentury,...` (技術研發部)
        -   `ou=HR,ou=21stCentury,...` (人力資源部)
        -   `ou=Finance,ou=21stCentury,...` (財務部)
        -   ...以此類推，為所有部門建立對應的 OU。
    -   `ou=Groups,dc=1111,dc=com`：一個專門存放所有權限群組的 OU，方便集中管理。

-   **使用者帳號 (Users)**：
    -   使用者帳號將存放在其所屬的部門 OU 底下。
    -   例如，21 世紀技術研發部的員工 `John Doe`，其 DN 將會是：`uid=jdoe,ou=TechRD,ou=21stCentury,ou=Subsidiaries,dc=1111,dc=com`。

-   **權限群組 (Groups)**：
    -   群組將存放在 `ou=Groups` 中。
    -   群組命名將能清晰反映其權限範圍，例如：
        -   `cn=21stCentury-TechRD-Admins,ou=Groups,dc=1111,dc=com`
        -   `cn=iHunter-Sales-Users,ou=Groups,dc=1111,dc=com`
        -   `cn=Global-Admins,ou=Groups,dc=1111,dc=com`

#### 步驟 2：定義 Schema

-   **使用者 (Users)**：我們將採用標準的 `inetOrgPerson` objectClass。它包含了 `uid`, `cn` (Common Name), `sn` (Surname), `givenName`, `mail`, `userPassword` 等常用屬性，無需擴充自訂 schema 即可滿足需求。
-   **群組 (Groups)**：我們將採用標準的 `groupOfUniqueNames` objectClass。它透過 `uniqueMember` 屬性來管理群組成員，結構清晰。

#### 步驟 3：規劃存取控制清單 (ACLs)

我將定義一組安全的 ACL 基礎規則：

1.  **管理員權限**：LDAP 管理員 (`cn=admin,dc=1111,dc=com`) 擁有對整個目錄的完全讀寫權限。
2.  **使用者自身權限**：任何登入的使用者可以修改自己的密碼 (`userPassword`) 和部分個人資訊 (如電話號碼)。
3.  **已驗證使用者權限**：所有登入的使用者可以讀取目錄中大部分公開的資訊（如員工姓名、部門、email），但無法讀取敏感資訊（如密碼雜湊值）。
4.  **匿名使用者權限**：匿名使用者僅能用於驗證，無法讀取任何目錄資料。

#### 步驟 4：建立 LDIF 設定檔

我將產生以下 LDIF (LDAP Data Interchange Format) 檔案，用於初始化和填充目錄資料：

-   `01-base-structure.ldif`：建立基礎的 OU，如 `ou=Subsidiaries` 和 `ou=Groups`。
-   `02-companies-and-departments.ldif`：建立三家子公司及其下所有部門的 OU。
-   `03-groups.ldif`：建立核心權限群組，例如一個全域管理員群組。
-   `04-users.ldif`：為每個部門建立 1-2 名範例使用者，以供測試。

#### 步驟 5：產生自動化腳本 (`setup-ldap.sh`)

最後，我會提供一個 `setup-ldap.sh` 腳本。此腳本將會：
-   使用 `docker-compose` 啟動 OpenLDAP 服務 (基於您專案中的 `docker-compose.yml`)。
-   等待 OpenLDAP 服務準備就緒。
-   依照正確順序，使用 `ldapadd` 指令將上述的 `.ldif` 檔案匯入到 OpenLDAP 中，完成整個目錄的自動化建置。
-   腳本會包含清晰的註解，說明如何設定管理員密碼等變數。