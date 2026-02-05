# OpenLDAP 開發環境

此專案定義了一個基於 Docker 的 OpenLDAP 開發環境，包含 OpenLDAP 伺服器本身以及一個用於管理 LDAP 的 Web 介面工具 phpLDAPadmin。

## 啟動方式

請確保您的系統已安裝 Docker 和 Docker Compose。

在專案根目錄下，執行以下指令啟動所有服務：

```bash
docker compose up -d
```

-   `-d` 參數表示在背景執行服務。

## 服務說明

### 1. `openldap`

-   **用途**: 提供 OpenLDAP 伺服器，用於儲存和管理使用者、群組等目錄服務資料。
-   **映像檔**: `osixia/openldap:1.5.0`
-   **容器名稱**: `openldap`
-   **主機名稱**: `openldap`
-   **連接埠**:
    -   `389:389`: LDAP 標準埠 (非加密)
    -   `636:636`: LDAPS (LDAP over SSL/TLS) 加密埠
-   **環境變數**:
    -   `LDAP_ORGANISATION`: "DevOps Lab" (組織名稱)
    -   `LDAP_DOMAIN`: "devopslab.com" (LDAP 網域名稱)
    -   `LDAP_ADMIN_PASSWORD`: "adminpassword" (管理員密碼，**請務必修改為複雜且安全的密碼**)
    -   `LDAP_TLS_VERIFY_CLIENT`: "never" (不驗證客戶端憑證)
    -   `LDAP_TLS_CRT_FILENAME`, `LDAP_TLS_KEY_FILENAME`, `LDAP_TLS_CA_CRT_FILENAME`: TLS/SSL 憑證相關設定
    -   `LDAP_ALLOW_ANON_BIND`: "false" (不允許匿名綁定)
-   **資料卷 (Volumes)**:
    -   `./data/ldap:/var/lib/ldap`: LDAP 資料的持久化儲存
    -   `./data/config:/etc/ldap/slapd.d`: LDAP 配置的持久化儲存
    -   `./certs:/container/service/slapd/assets/certs`: 憑證檔案存放處
    -   `./ldif:/etc/ldap/ldif`: 初始 LDIF 檔案存放處
    -   `./scripts:/usr/local/bin`: 自定義腳本存放處
-   **重啟策略**: `always` (容器退出時總是重新啟動)

### 2. `phpldapadmin`

-   **用途**: 提供一個基於 Web 的圖形化介面，用於方便地管理 OpenLDAP 伺服器中的資料。
-   **映像檔**: `osixia/phpldapadmin:0.9.0`
-   **容器名稱**: `phpldapadmin`
-   **主機名稱**: `phpldapadmin`
-   **連接埠**: `8080:80` (透過 `http://localhost:8080` 訪問)
-   **環境變數**:
    -   `PHPLDAPADMIN_LDAP_HOSTS`: "openldap" (指向 OpenLDAP 服務的主機名稱)
    -   `PHPLDAPADMIN_LDAP_PORT`: "636" (使用 LDAPS 連接埠)
    -   `PHPLDAPADMIN_HTTPS`: "false" (Web 介面使用 HTTP 訪問)
    -   `PHPLDAPADMIN_LDAP_PROTOCOL`: "ldaps" (指定使用 LDAPS 協議連接 LDAP 伺服器)
    -   `PHPLDAPADMIN_LDAP_BASE_DN`: "dc=devopslab,dc=com" (LDAP 的基礎 DN)
    -   `PHPLDAPADMIN_LDAP_BIND_DN`: "cn=admin,dc=devopslab,dc=com" (LDAP 管理員的綁定 DN)
    -   `PHPLDAPADMIN_LDAP_PASS`: "adminpassword" (管理員密碼，**請確保與 `openldap` 服務的 `LDAP_ADMIN_PASSWORD` 一致**)
-   **依賴**: `openldap` (確保 openldap 服務啟動後才啟動 phpldapadmin)
-   **重啟策略**: `always` (容器退出時總是重新啟動)
