namespace Lab.XmlSerializer.ConsoleApp
{
    // 記憶體監控工具
    public class MemoryMonitor
    {
        private long _initialMemory;
        private readonly string _testName;

        public MemoryMonitor(string testName)
        {
            _testName = testName;
            // // 強制垃圾回收，確保測試準確性
            // GC.Collect();
            // GC.WaitForPendingFinalizers();
            // GC.Collect();

            // _initialMemory = GC.GetTotalMemory(false);
            Console.WriteLine($"\n=== {_testName} 開始 ===");
            Console.WriteLine($"初始記憶體: {_initialMemory / 1024.0 / 1024.0:F2} MB");
        }

        public void ShowCurrentMemory(string checkpoint)
        {
            var currentMemory = GC.GetTotalMemory(false);
            var difference = currentMemory - _initialMemory;
            Console.WriteLine($"{checkpoint}: {currentMemory / 1024.0 / 1024.0:F2} MB " +
                              $"(增加: {difference / 1024.0 / 1024.0:F2} MB)");
        }

        public void Finish()
        {
            // // 強制垃圾回收
            // GC.Collect();
            // GC.WaitForPendingFinalizers();
            // GC.Collect();
            //
            // var finalMemory = GC.GetTotalMemory(false);
            // var totalIncrease = finalMemory - _initialMemory;
            //
            // Console.WriteLine($"垃圾回收後記憶體: {finalMemory / 1024.0 / 1024.0:F2} MB");
            // Console.WriteLine($"總記憶體增加: {totalIncrease / 1024.0 / 1024.0:F2} MB");
            // Console.WriteLine($"=== {_testName} 結束 ===\n");
        }
    }

}
