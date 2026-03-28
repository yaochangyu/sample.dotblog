# Lab.SessionCacheProvider 專案結構

```
Lab.SessionCacheProvider/
├── Lab.SessionCacheProvider.slnx
├── @tree.md
├── archived/
│   ├── session-cache-provider.plan.md
│   └── bdd-test.plan.md
├── Lab.SessionCacheProvider/
│   ├── Lab.SessionCacheProvider.csproj
│   ├── ICookieAccessor.cs
│   ├── AspNetCookieAccessor.cs
│   ├── AspNetCoreCookieAccessor.cs
│   ├── SessionObject.cs
│   ├── SessionCacheProvider.cs
│   └── SessionCacheProviderExtensions.cs
└── Lab.SessionCacheProvider.Tests/
    ├── Lab.SessionCacheProvider.Tests.csproj
    ├── Features/
    │   ├── SessionObject.feature
    │   └── SessionCacheProvider.feature
    ├── StepDefinitions/
    │   ├── SessionObjectStepDefinitions.cs
    │   └── SessionCacheProviderStepDefinitions.cs
    └── Support/
        └── FakeCookieAccessor.cs
```
