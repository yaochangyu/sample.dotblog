using System.Collections.Concurrent;
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
        var hashGroups = existingHashes;
        var totalFilesProcessed = 0;
        var skippedFilesCount = 0;
        var duplicateGroupsCount = hashGroups.Count(g => g.Value.Count > 1);

        // 使用 ConcurrentDictionary 避免 lock 競爭
        var dirtyHashes = new ConcurrentDictionary<string, byte>();
        var lastWrittenCount = new ConcurrentDictionary<string, int>();

        // 初始化已存在的重複組的計數
        foreach (var kvp in existingHashes.Where(g => g.Value.Count > 1))
        {
            lastWrittenCount[kvp.Key] = kvp.Value.Count;
        }

        const int batchSize = 100;
        const int writeThreshold = batchSize; // 預先計算閾值

        // 移除不必要的 PLINQ，檔案列舉不需要平行化
        var files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
            .Where(IsMediaFile)
            .ToArray(); // 使用陣列避免重複列舉

        Console.WriteLine($"找到 {files.Length} 個檔案");
        Console.WriteLine();

        // 使用 HashSet.Except 減少記憶體配置
        var unprocessedFiles = files.Where(f => !processedFiles.Contains(f)).ToArray();
        skippedFilesCount = files.Length - unprocessedFiles.Length;

        var processorCount = Environment.ProcessorCount;
        var batchCount = (int)Math.Ceiling((double)unprocessedFiles.Length / (processorCount * 4));

        // 使用物件池減少字串配置
        var hashGroupsLock = new object();
        var consoleWriteLock = new object();

        for (int i = 0; i < batchCount; i++)
        {
            var startIdx = i * processorCount * 4;
            var count = Math.Min(processorCount * 4, unprocessedFiles.Length - startIdx);
            var batch = new ArraySegment<string>(unprocessedFiles, startIdx, count);

            // 平行計算 hash，先處理所有 I/O 密集操作
            var hashResults = batch
                .AsParallel()
                .WithDegreeOfParallelism(processorCount)
                .Select(file =>
                {
                    try
                    {
                        var hash = CalculateSHA256(file);
                        return (file, hash, success: true);
                    }
                    catch (Exception ex)
                    {
                        // 避免在平行迴圈內寫 Console，累積後批次輸出
                        return (file, hash: ex.Message, success: false);
                    }
                })
                .ToArray(); // 使用陣列避免多次列舉

            // 收集錯誤訊息，批次輸出
            var errors = hashResults.Where(r => !r.success).ToArray();
            if (errors.Length > 0)
            {
                lock (consoleWriteLock)
                {
                    foreach (var (file, errorMsg, _) in errors)
                    {
                        Console.WriteLine($"處理檔案時發生錯誤 [{file}]: {errorMsg}");
                    }
                }
            }

            // 縮小 lock 範圍，只鎖必要的寫入操作
            var successResults = hashResults.Where(r => r.success).ToArray();
            var newDuplicateGroups = new List<string>();

            lock (hashGroupsLock)
            {
                foreach (var (file, hash, _) in successResults)
                {
                    totalFilesProcessed++;
                    processedFiles.Add(file);

                    if (!hashGroups.TryGetValue(hash, out var fileGroup))
                    {
                        fileGroup = new List<string>();
                        hashGroups[hash] = fileGroup;
                    }

                    fileGroup.Add(file);
                    var currentCount = fileGroup.Count;

                    if (currentCount >= 2)
                    {
                        if (currentCount == 2)
                        {
                            duplicateGroupsCount++;
                            newDuplicateGroups.Add(hash); // 收集後批次輸出
                        }

                        // 使用 ConcurrentDictionary 的原子操作
                        if (!lastWrittenCount.TryGetValue(hash, out var lastCount) || lastCount != currentCount)
                        {
                            dirtyHashes.TryAdd(hash, 0);
                            lastWrittenCount[hash] = currentCount;
                        }
                    }
                }
            }

            // 批次輸出新找到的重複組
            if (newDuplicateGroups.Count > 0)
            {
                lock (consoleWriteLock)
                {
                    foreach (var hash in newDuplicateGroups)
                    {
                        var groupNum = duplicateGroupsCount - newDuplicateGroups.Count + newDuplicateGroups.IndexOf(hash) + 1;
                        Console.WriteLine($"找到第 {groupNum} 組重複檔案 (Hash: {hash.Substring(0, Math.Min(16, hash.Length))}...)");
                    }
                }
            }

            // 顯示進度 (移到 lock 外)
            var currentTotal = Interlocked.Add(ref totalFilesProcessed, 0); // 安全讀取
            if (currentTotal % 100 <= successResults.Length && currentTotal >= 100)
            {
                lock (consoleWriteLock)
                {
                    var totalCount = skippedFilesCount + currentTotal;
                    Console.WriteLine($"已處理 {totalCount}/{files.Length} 個檔案 (略過 {skippedFilesCount} 個)...");
                }
            }

            // 批次寫入：只寫入有變動的重複組
            if (dirtyHashes.Count >= writeThreshold)
            {
                var pendingWrites = new Dictionary<string, List<string>>(dirtyHashes.Count);
                lock (hashGroupsLock)
                {
                    foreach (var hash in dirtyHashes.Keys)
                    {
                        if (hashGroups.TryGetValue(hash, out var group))
                        {
                            pendingWrites[hash] = group;
                        }
                    }
                }

                WriteToDatabase(pendingWrites);

                lock (consoleWriteLock)
                {
                    Console.WriteLine($"已批次寫入 {pendingWrites.Count} 組重複檔案到資料庫");
                }

                dirtyHashes.Clear();
            }
        }

        // 最後將剩餘的資料寫入資料庫
        if (dirtyHashes.Count > 0)
        {
            var pendingWrites = new Dictionary<string, List<string>>(dirtyHashes.Count);
            lock (hashGroupsLock)
            {
                foreach (var hash in dirtyHashes.Keys)
                {
                    if (hashGroups.TryGetValue(hash, out var group))
                    {
                        pendingWrites[hash] = group;
                    }
                }
            }

            WriteToDatabase(pendingWrites);
            Console.WriteLine($"已批次寫入最後 {pendingWrites.Count} 組重複檔案到資料庫");
        }

        Console.WriteLine();
        Console.WriteLine($"總共掃描了 {files.Length} 個檔案");
        Console.WriteLine($"略過已處理的 {skippedFilesCount} 個檔案");
        Console.WriteLine($"新處理了 {totalFilesProcessed} 個檔案");
        Console.WriteLine($"找到 {duplicateGroupsCount} 組重複檔案");
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
            INSERT INTO DuplicateFiles (Hash, FilePath, FileCount, CreatedAt)
            VALUES ($hash, $filePath, $fileCount, $createdAt)
            ON CONFLICT(Hash, FilePath) DO UPDATE SET
                FileCount = excluded.FileCount,
                CreatedAt = excluded.CreatedAt
        ";

        var hashParam = command.CreateParameter();
        hashParam.ParameterName = "$hash";
        command.Parameters.Add(hashParam);

        var filePathParam = command.CreateParameter();
        filePathParam.ParameterName = "$filePath";
        command.Parameters.Add(filePathParam);

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
                hashParam.Value = hash;
                filePathParam.Value = filePath;
                fileCountParam.Value = files.Count;
                createdAtParam.Value = createdAt;

                command.ExecuteNonQuery();
            }
        }

        transaction.Commit();
    }
}
