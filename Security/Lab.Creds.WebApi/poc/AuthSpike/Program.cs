using AuthSpike.Hosting;

// spike 可執行入口：以測試 PKI 啟動授權伺服器與建立訂單 API，直到 Ctrl+C。
var runtime = await SpikeRuntime.StartAsync(authServerPort: 7443, ordersApiPort: 7444);
Console.WriteLine($"Authorization server: https://localhost:{runtime.AuthServer.Port}/");
Console.WriteLine($"Orders API:           https://localhost:{runtime.OrdersApi.Port}/orders");
Console.WriteLine("Press Ctrl+C to stop.");

var stop = new TaskCompletionSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop.TrySetResult(); };
await stop.Task;
await runtime.DisposeAsync();
