# Lab.SessionCacheProvider 專案結構

```
Lab.SessionCacheProvider/
├── Lab.SessionCacheProvider.slnx
├── @tree.md
├── archived/
│   ├── session-cache-provider.plan.md
│   ├── bdd-test.plan.md
│   └── remove-httpcontext-current.plan.md
├── Lab.SessionCacheProvider/
│   ├── Lab.SessionCacheProvider.csproj
│   ├── ICookieAccessor.cs
│   ├── AspNetCookieAccessor.cs
│   ├── AspNetCoreCookieAccessor.cs
│   ├── SessionObject.cs
│   ├── SessionCacheProvider.cs
│   ├── CacheSession.cs
│   └── SessionCacheProviderExtensions.cs
└── Lab.SessionCacheProvider.Tests/
    ├── Lab.SessionCacheProvider.Tests.csproj
    ├── Features/
    │   ├── SessionObject.feature
    │   ├── SessionCacheProvider.feature
    │   ├── CacheSession.feature
    │   └── TestServerIntegration.feature
    ├── StepDefinitions/
    │   ├── SessionObjectStepDefinitions.cs
    │   ├── SessionCacheProviderStepDefinitions.cs
    │   ├── CacheSessionStepDefinitions.cs
    │   └── TestServerIntegrationStepDefinitions.cs
    └── Support/
        ├── AssemblyInfo.cs
        ├── FakeCookieAccessor.cs
        └── TestServerFixture.cs
```
