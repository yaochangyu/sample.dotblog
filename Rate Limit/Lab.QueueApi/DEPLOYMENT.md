# 部署指南

本文件提供 Queued Web API 的詳細部署指南，包含開發、測試和生產環境的配置。

## 開發環境部署

### 1. 環境準備

確保已安裝以下軟體：

```bash
# 檢查 .NET 版本
dotnet --version  # 應該是 9.0.x

# 如果未安裝，請安裝 .NET 9 SDK
# Ubuntu/Debian
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y dotnet-sdk-9.0

# Windows
# 從 https://dotnet.microsoft.com/download 下載並安裝

# macOS
brew install --cask dotnet
```

### 2. 專案設定

```bash
# 克隆或下載專案
cd QueuedWebApi

# 還原 NuGet 套件
dotnet restore

# 建置專案
dotnet build

# 執行專案
dotnet run
```

### 3. 驗證部署

```bash
# 檢查健康狀態
curl http://localhost:5001/api/queuedapi/health

# 執行測試腳本
chmod +x test_api.sh
./test_api.sh
```

## 測試環境部署

### 1. 配置檔案調整

建立 `appsettings.Testing.json`：

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "QueuedWebApi": "Debug"
    }
  },
  "RateLimiting": {
    "MaxRequests": 2,
    "TimeWindowMinutes": 1
  },
  "Queue": {
    "Capacity": 100,
    "ProcessingTimeoutMinutes": 2
  }
}
```

### 2. 環境變數設定

```bash
export ASPNETCORE_ENVIRONMENT=Testing
export ASPNETCORE_URLS=http://0.0.0.0:5001
```

### 3. 執行測試環境

```bash
dotnet run --environment Testing
```

## 生產環境部署

### 1. 生產配置

建立 `appsettings.Production.json`：

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "QueuedWebApi": "Information"
    },
    "Console": {
      "IncludeScopes": false
    }
  },
  "AllowedHosts": "*",
  "RateLimiting": {
    "MaxRequests": 2,
    "TimeWindowMinutes": 1
  },
  "Queue": {
    "Capacity": 1000,
    "ProcessingTimeoutMinutes": 5
  }
}
```

### 2. 建置發布版本

```bash
# 建置發布版本
dotnet publish -c Release -o ./publish

# 或建置自包含版本
dotnet publish -c Release -r linux-x64 --self-contained -o ./publish
```

### 3. Docker 部署

建立 `Dockerfile`：

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5001

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["QueuedWebApi.csproj", "."]
RUN dotnet restore "./QueuedWebApi.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "QueuedWebApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "QueuedWebApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "QueuedWebApi.dll"]
```

建立 `docker-compose.yml`：

```yaml
version: '3.8'

services:
  queuedwebapi:
    build: .
    ports:
      - "5001:5001"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:5001
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:5001/api/queuedapi/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
```

部署命令：

```bash
# 建置並啟動
docker-compose up -d

# 檢查狀態
docker-compose ps

# 查看日誌
docker-compose logs -f queuedwebapi
```

### 4. Linux 服務部署

建立 systemd 服務檔案 `/etc/systemd/system/queuedwebapi.service`：

```ini
[Unit]
Description=Queued Web API
After=network.target

[Service]
Type=notify
ExecStart=/usr/bin/dotnet /opt/queuedwebapi/QueuedWebApi.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=queuedwebapi
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://0.0.0.0:5001
WorkingDirectory=/opt/queuedwebapi

