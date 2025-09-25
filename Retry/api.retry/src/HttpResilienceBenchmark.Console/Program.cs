using System.Text;
using BenchmarkDotNet.Running;
Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;
Console.WriteLine("選擇要執行的基準測試:");
Console.WriteLine("1. 原始測試 (HttpClientBenchmark)");
Console.WriteLine("2. 修正版測試 (FixedHttpClientBenchmark) - 比較 V7 vs V8");
Console.Write("請選擇 (1 或 2): ");

var choice = Console.ReadLine();

if (choice == "2")
{
    Console.WriteLine("執行修正版測試 - 比較 Polly V7 vs V8 vs Resilience...");
    BenchmarkRunner.Run<FixedHttpClientBenchmark>();
}
else
{
    Console.WriteLine("執行原始測試...");
    BenchmarkRunner.Run<HttpClientBenchmark>();
}
