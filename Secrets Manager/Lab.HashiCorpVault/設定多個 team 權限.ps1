# 設定 Vault 伺服器地址與 Root Token
$vaultAddress = "http://127.0.0.1:8200"
$rootToken = "hvs.KcMWurPzkMwNeOk3c28a6Ceq"

# 登入 Vault
$env:VAULT_ADDR = $vaultAddress
$env:VAULT_TOKEN = $rootToken

# 啟用 kv v2 秘密引擎
Write-Host "Enabling kv v2 secret engine..."
vault secrets enable -path=secret kv-v2

# 建立 Policies
Write-Host "Creating policies..."

$policies = @{
    "team-1" = @"
path "secret/data/db/connection" {
  capabilities = ["read"]
}

path "secret/data/db/connection/data" {
  capabilities = ["read"]
}
"@;
    "team-2" = @"
path "secret/data/db/connection" {
  capabilities = ["read"]
}

path "secret/data/db/connection/data" {
  capabilities = ["read"]
}
"@;
    "team-3" = @"
path "secret/data/db/connection" {
  capabilities = ["read"]
}

path "secret/data/db/connection/data" {
  capabilities = ["read"]
}
"@;
    "team-admin" = @"
path "secret/data/db/connection" {
  capabilities = ["create", "read", "update", "delete", "list"]
}

path "secret/data/db/connection/data" {
  capabilities = ["create", "read", "update", "delete", "list"]
}
"@
}

foreach ($policy in $policies.GetEnumerator()) {
    $policyName = $policy.Key
    $policyContent = $policy.Value
    $policyFile = ".\$policyName-policy.hcl"
    Set-Content -Path $policyFile -Value $policyContent -Encoding UTF8
    vault policy write $policyName $policyFile
    Remove-Item $policyFile
}

# 建立 Secrets
Write-Host "Writing secrets to kv v2..."
vault kv put secret/db/connection key-1=123456 key-2=77889 key-3=0806449

# 建立 Tokens
Write-Host "Creating tokens..."

$tokens = @(
    @{ Name = "team-1"; Policy = "team-1" },
    @{ Name = "team-2"; Policy = "team-2" },
    @{ Name = "team-3"; Policy = "team-3" },
    @{ Name = "team-admin"; Policy = "team-admin" }
)

foreach ($tokenInfo in $tokens) {
    $tokenName = $tokenInfo.Name
    $policyName = $tokenInfo.Policy
    $token = vault token create -policy=$policyName -format="json" | ConvertFrom-Json
    $token.auth.client_token | Set-Content -Path ".\$tokenName-token.txt"
    Write-Host "$tokenName token created and saved to $tokenName-token.txt"
}