[Install]
WantedBy=multi-user.target
```

部署步驟：

```bash
# 複製檔案到部署目錄
sudo mkdir -p /opt/queuedwebapi
sudo cp -r ./publish/* /opt/queuedwebapi/
sudo chown -R www-data:www-data /opt/queuedwebapi

# 啟用並啟動服務
sudo systemctl daemon-reload
sudo systemctl enable queuedwebapi
sudo systemctl start queuedwebapi

# 檢查狀態
sudo systemctl status queuedwebapi

# 查看日誌
sudo journalctl -u queuedwebapi -f
```

### 5. Nginx 反向代理

建立 Nginx 配置 `/etc/nginx/sites-available/queuedwebapi`：

```nginx
server {
    listen 80;
    server_name your-domain.com;
    
    location / {
        proxy_pass http://localhost:5001;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
        
        # 超時設定
        proxy_connect_timeout 60s;
        proxy_send_timeout 60s;
        proxy_read_timeout 60s;
    }
    
    # 健康檢查端點
    location /health {
        proxy_pass http://localhost:5001/api/queuedapi/health;
        access_log off;
    }
}
```

啟用配置：

```bash
sudo ln -s /etc/nginx/sites-available/queuedwebapi /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx
```

## 監控和維護

### 1. 健康檢查

設定定期健康檢查：

```bash
# 建立健康檢查腳本
cat > /opt/scripts/health-check.sh << 'EOF'
#!/bin/bash
HEALTH_URL="http://localhost:5001/api/queuedapi/health"
RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" $HEALTH_URL)

if [ $RESPONSE -eq 200 ]; then
    echo "$(date): Health check passed"
    exit 0
else
    echo "$(date): Health check failed with status $RESPONSE"
    exit 1
fi
EOF

chmod +x /opt/scripts/health-check.sh

# 設定 cron 任務
echo "*/5 * * * * /opt/scripts/health-check.sh >> /var/log/queuedwebapi-health.log 2>&1" | crontab -
```

### 2. 日誌輪轉

設定 logrotate `/etc/logrotate.d/queuedwebapi`：

```
/var/log/queuedwebapi*.log {
    daily
    missingok
    rotate 30
    compress
    delaycompress
    notifempty
    create 644 www-data www-data
    postrotate
        systemctl reload queuedwebapi
    endscript
}
```

### 3. 效能監控

建立監控腳本：

```bash
cat > /opt/scripts/monitor.sh << 'EOF'
#!/bin/bash
API_URL="http://localhost:5001/api/queuedapi/health"
RESPONSE=$(curl -s $API_URL)

QUEUE_LENGTH=$(echo $RESPONSE | jq -r '.queueLength')
CAN_ACCEPT=$(echo $RESPONSE | jq -r '.canAcceptRequest')
RETRY_AFTER=$(echo $RESPONSE | jq -r '.retryAfterSeconds')

echo "$(date): Queue Length: $QUEUE_LENGTH, Can Accept: $CAN_ACCEPT, Retry After: $RETRY_AFTER"

# 警報條件
if [ $QUEUE_LENGTH -gt 50 ]; then
    echo "WARNING: Queue length is high: $QUEUE_LENGTH"
fi
EOF

chmod +x /opt/scripts/monitor.sh
```

## 故障排除

### 常見問題

1. **端口被佔用**
```bash
# 檢查端口使用情況
sudo netstat -tulpn | grep :5001
# 或
sudo lsof -i :5001

# 終止佔用端口的程序
sudo kill -9 <PID>
```

2. **權限問題**
```bash
# 確保正確的檔案權限
sudo chown -R www-data:www-data /opt/queuedwebapi
sudo chmod +x /opt/queuedwebapi/QueuedWebApi
```

3. **記憶體不足**
```bash
# 檢查記憶體使用情況
free -h
ps aux | grep QueuedWebApi

# 調整服務配置限制記憶體使用
# 在 systemd 服務檔案中添加：
# MemoryLimit=512M
```

4. **日誌檢查**
```bash
# 檢查應用程式日誌
sudo journalctl -u queuedwebapi -n 100

# 檢查 Nginx 日誌
sudo tail -f /var/log/nginx/error.log
sudo tail -f /var/log/nginx/access.log
```

### 效能調優

1. **調整限流參數**
```csharp
// 在 Program.cs 中調整
builder.Services.AddSingleton<IRateLimiter>(provider => 
    new SlidingWindowRateLimiter(
        maxRequests: 5,                    // 增加到每分鐘 5 個請求
        timeWindow: TimeSpan.FromMinutes(1)
    ));
```

2. **調整佇列容量**
```csharp
builder.Services.AddSingleton<IRequestQueue>(provider => 
    new ChannelRequestQueue(capacity: 1000)); // 增加佇列容量
```

3. **調整超時設定**
```csharp
// 在控制器中調整等待超時
var response = await _requestQueue.WaitForResponseAsync(requestId, TimeSpan.FromMinutes(5));
```

## 安全考慮

1. **HTTPS 配置**
2. **API 金鑰驗證**
3. **速率限制**
4. **輸入驗證**
5. **日誌安全**

詳細的安全配置請參考 ASP.NET Core 安全最佳實踐。

