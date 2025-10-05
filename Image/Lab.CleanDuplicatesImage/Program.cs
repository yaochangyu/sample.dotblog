using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;

namespace Lab.CleanDuplicatesImage;

class Program
{
    // 支援的圖片副檔名
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff", ".tif", ".ico", ".svg",
        ".heic", ".heif", ".raw", ".cr2", ".nef", ".arw", ".dng"
    };

    // 支援的影片副檔名
    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm", ".m4v", ".mpg", ".mpeg"
    };

    // SQLite 資料庫檔案名稱
    private const string DatabaseFileName = "duplicates.db";

    static void Main(string[] args)
    {
        // 設定 Console 編碼為 UTF-8
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        Console.WriteLine("=== 重複檔案掃描工具 (優化版) ===");
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
            // 初始化資料庫
            InitializeDatabase();

            // 讀取已存在的重複檔案資料
            var (existingHashes, processedFiles) = LoadExistingHashes();
            Console.WriteLine($"從資料庫載入 {existingHashes.Count} 筆現有雜湊值");
            Console.WriteLine($"已處理 {processedFiles.Count} 個檔案");
            Console.WriteLine();

            ScanAndWriteDuplicates(folderPath, existingHashes, processedFiles);
            Console.WriteLine();
            Console.WriteLine("掃描完成！");
            Console.WriteLine($"結果已儲存至: {DatabaseFileName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"發生錯誤: {ex.Message}");
        }
    }

    static void ScanAndWriteDuplicates(string folderPath, Dictionary<string, List<string>> existingHashes, HashSet<string> processedFiles)
    {
        const int writeThreshold = 500;
        const int progressInterval = 100;

        // 初始化資料結構
        var hashGroups = new Dictionary<string, List<string>>(existingHashes);
        var dirtyHashes = new HashSet<string>();
        var lastWrittenCount = InitializeWrittenCount(existingHashes);

        // 掃描檔案
        var (allFiles, unprocessedFiles) = ScanFiles(folderPath, processedFiles);
        var skippedCount = allFiles.Length - unprocessedFiles.Length;

        Console.WriteLine($"找到 {allFiles.Length} 個檔案 (略過 {skippedCount} 個已處理)");
        Console.WriteLine();

        // 處理檔案
        var existingDuplicateGroups = hashGroups.Count(g => g.Value.Count > 1);
        var stats = new ProcessingStats
        {
            SkippedCount = skippedCount,
            DuplicateGroupsCount = existingDuplicateGroups
        };

        foreach (var file in unprocessedFiles)
        {
            ProcessFile(file, hashGroups, processedFiles, dirtyHashes, lastWrittenCount, stats);

            // 顯示進度
            ShowProgress(stats, allFiles.Length, progressInterval);

            // 批次寫入
            if (dirtyHashes.Count >= writeThreshold)
            {
                WriteBatch(dirtyHashes, hashGroups);
            }
        }

        // 最後寫入剩餘資料
        if (dirtyHashes.Count > 0)
        {
            WriteBatch(dirtyHashes, hashGroups, isLast: true);
        }

        // 顯示統計
        PrintSummary(stats, allFiles.Length);
    }

    private class ProcessingStats
    {
        public int ProcessedCount { get; set; }
        public int DuplicateGroupsCount { get; set; }
        public int SkippedCount { get; set; }
        public int LastProgressUpdate { get; set; }
    }

    static Dictionary<string, int> InitializeWrittenCount(Dictionary<string, List<string>> existingHashes)
    {
        var writtenCount = new Dictionary<string, int>();
        foreach (var kvp in existingHashes.Where(g => g.Value.Count > 1))
        {
            writtenCount[kvp.Key] = kvp.Value.Count;
        }
        return writtenCount;
    }

    static (string[] allFiles, string[] unprocessedFiles) ScanFiles(string folderPath, HashSet<string> processedFiles)
    {
        var allFiles = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
            .Where(IsMediaFile)
            .ToArray();

        var unprocessedFiles = allFiles.Where(f => !processedFiles.Contains(f)).ToArray();

        return (allFiles, unprocessedFiles);
    }

    static void ProcessFile(
        string file,
        Dictionary<string, List<string>> hashGroups,
        HashSet<string> processedFiles,
        HashSet<string> dirtyHashes,
        Dictionary<string, int> lastWrittenCount,
        ProcessingStats stats)
    {
        try
        {
            var hash = CalculateSHA256(file);
            processedFiles.Add(file);
            stats.ProcessedCount++;

            // 更新 hash 群組
            if (!hashGroups.TryGetValue(hash, out var fileList))
            {
                fileList = new List<string>();
                hashGroups[hash] = fileList;
            }
            fileList.Add(file);

            // 處理重複檔案
            if (fileList.Count >= 2)
            {
                if (fileList.Count == 2)
                {
                    stats.DuplicateGroupsCount++;
                }

                // 標記需要寫入的 hash
                if (!lastWrittenCount.TryGetValue(hash, out var lastCount) || lastCount != fileList.Count)
                {
                    dirtyHashes.Add(hash);
                    lastWrittenCount[hash] = fileList.Count;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"處理檔案時發生錯誤 [{file}]: {ex.Message}");
        }
    }

    static void ShowProgress(ProcessingStats stats, int totalFiles, int interval)
    {
        var currentTotal = stats.SkippedCount + stats.ProcessedCount;
        if (currentTotal - stats.LastProgressUpdate >= interval)
        {
            stats.LastProgressUpdate = currentTotal;
            Console.WriteLine($"已處理 {currentTotal}/{totalFiles} 個檔案 (略過 {stats.SkippedCount} 個)...");
        }
    }

    static void WriteBatch(HashSet<string> dirtyHashes, Dictionary<string, List<string>> hashGroups, bool isLast = false)
    {
        var pendingWrites = new Dictionary<string, List<string>>(dirtyHashes.Count);
        foreach (var hash in dirtyHashes)
        {
            pendingWrites[hash] = new List<string>(hashGroups[hash]);
        }

        WriteToDatabase(pendingWrites);
        Console.WriteLine($"已批次寫入{(isLast ? "最後 " : "")}{pendingWrites.Count} 組重複檔案到資料庫");
        dirtyHashes.Clear();
    }

    static void PrintSummary(ProcessingStats stats, int totalFiles)
    {
        Console.WriteLine();
        Console.WriteLine($"總共掃描了 {totalFiles} 個檔案");
        Console.WriteLine($"略過已處理的 {stats.SkippedCount} 個檔案");
        Console.WriteLine($"新處理了 {stats.ProcessedCount} 個檔案");
        Console.WriteLine($"找到 {stats.DuplicateGroupsCount} 組重複檔案");
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
    /// 載入資料庫中已存在的雜湊值和檔案路徑
    /// </summary>
    static (Dictionary<string, List<string>> hashGroups, HashSet<string> processedFiles) LoadExistingHashes()
    {
        var hashGroups = new Dictionary<string, List<string>>();
        var processedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!File.Exists(DatabaseFileName))
        {
            return (hashGroups, processedFiles);
        }

        using var connection = new SqliteConnection($"Data Source={DatabaseFileName}");
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Hash, FilePath FROM DuplicateFiles";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var hash = reader.GetString(0);
            var filePath = reader.GetString(1);

            // 記錄所有已處理的檔案路徑
            processedFiles.Add(filePath);

            if (!hashGroups.ContainsKey(hash))
            {
                hashGroups[hash] = new List<string>();
            }

            // 只加入實際存在的檔案
            if (File.Exists(filePath))
            {
                hashGroups[hash].Add(filePath);
            }
        }

        return (hashGroups, processedFiles);
    }

    /// <summary>
    /// 初始化 SQLite 資料庫
    /// </summary>
    static void InitializeDatabase()
    {
        using var connection = new SqliteConnection($"Data Source={DatabaseFileName}");
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS DuplicateFiles (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Hash TEXT NOT NULL,
                FilePath TEXT NOT NULL,
                FileName TEXT NOT NULL,
                FileSize INTEGER NOT NULL,
                FileCreatedTime TEXT NOT NULL,
                FileCount INTEGER NOT NULL,
                CreatedAt TEXT NOT NULL,
                UNIQUE(Hash, FilePath)
            );

            CREATE INDEX IF NOT EXISTS idx_hash ON DuplicateFiles(Hash);
        ";
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// 將重複檔案資訊寫入 SQLite 資料庫
    /// </summary>
    static void WriteToDatabase(Dictionary<string, List<string>> duplicates)
    {
        using var connection = new SqliteConnection($"Data Source={DatabaseFileName}");
        connection.Open();

        using var transaction = connection.BeginTransaction();

        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO DuplicateFiles (Hash, FilePath, FileName, FileSize, FileCreatedTime, FileCount, CreatedAt)
            VALUES ($hash, $filePath, $fileName, $fileSize, $fileCreatedTime, $fileCount, $createdAt)
            ON CONFLICT(Hash, FilePath) DO UPDATE SET
                FileName = excluded.FileName,
                FileSize = excluded.FileSize,
                FileCreatedTime = excluded.FileCreatedTime,
                FileCount = excluded.FileCount,
                CreatedAt = excluded.CreatedAt
        ";

        var hashParam = command.CreateParameter();
        hashParam.ParameterName = "$hash";
        command.Parameters.Add(hashParam);

        var filePathParam = command.CreateParameter();
        filePathParam.ParameterName = "$filePath";
        command.Parameters.Add(filePathParam);

        var fileNameParam = command.CreateParameter();
        fileNameParam.ParameterName = "$fileName";
        command.Parameters.Add(fileNameParam);

        var fileSizeParam = command.CreateParameter();
        fileSizeParam.ParameterName = "$fileSize";
        command.Parameters.Add(fileSizeParam);

        var fileCreatedTimeParam = command.CreateParameter();
        fileCreatedTimeParam.ParameterName = "$fileCreatedTime";
        command.Parameters.Add(fileCreatedTimeParam);

        var fileCountParam = command.CreateParameter();
        fileCountParam.ParameterName = "$fileCount";
        command.Parameters.Add(fileCountParam);

        var createdAtParam = command.CreateParameter();
        createdAtParam.ParameterName = "$createdAt";
        command.Parameters.Add(createdAtParam);

        var createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        foreach (var group in duplicates)
        {
            var hash = group.Key;
            var files = group.Value;

            foreach (var filePath in files)
            {
                var fileInfo = new FileInfo(filePath);

                hashParam.Value = hash;
                filePathParam.Value = filePath;
                fileNameParam.Value = fileInfo.Name;
                fileSizeParam.Value = fileInfo.Length;
                fileCreatedTimeParam.Value = fileInfo.CreationTime.ToString("yyyy-MM-dd HH:mm:ss");
                fileCountParam.Value = files.Count;
                createdAtParam.Value = createdAt;

                command.ExecuteNonQuery();
            }
        }

        transaction.Commit();
    }
}
