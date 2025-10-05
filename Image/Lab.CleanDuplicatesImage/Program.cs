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
        // 使用 ConcurrentDictionary 處理並發更新
        var hashGroups = new ConcurrentDictionary<string, List<string>>(
            existingHashes.Select(kvp => new KeyValuePair<string, List<string>>(kvp.Key, kvp.Value))
        );

        var totalFilesProcessed = 0;
        var duplicateGroupsCount = hashGroups.Count(g => g.Value.Count > 1);
        var dirtyHashes = new ConcurrentBag<string>();
        var lastWrittenCount = new ConcurrentDictionary<string, int>();

        // 初始化已存在的重複組的計數
        foreach (var kvp in existingHashes.Where(g => g.Value.Count > 1))
        {
            lastWrittenCount[kvp.Key] = kvp.Value.Count;
        }

        const int writeThreshold = 500; // 提高寫入閾值，減少 I/O 次數

        // 列舉檔案
        var files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
            .Where(IsMediaFile)
            .ToArray();

        Console.WriteLine($"找到 {files.Length} 個檔案");
        Console.WriteLine();

        var unprocessedFiles = files.Where(f => !processedFiles.Contains(f)).ToArray();
        var skippedFilesCount = files.Length - unprocessedFiles.Length;

        var processorCount = Environment.ProcessorCount;
        var optimalBatchSize = Math.Max(processorCount * 8, 200); // 動態批次大小
        var batchCount = (int)Math.Ceiling((double)unprocessedFiles.Length / optimalBatchSize);

        // 使用 ConcurrentQueue 累積 Console 輸出
        var consoleBuffer = new ConcurrentQueue<string>();
        var processedFilesSet = new ConcurrentBag<string>();
        var lastProgressUpdate = 0;
        var progressLock = new object();

        for (int i = 0; i < batchCount; i++)
        {
            var startIdx = i * optimalBatchSize;
            var count = Math.Min(optimalBatchSize, unprocessedFiles.Length - startIdx);
            var batch = new ArraySegment<string>(unprocessedFiles, startIdx, count);

            // 平行計算 hash
            var hashResults = batch
                .AsParallel()
                .WithDegreeOfParallelism(processorCount)
                .Select(file =>
                {
                    try
                    {
                        var hash = CalculateSHA256(file);
                        return (file, hash, success: true, error: string.Empty);
                    }
                    catch (Exception ex)
                    {
                        return (file, hash: string.Empty, success: false, error: ex.Message);
                    }
                })
                .ToArray();

            // 處理結果
            var newDuplicateCount = 0;
            foreach (var result in hashResults)
            {
                if (!result.success)
                {
                    consoleBuffer.Enqueue($"處理檔案時發生錯誤 [{result.file}]: {result.error}");
                    continue;
                }

                processedFilesSet.Add(result.file);
                Interlocked.Increment(ref totalFilesProcessed);

                // 使用 AddOrUpdate 減少鎖競爭
                var fileGroup = hashGroups.AddOrUpdate(
                    result.hash,
                    _ => new List<string> { result.file },
                    (_, existingGroup) =>
                    {
                        lock (existingGroup)
                        {
                            existingGroup.Add(result.file);
                        }
                        return existingGroup;
                    }
                );

                int currentCount;
                lock (fileGroup)
                {
                    currentCount = fileGroup.Count;
                }

                if (currentCount >= 2)
                {
                    if (currentCount == 2)
                    {
                        Interlocked.Increment(ref duplicateGroupsCount);
                        newDuplicateCount++;
                        consoleBuffer.Enqueue($"找到第 {duplicateGroupsCount} 組重複檔案 (Hash: {result.hash.Substring(0, Math.Min(16, result.hash.Length))}...)");
                    }

                    // 檢查是否需要寫入
                    var needsWrite = !lastWrittenCount.TryGetValue(result.hash, out var lastCount) || lastCount != currentCount;
                    if (needsWrite)
                    {
                        dirtyHashes.Add(result.hash);
                        lastWrittenCount[result.hash] = currentCount;
                    }
                }
            }

            // 顯示進度
            lock (progressLock)
            {
                if (totalFilesProcessed - lastProgressUpdate >= 100)
                {
                    lastProgressUpdate = totalFilesProcessed;
                    var totalCount = skippedFilesCount + totalFilesProcessed;
                    consoleBuffer.Enqueue($"已處理 {totalCount}/{files.Length} 個檔案 (略過 {skippedFilesCount} 個)...");
                }
            }

            // 輸出累積的訊息
            while (consoleBuffer.TryDequeue(out var message))
            {
                Console.WriteLine(message);
            }

            // 批次寫入
            if (dirtyHashes.Count >= writeThreshold)
            {
                var dirtyHashList = dirtyHashes.ToArray();
                dirtyHashes = new ConcurrentBag<string>();

                var pendingWrites = new Dictionary<string, List<string>>(dirtyHashList.Length);
                foreach (var hash in dirtyHashList.Distinct())
                {
                    if (hashGroups.TryGetValue(hash, out var group))
                    {
                        lock (group)
                        {
                            pendingWrites[hash] = new List<string>(group);
                        }
                    }
                }

                WriteToDatabase(pendingWrites);
                Console.WriteLine($"已批次寫入 {pendingWrites.Count} 組重複檔案到資料庫");
            }
        }

        // 更新 processedFiles
        foreach (var file in processedFilesSet)
        {
            processedFiles.Add(file);
        }

        // 輸出剩餘訊息
        while (consoleBuffer.TryDequeue(out var message))
        {
            Console.WriteLine(message);
        }

        // 最後寫入
        if (!dirtyHashes.IsEmpty)
        {
            var dirtyHashList = dirtyHashes.ToArray();
            var pendingWrites = new Dictionary<string, List<string>>(dirtyHashList.Length);

            foreach (var hash in dirtyHashList.Distinct())
            {
                if (hashGroups.TryGetValue(hash, out var group))
                {
                    lock (group)
                    {
                        pendingWrites[hash] = new List<string>(group);
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
