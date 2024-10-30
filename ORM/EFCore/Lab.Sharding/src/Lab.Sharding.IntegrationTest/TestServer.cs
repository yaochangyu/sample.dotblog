using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace JobBank1111.Job.WebAPI.IntegrationTest;

public class TestServer(DateTimeOffset now, 
                        string userId)
    : WebApplicationFactory<Program>
{
    private void ConfigureServices(IServiceCollection services)
    {
        //模擬身分
        services.AddFakeContextAccessor(userId);
        
        //模擬現在時間
        var fakeTimeProvider = new FakeTimeProvider(now);
        services.AddSingleton<TimeProvider>(fakeTimeProvider);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(this.ConfigureServices);
    }
}