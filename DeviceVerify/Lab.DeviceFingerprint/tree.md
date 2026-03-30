# DeviceVerify 專案結構

```
DeviceVerify/
├── Lab.Device-Fingerprint/
│   └── device-fingerprint.plan.md          # 實作計畫
│
└── Lab.DeviceFingerprint.WebApi/           # ASP.NET Core 10 WebAPI
    ├── Application/
    │   ├── DTOs/
    │   │   └── AuthDtos.cs                 # LoginRequest/Response、VerifyDeviceRequest/Response
    │   └── Services/
    │       ├── IAuthService.cs             # 認證服務介面
    │       └── AuthService.cs             # 認證服務實作（HybridCache OTP）
    ├── Controllers/
    │   ├── AuthController.cs              # POST /api/auth/login、/api/auth/verify-device
    │   └── MeController.cs               # GET /api/me（受保護）
    ├── Domain/
    │   └── Entities/
    │       ├── User.cs
    │       └── UserDevice.cs
    ├── Infrastructure/
    │   ├── Middleware/
    │   │   └── DeviceFingerprintMiddleware.cs  # 比對 X-Device-Fingerprint header 與 JWT claim
    │   └── Persistence/
    │       ├── AppDbContext.cs
    │       └── Migrations/
    │           ├── 20260330154016_InitialCreate.cs
    │           ├── 20260330154016_InitialCreate.Designer.cs
    │           └── AppDbContextModelSnapshot.cs
    ├── wwwroot/
    │   └── index.html                     # 前端測試頁面（FingerprintJS）
    ├── appsettings.json
    ├── appsettings.Development.json
    ├── docker-compose.yml                 # PostgreSQL 16 + Redis 7
    ├── Program.cs
    └── Lab.DeviceFingerprint.WebApi.csproj
```
