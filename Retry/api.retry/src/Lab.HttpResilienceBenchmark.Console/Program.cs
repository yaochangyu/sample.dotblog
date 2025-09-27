using System.Text;
using BenchmarkDotNet.Running;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

Console.WriteLine("比較 Polly V7 vs V8 vs Resilience...");
BenchmarkRunner.Run<HttpClientBenchmark>();