# OpenLDAP Docker Compose 開發環境設定指南

本指南旨在提供一個「可重現、可自動化、可驗證」的 OpenLDAP 開發環境，使用 Docker Compose 進行部署，並涵蓋基本功能、管理員帳號、使用者/群組管理、ACL 權限設定、日誌記錄及 TLS/SSL 加密。

## 專案結構

在專案根目錄下，您將會看到以下結構：

```
.
├── docker-compose.yml
├── ldif
│   ├── acl.ldif
│   └── users_and_groups.ldif
├── scripts
│   ├── setup.sh
│   └── verify.sh
└── certs/
└── data/
    ├── config/
    └── ldap/
```

*   `docker-compose.yml`: 定義 OpenLDAP 和 phpLDAPadmin 服務。
*   `ldif/`: 存放 LDIF (LDAP Data Interchange Format) 檔案，用於初始化 LDAP 資料。
*   `scripts/`: 存放自動化設定和驗證腳本。
*   `certs/`: 存放 TLS/SSL 憑證。
*   `data/`: 存放 OpenLDAP 的持久化資料和配置。

## 步驟 1: 準備工作

### 1.1 安裝 Docker 和 Docker Compose

請確保您的系統已安裝 Docker 和 Docker Compose。

*   **Docker 安裝指南**：[https://docs.docker.com/get-docker/](https://docs.docker.com/get-docker/)
*   **Docker Compose 安裝指南**：[https://docs.docker.com/compose/install/](https://docs.docker.com/compose/install/)

### 1.2 建立必要的目錄

```bash
mkdir -p certs ldif scripts data/ldap data/config
```

### 1.3 建立 `docker-compose.yml`

在專案根目錄下建立 `docker-compose.yml` 檔案，內容如下：

```yaml
version: '3.8'

services:
  openldap:
    image: osixia/openldap:1.5.0
    container_name: openldap
    hostname: openldap
    ports:
      - "389:389"
      - "636:636"
    environment:
      # Domain configuration
      LDAP_ORGANISATION: "DevOps Lab"
      LDAP_DOMAIN: "devopslab.com"
      LDAP_ADMIN_PASSWORD: "adminpassword" # 請務必更改此密碼為複雜且安全的密碼
      
      # TLS/SSL configuration
      LDAP_TLS_VERIFY_CLIENT: "never"
      LDAP_TLS_CRT_FILENAME: "openldap.crt"
      LDAP_TLS_KEY_FILENAME: "openldap.key"
      LDAP_TLS_CA_CRT_FILENAME: "ca.crt"
      
      # Persistence
      LDAP_REPLICATION_HOSTS: "openldap" 
      
      # For initial setup, allow root to bind without authentication (will be secured later)
      LDAP_ALLOW_ANON_BIND: "false"

    volumes:
      - ./data/ldap:/var/lib/ldap # Persistent storage for LDAP data
      - ./data/config:/etc/ldap/slapd.d # Persistent storage for LDAP configuration
      - ./certs:/container/service/slapd/assets/certs # Certificates volume
      - ./ldif:/etc/ldap/ldif # Volume for initial LDIF files
      - ./scripts:/usr/local/bin # Volume for custom scripts
    
    restart: always

  phpldapadmin:
    image: osixia/phpldapadmin:0.12.0
    container_name: phpldapadmin
    hostname: phpldapadmin
    ports:
      - "8080:80"
    environment:
      PHPLDAPADMIN_LDAP_HOSTS: "openldap"
      PHPLDAPADMIN_LDAP_PORT: "636" # Use 636 for LDAPS
      PHPLDAPADMIN_HTTPS: "false" # Keep false for http access to phpldapadmin's web UI
      PHPLDAPADMIN_LDAP_PROTOCOL: "ldaps" # Use 'ldaps' for TLS/SSL connection
      PHPLDAPADMIN_LDAP_BASE_DN: "dc=devopslab,dc=com"
      PHPLDAPADMIN_LDAP_BIND_DN: "cn=admin,dc=devopslab,dc=com"
      PHPLDAPADMIN_LDAP_PASS: "adminpassword" # 請務必更改此密碼與LDAP_ADMIN_PASSWORD一致

    depends_on:
      - openldap
    restart: always
```

### 1.4 建立 `ldif/users_and_groups.ldif`

此檔案將初始化組織單位、範例使用者和群組。在 `ldif` 目錄下建立 `users_and_groups.ldif`：

```ldif
dn: ou=users,dc=devopslab,dc=com
objectClass: organizationalUnit
ou: users

dn: ou=groups,dc=devopslab,dc=com
objectClass: organizationalUnit
ou: groups

dn: cn=developers,ou=groups,dc=devopslab,dc=com
objectClass: top
objectClass: groupOfNames
cn: developers
member: cn=john.doe,ou=users,dc=devopslab,dc=com
member: cn=jane.smith,ou=users,dc=devopslab,dc=com

dn: cn=managers,ou=groups,dc=devopslab,dc=com
objectClass: top
objectClass: groupOfNames
cn: managers
member: cn=jane.smith,ou=users,dc=devopslab,dc=com

dn: cn=john.doe,ou=users,dc=devopslab,dc=com
objectClass: top
objectClass: person
objectClass: organizationalPerson
objectClass: inetOrgPerson
cn: john.doe
sn: Doe
givenName: John
displayName: John Doe
uid: john.doe
mail: john.doe@devopslab.com
userPassword: {SSHA}password # 開發環境使用，實際部署請使用 slappasswd 生成安全密碼

dn: cn=jane.smith,ou=users,dc=devopslab,dc=com
objectClass: top
objectClass: person
objectClass: organizationalPerson
objectClass: inetOrgPerson
cn: jane.smith
sn: Smith
givenName: Jane
displayName: Jane Smith
uid: jane.smith
mail: jane.smith@devopslab.com
userPassword: {SSHA}password # 開發環境使用，實際部署請使用 slappasswd 生成安全密碼
```

### 1.5 建立 `ldif/acl.ldif`

此檔案將配置 ACL 權限。在 `ldif` 目錄下建立 `acl.ldif`：

```ldif
dn: olcDatabase={1}mdb,cn=config
changetype: modify
add: olcAccess
olcAccess: {0}to attrs=userPassword,shadowLastChange by self write by anonymous auth by * none
olcAccess: {1}to dn.base="" by * read
olcAccess: {2}to * by dn.exact="cn=admin,dc=devopslab,dc=com" write by * read
olcAccess: {3}to * by group/groupOfNames/member="cn=managers,ou=groups,dc=devopslab,dc=com" write by group/groupOfNames/member="cn=developers,ou=groups,dc=devopslab,dc=com" read by * none
```
**ACL 說明：**
*   **密碼修改**：使用者可以修改自己的密碼。
*   **匿名讀取 Base DN**：允許匿名使用者讀取 Base DN (例如，用於查找架構)。
*   **管理員權限**：`cn=admin,dc=devopslab,dc=com` 擁有所有條目的讀寫權限。
*   **群組權限**：
    *   `managers` 群組的成員擁有所有條目的讀寫權限。
    *   `developers` 群組的成員擁有所有條目的讀取權限，但無寫入權限。
    *   其他所有使用者無權限。

### 1.6 建立 `scripts/setup.sh`

此腳本將自動化憑證生成、服務啟動和 LDIF 檔案導入。在 `scripts` 目錄下建立 `setup.sh`：

```bash
#!/bin/bash

# Exit immediately if a command exits with a non-zero status.
set -e

echo "Starting OpenLDAP setup..."

# 1. Generate self-signed certificates for TLS/SSL
echo "Generating self-signed certificates..."
mkdir -p certs
cd certs

# Generate CA key and certificate
openssl genrsa -out ca.key 2048
openssl req -new -x509 -nodes -days 365 -key ca.key -out ca.crt -subj "/CN=DevOpsLab CA"

# Generate OpenLDAP server key and certificate signing request
openssl genrsa -out openldap.key 2048
openssl req -new -nodes -key openldap.key -out openldap.csr -subj "/CN=openldap.devopslab.com"

# Sign the OpenLDAP server certificate with the CA
openssl x509 -req -days 365 -in openldap.csr -CA ca.crt -CAkey ca.key -CAcreateserial -out openldap.crt

echo "Certificates generated in ./certs directory."
cd ..

# 2. Start the OpenLDAP and phpLDAPadmin containers
echo "Starting Docker Compose services..."
docker-compose up -d openldap phpldapadmin

# 3. Wait for the OpenLDAP server to be ready
echo "Waiting for OpenLDAP server to be ready..."
# Loop until ldapsearch can connect
for i in $(seq 1 30); do
  docker exec openldap ldapsearch -x -H ldap://localhost -b "" -s base > /dev/null 2>&1
  if [ $? -eq 0 ]; then
    echo "OpenLDAP server is ready."
    break
  fi
  echo "Waiting for OpenLDAP... ($i/30)"
  sleep 2
done

# Check if the server is actually ready after the loop
docker exec openldap ldapsearch -x -H ldap://localhost -b "" -s base
if [ $? -ne 0 ]; then
  echo "Error: OpenLDAP server did not become ready in time."
  exit 1
fi

# 4. Import the users_and_groups.ldif file
echo "Importing users_and_groups.ldif..."
docker exec -it openldap ldapadd -x -D "cn=admin,dc=devopslab,dc=com" -w "adminpassword" -H ldap://localhost -f /etc/ldap/ldif/users_and_groups.ldif

# 5. Import the acl.ldif file (for olcAccess configuration)
echo "Importing acl.ldif for access control..."
docker exec -it openldap ldapmodify -x -D "cn=admin,dc=devopslab,dc=com" -w "adminpassword" -H ldap://localhost -f /etc/ldap/ldif/acl.ldif

echo "OpenLDAP setup complete!"
echo "You can access phpLDAPadmin at http://localhost:8080"
```

確保 `setup.sh` 具有執行權限：
```bash
chmod +x scripts/setup.sh
```

### 1.7 建立 `scripts/verify.sh`

此腳本將執行所有要求的驗證步驟。在 `scripts` 目錄下建立 `verify.sh`：

```bash
#!/bin/bash

# Exit immediately if a command exits with a non-zero status.
set -e

echo "Starting OpenLDAP verification..."

# --- 1. Check Docker container status ---
echo "--- 1. Checking Docker container status ---"
if docker-compose ps openldap | grep -q "Up"; then
  echo "OpenLDAP container is up and running."
else
  echo "Error: OpenLDAP container is not running."
  exit 1
fi

if docker-compose ps phpldapadmin | grep -q "Up"; then
  echo "phpLDAPadmin container is up and running."
else
  echo "Error: phpLDAPadmin container is not running."
  exit 1
fi

# --- 2. LDAP Connectivity Test (unencrypted, port 389) ---
echo "--- 2. Performing LDAP Connectivity Test (port 389) ---"
docker exec openldap ldapsearch -x -H ldap://localhost:389 -b "dc=devopslab,dc=com" -s base "objectClass=*" > /dev/null
if [ $? -eq 0 ]; then
  echo "LDAP connectivity on port 389 is successful."
else
  echo "Error: LDAP connectivity on port 389 failed."
  exit 1
fi

# --- 3. Admin Bind Test ---
echo "--- 3. Performing Admin Bind Test ---"
docker exec openldap ldapsearch -x -H ldap://localhost:389 -D "cn=admin,dc=devopslab,dc=com" -w "adminpassword" -b "dc=devopslab,dc=com" -s base "objectClass=*" > /dev/null
if [ $? -eq 0 ]; then
  echo "Admin bind test successful."
else
  echo "Error: Admin bind test failed."
  exit 1
fi

# --- 4. User Query Test ---
echo "--- 4. Performing User Query Test ---"
docker exec openldap ldapsearch -x -H ldap://localhost:389 -D "cn=admin,dc=devopslab,dc=com" -w "adminpassword" -b "ou=users,dc=devopslab,dc=com" "uid=john.doe" | grep "dn: cn=john.doe" > /dev/null
if [ $? -eq 0 ]; then
  echo "User 'john.doe' found successfully."
else
  echo "Error: User 'john.doe' not found."
  exit 1
fi

# --- 5. TLS/SSL Connection Test (ldaps, port 636) ---
echo "--- 5. Performing TLS/SSL Connection Test (port 636) ---"
# Temporarily trust the self-signed CA for this test
export LDAPTLS_CACERT=./certs/ca.crt
docker exec openldap ldapsearch -x -H ldaps://localhost:636 -D "cn=admin,dc=devopslab,dc=com" -w "adminpassword" -b "dc=devopslab,dc=com" -s base "objectClass=*" > /dev/null
if [ $? -eq 0 ]; then
  echo "TLS/SSL (LDAPS) connection successful on port 636."
else
  echo "Error: TLS/SSL (LDAPS) connection failed on port 636."
  exit 1
fi
unset LDAPTLS_CACERT # Unset the variable

# --- 6. ACL Test ---
echo "--- 6. Performing ACL Test ---"
echo "  - Testing developer read access to other user's entry (john.doe trying to read jane.smith)"
docker exec openldap ldapsearch -x -H ldap://localhost:389 -D "cn=john.doe,ou=users,dc=devopslab,dc=com" -w "password" -b "ou=users,dc=devopslab,dc=com" "uid=jane.smith" | grep "dn: cn=jane.smith" > /dev/null
if [ $? -eq 0 ]; then
  echo "  - Developer 'john.doe' has read access to other user's entry."
else
  echo "  - Error: Developer 'john.doe' does not have read access to other user's entry."
  exit 1
fi

echo "  - Testing developer write access failure (john.doe trying to modify jane.smith's description)"
if docker exec openldap ldapmodify -x -D "cn=john.doe,ou=users,dc=devopslab,dc=com" -w "password" -H ldap://localhost <<EOF 2>&1 | grep -q "Insufficient access" ; then
dn: cn=jane.smith,ou=users,dc=devopslab,dc=com
changetype: modify
add: description
description: Test Description
EOF
  echo "  - Developer 'john.doe' correctly denied write access to other user's entry."
else
  echo "  - Error: Developer 'john.doe' unexpectedly gained write access to other user's entry, or test failed."
  exit 1
fi

echo "All verification steps completed successfully!"
```

確保 `verify.sh` 具有執行權限：
```bash
chmod +x scripts/verify.sh
```

## 步驟 2: 啟動環境

在專案根目錄下，執行設定腳本：

```bash
./scripts/setup.sh
```

此腳本將自動執行以下操作：
1.  生成 TLS/SSL 自簽憑證並存放到 `certs/` 目錄。
2.  啟動 `openldap` 和 `phpldapadmin` Docker 容器。
3.  等待 OpenLDAP 服務啟動。
4.  將 `ldif/users_and_groups.ldif` 導入 OpenLDAP，建立範例使用者和群組。
5.  將 `ldif/acl.ldif` 導入 OpenLDAP，配置 ACL 權限。

當腳本執行完成後，您會看到 "OpenLDAP setup complete!" 的訊息。

## 步驟 3: 驗證環境

環境啟動並配置完成後，您可以執行驗證腳本來確認所有功能是否正常：

```bash
./scripts/verify.sh
```

此腳本將執行以下驗證：
1.  檢查 `openldap` 和 `phpldapadmin` Docker 容器的運行狀態。
2.  測試未加密的 LDAP 連線 (Port 389)。
3.  測試管理員帳號的綁定 (Bind) 操作。
4.  測試查詢範例使用者 `john.doe`。
5.  測試 TLS/SSL 加密的 LDAP 連線 (LDAPS, Port 636)。
6.  測試 ACL 權限：
    *   驗證 `john.doe` (developer 群組) 是否具有讀取其他使用者條目的權限。
    *   驗證 `john.doe` (developer 群組) 嘗試修改其他使用者條目時是否被拒絕寫入權限。

如果所有測試都通過，您將看到 "All verification steps completed successfully!" 的訊息。

## 步驟 4: 存取 phpLDAPadmin

您可以在瀏覽器中透過 `http://localhost:8080` 存取 phpLDAPadmin 網頁介面。

使用以下憑證登入：
*   **Login DN:** `cn=admin,dc=devopslab,dc=com`
*   **Password:** `adminpassword` (請記住，實際部署應使用您在 `docker-compose.yml` 中設定的安全密碼)

在 phpLDAPadmin 中，您可以瀏覽、修改和管理您的 LDAP 目錄。

## 步驟 5: 停止和清理環境

當您完成開發或測試後，可以使用以下指令停止並移除 Docker 容器和網路：

```bash
docker-compose down
```

如果您希望移除所有持久化資料和憑證，可以執行：

```bash
docker-compose down --volumes
rm -rf certs data
```

**重要提示**：在實際生產環境中，請務必更換所有預設密碼，並使用由受信任憑證機構 (CA) 簽發的有效憑證，而非自簽憑證。

---
本指南已完成。如果您有其他問題或需要進一步的修改，請隨時告知。
