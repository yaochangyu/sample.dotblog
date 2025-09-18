using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Lab.QueueApi.Tests;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // 設置測試專用的服務配置
            // 例如：替換為測試用的記憶體資料庫或模擬服務

            // 可以在這裡設置測試專用的服務配置
            // 例如：替換為測試用的記憶體資料庫或模擬服務
        });

        builder.UseEnvironment("Testing");
    }
}

