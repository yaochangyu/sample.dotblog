using System.Security.Cryptography;
using System.Text;

namespace Lab.CleanDuplicatesImage;

class Program
{
    // 支援的圖片副檔名
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff", ".tif", ".ico", ".svg"
    };

    // 支援的影片副檔名
    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm", ".m4v", ".mpg", ".mpeg"
    };

    // 批次處理門檻：每累積 100 組重複檔案就寫入一次 CSV
    private const int BatchThreshold = 100;

    static void Main(string[] args)
    {
        // 設定 Console 編碼為 UTF-8
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        Console.WriteLine("=== 重複檔案掃描工具 ===");
        Console.WriteLine();

        // 接收使用者輸入
        Console.Write("請輸入要掃描的資料夾路徑: ");
        var folderPath = Console.ReadLine()?.Trim();

        if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
        {
            Console.WriteLine("錯誤：資料夾路徑無效或不存在！");
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"開始掃描資料夾: {folderPath}");
        Console.WriteLine();

        try
        {
            ScanAndWriteDuplicates(folderPath);
            Console.WriteLine();
            Console.WriteLine("掃描完成！");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"發生錯誤: {ex.Message}");
        }
    }

    static void ScanAndWriteDuplicates(string folderPath)
    {
        var hashGroups = new Dictionary<string, List<string>>();
        var duplicateGroupsFound = 0;
        var batchNumber = 1;
        var totalFilesProcessed = 0;

        // 取得所有符合條件的檔案
        var files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
            .Where(IsMediaFile)
            .ToList();

        Console.WriteLine($"找到 {files.Count} 檔案");
        Console.WriteLine();

        foreach (var file in files)
        {
            try
            {
                totalFilesProcessed++;

                // 顯示進度
                if (totalFilesProcessed % 100 == 0)
                {
                    Console.WriteLine($"已處理 {totalFilesProcessed}/{files.Count} 個檔案...");
                }

                var hash = CalculateSHA256(file);

                if (!hashGroups.ContainsKey(hash))
                {
                    hashGroups[hash] = new List<string>();
                }

                hashGroups[hash].Add(file);

                // 如果這個雜湊值的檔案從 1 個變成 2 個，表示找到新的重複組
                if (hashGroups[hash].Count == 2)
                {
                    duplicateGroupsFound++;
                }

                // 達到批次門檻，寫入 CSV
                if (duplicateGroupsFound >= BatchThreshold)
                {
                    var duplicates = hashGroups.Where(g => g.Value.Count > 1)
                        .ToDictionary(g => g.Key, g => g.Value);

                    WriteToCsv(duplicates, batchNumber);
                    Console.WriteLine($"已將 {duplicates.Count} 組重複檔案寫入 CSV (批次 {batchNumber})");

                    batchNumber++;
                    duplicateGroupsFound = 0;
                    hashGroups.Clear();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"處理檔案時發生錯誤 [{file}]: {ex.Message}");
            }
        }

        // 處理剩餘的重複檔案
        var remainingDuplicates = hashGroups.Where(g => g.Value.Count > 1)
            .ToDictionary(g => g.Key, g => g.Value);

        if (remainingDuplicates.Count > 0)
        {
            WriteToCsv(remainingDuplicates, batchNumber);
            Console.WriteLine($"已將 {remainingDuplicates.Count} 組重複檔案寫入 CSV (批次 {batchNumber})");
        }

        Console.WriteLine();
        Console.WriteLine($"總共處理了 {totalFilesProcessed} 個檔案");
    }

    /// <summary>
    /// 判斷是否為圖片或影片檔案
    /// </summary>
    static bool IsMediaFile(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        return ImageExtensions.Contains(extension) || VideoExtensions.Contains(extension);
    }

    /// <summary>
    /// 計算檔案的 SHA-256 雜湊值
    /// </summary>
    static string CalculateSHA256(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hash = sha256.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    /// <summary>
    /// 將重複檔案資訊寫入 CSV
    /// </summary>
    static void WriteToCsv(Dictionary<string, List<string>> duplicates, int batchNumber)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd");
        var fileName = $"duplicates_{timestamp}_{batchNumber}.csv";

        using var writer = new StreamWriter(fileName, false, Encoding.UTF8);

        // 寫入標題列
        writer.WriteLine("雜湊值,檔案路徑列表");

        foreach (var group in duplicates)
        {
            var hash = group.Key;
            var paths = string.Join(" | ", group.Value);

            // 為了避免 CSV 欄位中的逗號造成問題，將路徑列表用引號包起來
            writer.WriteLine($"{hash},\"{paths}\"");
        }
    }
}
