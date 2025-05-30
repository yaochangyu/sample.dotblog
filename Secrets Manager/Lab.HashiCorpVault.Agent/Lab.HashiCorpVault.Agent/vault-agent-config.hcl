# Vault Agent 配置
exit_after_auth = false
pid_file = "./vault-agent.pid"

auto_auth {
    method "approle" {
        mount_path = "auth/approle"
        config = {
            role_id_file_path = "role_id"
            secret_id_file_path = "secret_id"
        }
    }

    sink "file" {
        config = {
            path = "vault-token"
        }
    }
}

# 快取配置
cache {
    use_auto_auth_token = true
}

# 範本配置 - 用於獲取實際的秘密
template {
    source      = "dev-db-config.ctmpl"
    destination = "dev-db-config.json"
}

# template {
#     source      = "prod-db-config.ctmpl"
#     destination = "prod-db-config.json"
# }

vault {
    address = "http://127.0.0.1:8200"
}

listener "tcp" {
    address = "0.0.0.0:8100"
    tls_disable = true
}