using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;

namespace Lab.CleanDuplicatesImage;

/// <summary>
/// 檔案記錄資料模型
/// </summary>
/// <param name="Path">檔案路徑</param>
/// <param name="Timestamp">時間戳記（標記時間或略過時間）</param>
/// <param name="Exists">檔案是否存在</param>
/// <param name="Size">檔案大小</param>
/// <param name="CreatedTime">建立時間</param>
/// <param name="ModifiedTime">修改時間</param>
record FileRecord(
    string Path,
    string Timestamp,
    bool Exists,
    long Size,
    DateTime? CreatedTime,
    DateTime? ModifiedTime)
{
    /// <summary>
    /// 從檔案路徑和時間戳記建立 FileRecord
    /// </summary>
    public static FileRecord Create(string path, string timestamp)
    {
        if (File.Exists(path))
        {
            var fileInfo = new FileInfo(path);
            return new FileRecord(
                path,
                timestamp,
                true,
                fileInfo.Length,
                fileInfo.CreationTime,
                fileInfo.LastWriteTime);
        }

        return new FileRecord(path, timestamp, false, 0, null, null);
    }
}

/// <summary>
/// 檔案群組資料模型
/// </summary>
/// <param name="Hash">檔案雜湊值</param>
/// <param name="Files">檔案清單</param>
record FileGroup(string Hash, List<FileRecord> Files)
{
    /// <summary>
    /// 取得群組的時間戳記（使用第一個檔案的時間戳記）
    /// </summary>
    public string Timestamp => Files.FirstOrDefault()?.Timestamp ?? string.Empty;

    /// <summary>
    /// 取得存在的檔案數量
    /// </summary>
    public int ExistingFileCount => Files.Count(f => f.Exists);

    /// <summary>
    /// 取得遺失的檔案數量
    /// </summary>
    public int MissingFileCount => Files.Count(f => !f.Exists);
}

/// <summary>
/// 資料庫輔助類別，提供統一的資料庫連線和基本操作
/// </summary>
static class DatabaseHelper
{
    private const string DatabaseFileName = "duplicates.db";

    /// <summary>
    /// 建立資料庫連線
    /// </summary>
    public static SqliteConnection CreateConnection()
    {
        return new SqliteConnection($"Data Source={DatabaseFileName}");
    }

    /// <summary>
    /// 執行非查詢命令（INSERT、UPDATE、DELETE）
    /// </summary>
    public static int ExecuteNonQuery(string commandText, Action<SqliteCommand>? configureParameters = null)
    {
        using var connection = CreateConnection();
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = commandText;
        configureParameters?.Invoke(command);

        return command.ExecuteNonQuery();
    }

    /// <summary>
    /// 執行查詢並處理結果
    /// </summary>
    public static T ExecuteQuery<T>(string commandText, Func<SqliteDataReader, T> processReader, Action<SqliteCommand>? configureParameters = null)
    {
        using var connection = CreateConnection();
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = commandText;
        configureParameters?.Invoke(command);

        using var reader = command.ExecuteReader();
        return processReader(reader);
    }

    /// <summary>
    /// 執行帶交易的批次操作
    /// </summary>
    public static void ExecuteTransaction(Action<SqliteConnection, SqliteTransaction> action)
    {
        using var connection = CreateConnection();
        connection.Open();

        using var transaction = connection.BeginTransaction();
        try
        {
            action(connection, transaction);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}

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

        // 初始化資料庫
        InitializeDatabase();

        // 建立報表資料夾
        if (!Directory.Exists("Reports"))
        {
            Directory.CreateDirectory("Reports");
        }
        
        while (true)
        {
            // 主選單
            Console.WriteLine("請選擇功能：");
            Console.WriteLine("1. 掃描重複檔案");
            Console.WriteLine("2. 查看並標記重複檔案");
            Console.WriteLine("3. 查看已標記刪除檔案（可取消標記）");
            Console.WriteLine("4. 查看已標記略過檔案（可取消略過）");
            Console.WriteLine("5. 執行刪除（刪除已標記的檔案）");
            Console.WriteLine("6. 查看已標記略過檔案報表");
            Console.WriteLine("7. 查看已標記刪除檔案報表");
            Console.WriteLine("8. 重複檔案分析報表");
            Console.WriteLine("9. 離開");
            Console.Write("請輸入選項 (1-9): ");

            var menuChoice = Console.ReadLine()?.Trim();
            Console.WriteLine();

            if (menuChoice == "9")
            {
                Console.WriteLine("感謝使用，再見！");
                return;
            }

            if (menuChoice == "1")
            {
                RunScanMode();
                Console.WriteLine();
                continue;
            }

            if (menuChoice == "2")
            {
                // 直接進入標記模式
                InitializeDatabase();
                var duplicateFileGroups = LoadDuplicateGroupsWithDetails();

                if (duplicateFileGroups.Count == 0)
                {
                    Console.WriteLine("資料庫中沒有重複檔案記錄，請先執行掃描！");
                    Console.WriteLine();
                    continue;
                }

                InteractiveDeleteDuplicates(duplicateFileGroups);
                Console.WriteLine();
                Console.WriteLine("提示：標記為待刪除的檔案儲存在資料庫的 FilesToDelete 資料表中");
                Console.WriteLine("您可以查看該資料表後再決定是否實際刪除檔案");
                Console.WriteLine();
                continue;
            }

            if (menuChoice == "3")
            {
                // 查看已標記刪除檔案
                ViewMarkedForDeletionFiles();
                Console.WriteLine();
                continue;
            }

            if (menuChoice == "4")
            {
                // 查看已標記略過檔案
                ViewSkippedFiles();
                Console.WriteLine();
                continue;
            }

            if (menuChoice == "5")
            {
                // 執行實際刪除
                ExecuteMarkedDeletions();
                Console.WriteLine();
                continue;
            }

            if (menuChoice == "6")
            {
                // 查看已標記略過檔案報表
                GenerateSkippedFilesReport();
                Console.WriteLine();
                continue;
            }

            if (menuChoice == "7")
            {
                // 查看已標記刪除檔案報表
                GenerateMarkedForDeletionReport();
                Console.WriteLine();
                continue;
            }

            if (menuChoice == "8")
            {
                // 重複檔案分析報表
                GenerateDuplicateAnalysisReport();
                Console.WriteLine();
                continue;
            }
        }
    }

    record FileDetails(string Path, long Size, string CreatedTime);

    static Dictionary<string, List<FileDetails>> LoadDuplicateGroupsWithDetails()
    {
        if (!File.Exists(DatabaseFileName))
        {
            return new Dictionary<string, List<FileDetails>>();
        }

        return DatabaseHelper.ExecuteQuery(
            "SELECT Hash, FilePath, FileSize, FileCreatedTime FROM DuplicateFiles WHERE IsDeleted = 0 OR IsDeleted IS NULL",
            reader =>
            {
                var hashGroups = new Dictionary<string, List<FileDetails>>();
                while (reader.Read())
                {
                    var hash = reader.GetString(0);
                    if (!hashGroups.ContainsKey(hash))
                    {
                        hashGroups[hash] = new List<FileDetails>();
                    }

                    hashGroups[hash].Add(new FileDetails(
                        reader.GetString(1),
                        reader.GetInt64(2),
                        reader.GetString(3)
                    ));
                }
                return hashGroups;
            });
    }

    static void RunScanMode()
    {
        // 接收使用者輸入多個資料夾路徑（支援逗點分隔）
        var folderPaths = new List<string>();

        while (folderPaths.Count == 0)
        {
            Console.Write("請輸入要掃描的資料夾路徑（可用逗點分隔多個路徑，或輸入 'q' 離開）: ");
            var input = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(input))
            {
                Console.WriteLine("錯誤：至少需要輸入一個路徑！");
                continue;
            }

            if (input.Equals("q", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("已取消掃描");
                Console.WriteLine();
                return;
            }

            // 用逗點分隔路徑
            var paths = input.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .ToList();

            var invalidPaths = new List<string>();
            var validPaths = new List<string>();

            foreach (var path in paths)
            {
                if (Directory.Exists(path))
                {
                    validPaths.Add(path);
                }
                else
                {
                    invalidPaths.Add(path);
                }
            }

            if (invalidPaths.Count > 0)
            {
                Console.WriteLine("以下路徑不存在或無效：");
                foreach (var path in invalidPaths)
                {
                    Console.WriteLine($"  - {path}");
                }
                Console.WriteLine();
            }

            if (validPaths.Count > 0)
            {
                folderPaths.AddRange(validPaths);
                Console.WriteLine($"已加入 {validPaths.Count} 個有效路徑");
            }

            if (folderPaths.Count == 0)
            {
                Console.WriteLine("沒有有效的路徑，請重新輸入！");
                Console.WriteLine();
            }
        }

        Console.WriteLine();
        Console.WriteLine($"開始掃描 {folderPaths.Count} 個資料夾:");
        foreach (var path in folderPaths)
        {
            Console.WriteLine($"  - {path}");
        }

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

            // 掃描所有資料夾
            foreach (var folderPath in folderPaths)
            {
                Console.WriteLine($"正在掃描資料夾: {folderPath}");
                ScanAndWriteDuplicates(folderPath, existingHashes, processedFiles);
                Console.WriteLine();
            }

            Console.WriteLine("所有資料夾掃描完成！");
            Console.WriteLine($"結果已儲存至: {DatabaseFileName}");
            Console.WriteLine();
            Console.WriteLine("提示：使用選項 2 可以查看並標記重複檔案");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"發生錯誤: {ex.Message}");
        }
    }

    static void InteractiveDeleteDuplicates(Dictionary<string, List<FileDetails>> hashGroups)
    {
        var duplicateGroups = hashGroups.Where(g => g.Value.Count > 1).ToList();

        if (duplicateGroups.Count == 0)
        {
            Console.WriteLine("沒有找到重複檔案！");
            return;
        }

        // 載入已標記的檔案及其 Hash
        var markedFiles = LoadMarkedFiles();
        var markedFilesByHash = LoadMarkedFilesByHash();
        var skippedHashes = LoadSkippedHashes();

        // 過濾出尚未完全標記的群組（至少保留一個檔案未標記）
        var groupsWithUnmarkedFiles = duplicateGroups
            .Where(g =>
            {
                var hash = g.Key;

                // 檢查是否已經被跳過
                if (skippedHashes.Contains(hash))
                {
                    return false;
                }

                var allFilePaths = g.Value.Select(f => f.Path).ToList();
                var unmarkedFiles = allFilePaths.Where(f => !markedFiles.Contains(f)).ToList();

                // 檢查此 hash 是否已經完全處理過
                if (markedFilesByHash.ContainsKey(hash))
                {
                    var markedCount = markedFilesByHash[hash].Count;
                    var totalCount = allFilePaths.Count;

                    if (markedCount >= totalCount - 1)
                    {
                        return false;
                    }
                }

                return unmarkedFiles.Count > 0;
            })
            .Select(g => new
            {
                Group = g,
                FileSize = g.Value.Select(f => f.Size).FirstOrDefault()
            })
            .OrderByDescending(x => x.FileSize) // 依檔案大小降序排列（大的優先）
            .Select(x => x.Group)
            .ToList();

        if (groupsWithUnmarkedFiles.Count == 0)
        {
            Console.WriteLine("所有重複檔案都已標記完成！");
            Console.WriteLine($"已標記 {markedFiles.Count} 個檔案為待刪除");
            Console.WriteLine("請使用選項 3 查看或管理已標記的檔案");
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"找到 {groupsWithUnmarkedFiles.Count} 組尚有未標記檔案的重複檔案");
        if (markedFiles.Count > 0)
        {
            Console.WriteLine($"已標記 {markedFiles.Count} 個檔案為待刪除");
        }

        Console.WriteLine();

        int groupIndex = 1;
        foreach (var group in groupsWithUnmarkedFiles)
        {
            Console.WriteLine($"=== 重複組 {groupIndex}/{groupsWithUnmarkedFiles.Count} ===");
            Console.WriteLine($"Hash: {group.Key}");
            Console.WriteLine($"共 {group.Value.Count} 個檔案:");
            Console.WriteLine();

            var filesInGroup = group.Value;
            
            // 顯示檔案清單
            for (int i = 0; i < filesInGroup.Count; i++)
            {
                var fileDetails = filesInGroup[i];
                var isMarked = markedFiles.Contains(fileDetails.Path);
                var markIndicator = isMarked ? " [已標記]" : "";

                Console.WriteLine($"[{i + 1}] {fileDetails.Path}{markIndicator}");
                Console.WriteLine($"    大小: {FormatFileSize(fileDetails.Size)}");
                Console.WriteLine($"    建立時間: {fileDetails.CreatedTime}");
                Console.WriteLine();
            }

            // 詢問操作
            while (true)
            {
                var validFilePaths = filesInGroup.Select(f => f.Path).ToList();
                DisplayMenu(
                    "輸入編號標記該檔案為待刪除（可用逗號分隔多個，例如: 1,3,5）",
                    "輸入 'p' 或 'p 編號' 預覽檔案（例如: p 1,2 或 p 預覽所有）",
                    "輸入 'k' 保留所有檔案並跳過此組",
                    "輸入 'a' 自動保留最舊的檔案，標記其他為待刪除",
                    "輸入 'q' 結束作業"
                );

                var choice = Console.ReadLine()?.Trim().ToLower();

                if (choice == "q")
                {
                    Console.WriteLine("已結束標記作業");
                    return;
                }

                if (HandlePreviewCommand(choice, validFilePaths))
                {
                    continue;
                }

                if (choice == "k")
                {
                    MarkHashAsSkipped(group.Key, validFilePaths);
                    Console.WriteLine("已跳過此組並記錄，下次不會再顯示此組");
                    Console.WriteLine();
                    break;
                }

                if (choice == "a")
                {
                    var oldestFile = filesInGroup
                        .OrderBy(f => DateTime.Parse(f.CreatedTime))
                        .First();

                    var autoDeletePaths = filesInGroup
                        .Where(f => f.Path != oldestFile.Path)
                        .Select(f => f.Path)
                        .ToList();

                    Console.WriteLine($"保留最舊的檔案: {oldestFile.Path}");
                    Console.WriteLine($"建立時間: {oldestFile.CreatedTime}");
                    Console.WriteLine();
                    Console.WriteLine($"將標記以下 {autoDeletePaths.Count} 個檔案為待刪除：");
                    foreach (var filePath in autoDeletePaths)
                    {
                        var fileToMark = filesInGroup.First(f => f.Path == filePath);
                        Console.WriteLine($"  - {filePath}");
                        Console.WriteLine($"    建立時間: {fileToMark.CreatedTime}");
                    }

                    Console.WriteLine();
                    if (ConfirmAction("確認標記？"))
                    {
                        if (MarkFilesForDeletion(group.Key, autoDeletePaths))
                        {
                            Console.WriteLine("標記完成！");
                            markedFiles.UnionWith(autoDeletePaths);
                        }
                    }
                    else
                    {
                        Console.WriteLine("已取消標記");
                    }

                    Console.WriteLine();
                    break;
                }

                var indices = ParseIndices(choice, filesInGroup.Count);

                if (indices.Count == 0)
                {
                    Console.WriteLine("無效的選擇，請重新輸入！");
                    Console.WriteLine();
                    continue;
                }

                if (indices.Count == filesInGroup.Count)
                {
                    Console.WriteLine("錯誤：不能刪除所有檔案，至少要保留一個！");
                    Console.WriteLine();
                    continue;
                }

                var filesToDelete = indices.Select(i => filesInGroup[i - 1].Path).ToList();

                var alreadyMarked = filesToDelete.Where(f => markedFiles.Contains(f)).ToList();
                var newMarks = filesToDelete.Where(f => !markedFiles.Contains(f)).ToList();

                if (alreadyMarked.Count > 0)
                {
                    Console.WriteLine($"以下 {alreadyMarked.Count} 個檔案已經被標記：");
                    foreach (var file in alreadyMarked)
                    {
                        Console.WriteLine($"  - {file}");
                    }

                    Console.WriteLine("這些檔案會先取消標記，然後重新標記");
                    Console.WriteLine();
                }

                if (newMarks.Count > 0)
                {
                    Console.WriteLine($"將標記以下 {newMarks.Count} 個檔案為待刪除：");
                    foreach (var file in newMarks)
                    {
                        Console.WriteLine($"  - {file}");
                    }
                }

                if (ConfirmAction("確認標記？"))
                {
                    if (alreadyMarked.Count > 0)
                    {
                        UnmarkFiles(alreadyMarked);
                        markedFiles.ExceptWith(alreadyMarked);
                    }

                    if (MarkFilesForDeletion(group.Key, filesToDelete))
                    {
                        Console.WriteLine("標記完成！");
                        markedFiles.UnionWith(filesToDelete);
                    }

                    Console.WriteLine();
                    break;
                }
                else
                {
                    Console.WriteLine("已取消標記");
                    Console.WriteLine();
                }
            }

            groupIndex++;
        }

        Console.WriteLine("所有重複檔案處理完成！");
    }

    static bool MarkFilesForDeletion(string hash, List<string> files)
    {
        var success = true;
        foreach (var file in files)
        {
            try
            {
                MarkFileForDeletion(hash, file);
                Console.WriteLine($"已標記為待刪除: {file}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"標記失敗 [{file}]: {ex.Message}");
                success = false;
            }
        }

        return success;
    }

    static void MarkFileForDeletion(string hash, string filePath)
    {
        DatabaseHelper.ExecuteNonQuery(
            "INSERT OR IGNORE INTO FilesToDelete (Hash, FilePath, MarkedAt) VALUES ($hash, $filePath, $markedAt)",
            cmd =>
            {
                cmd.Parameters.Add(new SqliteParameter("$hash", (object)hash ?? DBNull.Value));
                cmd.Parameters.Add(new SqliteParameter("$filePath", filePath));
                cmd.Parameters.Add(new SqliteParameter("$markedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
            });
    }

    static void ExecuteMarkedDeletions()
    {
        InitializeDatabase();

        using var connection = new SqliteConnection($"Data Source={DatabaseFileName}");
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT FilePath, MarkedAt FROM FilesToDelete ORDER BY MarkedAt";

        var markedFiles = new List<(string path, string markedAt)>();

        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                markedFiles.Add((reader.GetString(0), reader.GetString(1)));
            }
        }

        if (markedFiles.Count == 0)
        {
            Console.WriteLine("沒有標記為待刪除的檔案！");
            return;
        }

        Console.WriteLine($"找到 {markedFiles.Count} 個標記為待刪除的檔案：");
        Console.WriteLine();

        var existingFiles = markedFiles.Where(f => File.Exists(f.path)).ToList();
        var missingFiles = markedFiles.Where(f => !File.Exists(f.path)).ToList();

        if (existingFiles.Count > 0)
        {
            Console.WriteLine("存在的檔案：");
            foreach (var (path, markedAt) in existingFiles)
            {
                var fileInfo = new FileInfo(path);
                Console.WriteLine($"  - {path}");
                Console.WriteLine($"    大小: {FormatFileSize(fileInfo.Length)}，標記時間: {markedAt}");
            }

            Console.WriteLine();
        }

        if (missingFiles.Count > 0)
        {
            Console.WriteLine($"已不存在的檔案 ({missingFiles.Count} 個)：");
            foreach (var (path, _) in missingFiles)
            {
                Console.WriteLine($"  - {path}");
            }

            Console.WriteLine();
        }

        if (existingFiles.Count == 0)
        {
            Console.WriteLine("所有標記的檔案都已不存在！");
            if (ConfirmAction("是否要清除這些記錄？"))
            {
                ClearDeletedMarks();
            }

            return;
        }

        if (!ConfirmAction($"確認要刪除這 {existingFiles.Count} 個檔案嗎？"))
        {
            Console.WriteLine("已取消刪除操作");
            return;
        }

        Console.WriteLine();
        Console.WriteLine("開始刪除檔案...");

        int successCount = 0;
        int failCount = 0;

        var deletedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        foreach (var (path, _) in existingFiles)
        {
            try
            {
                File.Delete(path);
                Console.WriteLine($"✓ 已刪除: {path}");
                successCount++;

                // 從標記表中移除
                var deleteMarkCommand = connection.CreateCommand();
                deleteMarkCommand.CommandText = "DELETE FROM FilesToDelete WHERE FilePath = $path";
                var markPathParam = deleteMarkCommand.CreateParameter();
                markPathParam.ParameterName = "$path";
                markPathParam.Value = path;
                deleteMarkCommand.Parameters.Add(markPathParam);
                deleteMarkCommand.ExecuteNonQuery();

                // 標記為已刪除（保留歷史記錄）
                var updateCommand = connection.CreateCommand();
                updateCommand.CommandText = @"
                    UPDATE DuplicateFiles
                    SET IsDeleted = 1, DeletedAt = $deletedAt
                    WHERE FilePath = $path";
                var updatePathParam = updateCommand.CreateParameter();
                updatePathParam.ParameterName = "$path";
                updatePathParam.Value = path;
                updateCommand.Parameters.Add(updatePathParam);
                var deletedAtParam = updateCommand.CreateParameter();
                deletedAtParam.ParameterName = "$deletedAt";
                deletedAtParam.Value = deletedAt;
                updateCommand.Parameters.Add(deletedAtParam);
                updateCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ 刪除失敗 [{path}]: {ex.Message}");
                failCount++;
            }
        }

        Console.WriteLine();
        Console.WriteLine($"刪除完成！成功: {successCount}，失敗: {failCount}");

        if (missingFiles.Count > 0)
        {
            if (ConfirmAction($"是否要清除 {missingFiles.Count} 個已不存在的檔案記錄？"))
            {
                foreach (var (path, _) in missingFiles)
                {
                    var deleteCommand = connection.CreateCommand();
                    deleteCommand.CommandText = "DELETE FROM FilesToDelete WHERE FilePath = $path";
                    var pathParam = deleteCommand.CreateParameter();
                    pathParam.ParameterName = "$path";
                    pathParam.Value = path;
                    deleteCommand.Parameters.Add(pathParam);
                    deleteCommand.ExecuteNonQuery();
                }

                Console.WriteLine("已清除記錄！");
            }
        }
    }

    static void ClearDeletedMarks()
    {
        using var connection = new SqliteConnection($"Data Source={DatabaseFileName}");
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM FilesToDelete";
        var count = command.ExecuteNonQuery();

        Console.WriteLine($"已清除 {count} 筆記錄");
    }

    static void ViewSkippedFiles()
    {
        InitializeDatabase();

        var skippedGroups = LoadSkippedFilesGroupedByHash();

        if (skippedGroups.Count == 0)
        {
            Console.WriteLine("目前沒有已標記略過的檔案群組！");
            return;
        }

        Console.WriteLine($"=== 已標記略過檔案群組清單 (共 {skippedGroups.Count} 組) ===");
        Console.WriteLine();

        int groupIndex = 1;
        var groupList = new List<(string hash, List<(string path, string skippedAt)> files)>();

        foreach (var group in skippedGroups)
        {
            var hash = group.Key;
            var files = group.Value;
            groupList.Add((hash, files));

            Console.WriteLine($"[{groupIndex}] Hash: {hash}");
            Console.WriteLine($"    檔案數量: {files.Count}");
            Console.WriteLine($"    略過時間: {files.First().skippedAt}");

            DisplayFileGroup(files, "略過時間");

            Console.WriteLine();
            groupIndex++;
        }

        // 操作選單
        while (true)
        {
            DisplayMenu(
                "輸入編號取消略過該群組（可用逗號分隔多個，例如: 1,3,5）",
                "輸入 'p' 或 'p 編號' 預覽群組檔案（例如: p 1,2 或 p 預覽所有）",
                "輸入 'a' 清除所有略過標記",
                "輸入 'q' 返回主選單"
            );

            var choice = Console.ReadLine()?.Trim().ToLower();
            Console.WriteLine();

            if (choice == "q")
            {
                return;
            }

            // 處理預覽指令（群組模式）
            if (choice?.StartsWith("p") == true)
            {
                var previewPart = choice.Substring(1).Trim();
                var previewIndices = ParsePreviewIndices(previewPart, groupList.Count);

                List<string> filesToPreview;
                if (previewIndices.Count == 0)
                {
                    // 預覽所有群組的檔案
                    filesToPreview = groupList
                        .SelectMany(g => g.files)
                        .Where(f => File.Exists(f.path))
                        .Select(f => f.path)
                        .ToList();
                }
                else
                {
                    // 預覽指定群組的檔案
                    filesToPreview = previewIndices
                        .SelectMany(i => groupList[i - 1].files)
                        .Where(f => File.Exists(f.path))
                        .Select(f => f.path)
                        .ToList();
                }

                if (filesToPreview.Count == 0)
                {
                    Console.WriteLine("沒有存在的檔案可以預覽！");
                    Console.WriteLine();
                    continue;
                }

                Console.WriteLine($"正在開啟 {filesToPreview.Count} 個檔案進行預覽...");
                PreviewFiles(filesToPreview);
                Console.WriteLine();
                continue;
            }

            if (choice == "a")
            {
                if (ConfirmAction($"確認要清除所有 {skippedGroups.Count} 組略過標記嗎？"))
                {
                    ClearAllSkippedMarks();
                    Console.WriteLine("已清除所有略過標記！");
                    return;
                }
                else
                {
                    Console.WriteLine("已取消操作");
                    Console.WriteLine();
                    continue;
                }
            }

            var indices = ParseIndices(choice, groupList.Count);

            if (indices.Count == 0)
            {
                Console.WriteLine("無效的選擇，請重新輸入！");
                Console.WriteLine();
                continue;
            }

            var hashesToUnskip = indices.Select(i => groupList[i - 1].hash).ToList();

            Console.WriteLine($"確認要取消以下 {hashesToUnskip.Count} 組的略過標記：");
            foreach (var index in indices)
            {
                var (hash, files) = groupList[index - 1];
                Console.WriteLine($"  [{index}] Hash: {hash} ({files.Count} 個檔案)");
            }

            if (ConfirmAction("確認取消略過標記？"))
            {
                UnskipHashes(hashesToUnskip);
                Console.WriteLine("已取消略過標記！");
                return;
            }
            else
            {
                Console.WriteLine("已取消操作");
                Console.WriteLine();
            }
        }
    }

    static void ViewMarkedForDeletionFiles()
    {
        InitializeDatabase();

        var markedFiles = LoadMarkedForDeletionFilesWithDetails();

        if (markedFiles.Count == 0)
        {
            Console.WriteLine("目前沒有已標記刪除的檔案！");
            return;
        }

        Console.WriteLine($"=== 已標記刪除檔案清單 (共 {markedFiles.Count} 個) ===");
        Console.WriteLine();

        var existingFiles = markedFiles.Where(f => File.Exists(f.path)).ToList();
        var missingFiles = markedFiles.Where(f => !File.Exists(f.path)).ToList();

        if (existingFiles.Count > 0)
        {
            Console.WriteLine($"存在的檔案 ({existingFiles.Count} 個)：");
            for (int i = 0; i < existingFiles.Count; i++)
            {
                var (path, markedAt) = existingFiles[i];
                var fileInfo = new FileInfo(path);
                Console.WriteLine($"[{i + 1}] {path}");
                Console.WriteLine($"    大小: {FormatFileSize(fileInfo.Length)}，標記時間: {markedAt}");
                Console.WriteLine();
            }
        }

        if (missingFiles.Count > 0)
        {
            Console.WriteLine($"已標記刪除的檔案 ({missingFiles.Count} 個)：");
            foreach (var (path, markedAt) in missingFiles)
            {
                Console.WriteLine($"  - {path} (標記時間: {markedAt})");
            }

            Console.WriteLine();
        }

        // 操作選單
        while (true)
        {
            DisplayMenu(
                "輸入編號取消標記（可用逗號分隔多個，例如: 1,3,5）",
                "輸入 'p' 或 'p 編號' 預覽檔案（例如: p 1,2 或 p 預覽所有）",
                "輸入 'a' 清除所有標記",
                "輸入 'q' 返回主選單"
            );

            var choice = Console.ReadLine()?.Trim().ToLower();
            Console.WriteLine();

            if (choice == "q")
            {
                return;
            }

            // 處理預覽指令
            if (choice?.StartsWith("p") == true)
            {
                if (existingFiles.Count == 0)
                {
                    Console.WriteLine("沒有存在的檔案可以預覽！");
                    Console.WriteLine();
                    continue;
                }

                var existingPaths = existingFiles.Select(f => f.path).ToList();
                if (HandlePreviewCommand(choice, existingPaths))
                {
                    continue;
                }
            }

            if (choice == "a")
            {
                if (ConfirmAction($"確認要清除所有 {markedFiles.Count} 個標記嗎？"))
                {
                    ClearAllMarks();
                    Console.WriteLine("已清除所有標記！");
                    return;
                }
                else
                {
                    Console.WriteLine("已取消操作");
                    Console.WriteLine();
                    continue;
                }
            }

            if (existingFiles.Count == 0)
            {
                Console.WriteLine("沒有存在的檔案可以取消標記！");
                Console.WriteLine();
                continue;
            }

            var indices = ParseIndices(choice, existingFiles.Count);

            if (indices.Count == 0)
            {
                Console.WriteLine("無效的選擇，請重新輸入！");
                Console.WriteLine();
                continue;
            }

            var filesToUnmark = indices.Select(i => existingFiles[i - 1].path).ToList();

            Console.WriteLine($"確認要取消以下 {filesToUnmark.Count} 個檔案的標記：");
            foreach (var file in filesToUnmark)
            {
                Console.WriteLine($"  - {file}");
            }

            if (ConfirmAction("確認取消標記？"))
            {
                UnmarkFiles(filesToUnmark);
                Console.WriteLine("已取消標記！");
                return;
            }
            else
            {
                Console.WriteLine("已取消操作");
                Console.WriteLine();
            }
        }
    }

    static List<(string path, string markedAt)> LoadMarkedForDeletionFilesWithDetails()
    {
        try
        {
            return DatabaseHelper.ExecuteQuery(
                "SELECT FilePath, MarkedAt FROM FilesToDelete ORDER BY MarkedAt",
                reader =>
                {
                    var files = new List<(string, string)>();
                    while (reader.Read())
                    {
                        files.Add((reader.GetString(0), reader.GetString(1)));
                    }
                    return files;
                });
        }
        catch
        {
            // 資料表可能不存在，忽略錯誤
            return new List<(string, string)>();
        }
    }

    static Dictionary<string, List<(string path, string markedAt)>> LoadMarkedForDeletionFilesGroupedByHash()
    {
        return LoadFilesGroupedByHash("FilesToDelete", "MarkedAt");
    }

    static void UnmarkFiles(List<string> filePaths)
    {
        DatabaseHelper.ExecuteTransaction((connection, transaction) =>
        {
            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "DELETE FROM FilesToDelete WHERE FilePath = $path";

            var pathParam = new SqliteParameter("$path", "");
            command.Parameters.Add(pathParam);

            foreach (var path in filePaths)
            {
                pathParam.Value = path;
                command.ExecuteNonQuery();
            }
        });
    }

    static void ClearAllMarks()
    {
        DatabaseHelper.ExecuteNonQuery("DELETE FROM FilesToDelete");
    }

    static HashSet<string> LoadMarkedFiles()
    {
        try
        {
            return DatabaseHelper.ExecuteQuery(
                "SELECT FilePath FROM FilesToDelete",
                reader =>
                {
                    var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    while (reader.Read())
                    {
                        files.Add(reader.GetString(0));
                    }
                    return files;
                });
        }
        catch
        {
            // 資料表可能不存在，忽略錯誤
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    static Dictionary<string, HashSet<string>> LoadMarkedFilesByHash()
    {
        try
        {
            return DatabaseHelper.ExecuteQuery(
                "SELECT Hash, FilePath FROM FilesToDelete WHERE Hash IS NOT NULL",
                reader =>
                {
                    var filesByHash = new Dictionary<string, HashSet<string>>();
                    while (reader.Read())
                    {
                        var hash = reader.GetString(0);
                        var filePath = reader.GetString(1);

                        if (!filesByHash.ContainsKey(hash))
                        {
                            filesByHash[hash] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        }

                        filesByHash[hash].Add(filePath);
                    }
                    return filesByHash;
                });
        }
        catch
        {
            // 資料表可能不存在或沒有 Hash 欄位，忽略錯誤
            return new Dictionary<string, HashSet<string>>();
        }
    }

    static HashSet<string> LoadSkippedHashes()
    {
        try
        {
            return DatabaseHelper.ExecuteQuery(
                "SELECT Hash FROM SkippedHashes",
                reader =>
                {
                    var hashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    while (reader.Read())
                    {
                        hashes.Add(reader.GetString(0));
                    }
                    return hashes;
                });
        }
        catch
        {
            // 資料表可能不存在，忽略錯誤
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    static void MarkHashAsSkipped(string hash, List<string> filePaths)
    {
        var skippedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        DatabaseHelper.ExecuteTransaction((connection, transaction) =>
        {
            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "INSERT OR IGNORE INTO SkippedHashes (Hash, FilePath, SkippedAt) VALUES ($hash, $filePath, $skippedAt)";

            var hashParam = new SqliteParameter("$hash", hash);
            var filePathParam = new SqliteParameter("$filePath", "");
            var skippedAtParam = new SqliteParameter("$skippedAt", skippedAt);

            command.Parameters.Add(hashParam);
            command.Parameters.Add(filePathParam);
            command.Parameters.Add(skippedAtParam);

            foreach (var filePath in filePaths)
            {
                filePathParam.Value = filePath;
                command.ExecuteNonQuery();
            }
        });
    }

    static Dictionary<string, List<(string path, string skippedAt)>> LoadSkippedFilesGroupedByHash()
    {
        return LoadFilesGroupedByHash("SkippedHashes", "SkippedAt");
    }

    static void UnskipHashes(List<string> hashes)
    {
        DatabaseHelper.ExecuteTransaction((connection, transaction) =>
        {
            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "DELETE FROM SkippedHashes WHERE Hash = $hash";

            var hashParam = new SqliteParameter("$hash", "");
            command.Parameters.Add(hashParam);

            foreach (var hash in hashes)
            {
                hashParam.Value = hash;
                command.ExecuteNonQuery();
            }
        });
    }

    static void ClearAllSkippedMarks()
    {
        DatabaseHelper.ExecuteNonQuery("DELETE FROM SkippedHashes");
    }

    static void GenerateSkippedFilesReport()
    {
        InitializeDatabase();

        var skippedGroups = LoadSkippedFilesGroupedByHash();

        if (skippedGroups.Count == 0)
        {
            Console.WriteLine("目前沒有已標記略過的檔案群組！");
            return;
        }

        GenerateReport(skippedGroups, "SkippedFilesReport", "SkippedFilesReport.html", "SkippedAt");
    }

    static void GenerateMarkedForDeletionReport()
    {
        InitializeDatabase();

        var markedFilesGrouped = LoadMarkedForDeletionFilesGroupedByHash();

        if (markedFilesGrouped.Count == 0)
        {
            Console.WriteLine("目前沒有已標記刪除的檔案！");
            return;
        }

        GenerateReport(markedFilesGrouped, "MarkedForDeletionReport", "MarkedForDeletionReport.html", "MarkedAt");
    }


    static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }

    /// <summary>
    /// 解析預覽指令，返回要預覽的項目索引
    /// </summary>
    /// <param name="previewPart">預覽指令部分（例如 "1,2,3" 或空字串表示全部）</param>
    /// <param name="maxCount">最大項目數量</param>
    /// <returns>要預覽的索引列表，空列表表示預覽全部</returns>
    static List<int> ParsePreviewIndices(string previewPart, int maxCount)
    {
        if (string.IsNullOrEmpty(previewPart))
        {
            // 空字串表示預覽全部
            return new List<int>();
        }

        return previewPart.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => int.TryParse(s, out _))
            .Select(int.Parse)
            .Where(i => i >= 1 && i <= maxCount)
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// 處理預覽指令，預覽指定的檔案
    /// </summary>
    /// <param name="choice">使用者輸入的完整指令（例如 "p" 或 "p 1,2"）</param>
    /// <param name="files">所有可預覽的檔案列表</param>
    /// <returns>true 表示已處理預覽指令，false 表示不是預覽指令</returns>
    static bool HandlePreviewCommand(string? choice, List<string> files)
    {
        if (choice?.StartsWith("p") != true)
        {
            return false;
        }

        var previewPart = choice!.Substring(1).Trim();
        var previewIndices = ParsePreviewIndices(previewPart, files.Count);

        if (previewIndices.Count == 0)
        {
            // 預覽所有檔案
            var existingFiles = files.Where(File.Exists).ToList();
            if (existingFiles.Count == 0)
            {
                Console.WriteLine("沒有存在的檔案可以預覽！");
                Console.WriteLine();
                return true;
            }

            Console.WriteLine($"正在開啟所有 {existingFiles.Count} 個檔案進行預覽...");
            PreviewFiles(existingFiles);
            Console.WriteLine();
            return true;
        }

        // 預覽指定的檔案
        var filesToPreview = previewIndices
            .Select(i => files[i - 1])
            .Where(File.Exists)
            .ToList();

        if (filesToPreview.Count == 0)
        {
            Console.WriteLine("選擇的檔案都不存在，無法預覽！");
            Console.WriteLine();
            return true;
        }

        Console.WriteLine($"正在開啟 {filesToPreview.Count} 個檔案進行預覽...");
        PreviewFiles(filesToPreview);
        Console.WriteLine();
        return true;
    }

    /// <summary>
    /// 顯示確認對話框
    /// </summary>
    /// <param name="message">確認訊息</param>
    /// <returns>true 表示使用者確認，false 表示取消</returns>
    static bool ConfirmAction(string message)
    {
        Console.Write($"{message} (Y/n): ");
        var confirm = Console.ReadLine()?.Trim().ToUpper();
        return string.IsNullOrEmpty(confirm) || confirm == "Y" || confirm == "YES";
    }

    /// <summary>
    /// 解析使用者輸入的編號（支援逗號分隔）
    /// </summary>
    static List<int> ParseIndices(string? input, int maxCount)
    {
        return input?.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => int.TryParse(s, out _))
            .Select(int.Parse)
            .Where(i => i >= 1 && i <= maxCount)
            .Distinct()
            .ToList() ?? new List<int>();
    }

    /// <summary>
    /// 顯示檔案資訊（包含路徑、大小、時間等）
    /// </summary>
    static void DisplayFileInfo(string path, bool showDetails = true)
    {
        Console.WriteLine($"  - {path}");

        if (showDetails && File.Exists(path))
        {
            var fileInfo = new FileInfo(path);
            Console.WriteLine($"    大小: {FormatFileSize(fileInfo.Length)}");
        }
    }

    /// <summary>
    /// 顯示檔案群組資訊（分為存在和不存在的檔案）
    /// </summary>
    static void DisplayFileGroup(List<(string path, string timestamp)> files, string timestampLabel)
    {
        var existingFiles = files.Where(f => File.Exists(f.path)).ToList();
        var missingFiles = files.Where(f => !File.Exists(f.path)).ToList();

        if (existingFiles.Count > 0)
        {
            Console.WriteLine($"    存在的檔案 ({existingFiles.Count} 個):");
            foreach (var (path, _) in existingFiles)
            {
                DisplayFileInfo(path, showDetails: true);
            }
        }

        if (missingFiles.Count > 0)
        {
            Console.WriteLine($"    已不存在的檔案 ({missingFiles.Count} 個):");
            foreach (var (path, _) in missingFiles)
            {
                Console.WriteLine($"      - {path}");
            }
        }
    }

    /// <summary>
    /// 顯示操作選單
    /// </summary>
    static void DisplayMenu(params string[] options)
    {
        Console.WriteLine("操作選項：");
        foreach (var option in options)
        {
            Console.WriteLine($"  {option}");
        }
        Console.Write("請選擇: ");
    }

    /// <summary>
    /// 建立報表資料物件
    /// </summary>
    static object CreateReportData(Dictionary<string, List<(string path, string timestamp)>> groups, string timestampLabel)
    {
        return new
        {
            GeneratedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            TotalGroups = groups.Count,
            Groups = groups.Select(g => new
            {
                Hash = g.Key,
                Timestamp = g.Value.First().timestamp,
                FileCount = g.Value.Count,
                Files = g.Value.Select(f => new
                {
                    Path = f.path,
                    Exists = File.Exists(f.path),
                    Size = File.Exists(f.path) ? new FileInfo(f.path).Length : 0,
                    CreatedTime = File.Exists(f.path)
                        ? new FileInfo(f.path).CreationTime.ToString("yyyy-MM-dd HH:mm:ss")
                        : null,
                    ModifiedTime = File.Exists(f.path)
                        ? new FileInfo(f.path).LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")
                        : null
                }).ToList()
            }).ToList()
        };
    }

    /// <summary>
    /// 序列化報表資料為 JSON
    /// </summary>
    static string SerializeReportData(object reportData, bool indent = true)
    {
        return System.Text.Json.JsonSerializer.Serialize(reportData, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = indent,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
    }

    /// <summary>
    /// 統一的報表生成方法
    /// </summary>
    static void GenerateReport(
        Dictionary<string, List<(string path, string timestamp)>> groups,
        string reportPrefix,
        string templateFileName,
        string timestampLabel)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

        // 1. 建立報表資料
        var reportData = CreateReportData(groups, timestampLabel);

        // 2. 產生 JSON 檔案
        var jsonFileName = $"Reports/{reportPrefix}_{timestamp}.json";
        var json = SerializeReportData(reportData, indent: true);
        File.WriteAllText(jsonFileName, json, Encoding.UTF8);

        // 3. 產生 HTML 檔案
        var jsonCompact = SerializeReportData(reportData, indent: false);
        var template = File.ReadAllText($"Templates/{templateFileName}", Encoding.UTF8);
        var html = template.Replace("{{REPORT_DATA}}", jsonCompact);

        var htmlFileName = $"Reports/{reportPrefix}_{timestamp}.html";
        File.WriteAllText(htmlFileName, html, Encoding.UTF8);

        // 4. 顯示統計資訊
        var totalFiles = groups.Sum(g => g.Value.Count);
        var totalExistingFiles = groups.Sum(g => g.Value.Count(f => File.Exists(f.path)));
        var totalMissingFiles = totalFiles - totalExistingFiles;

        Console.WriteLine($"JSON 報表已產生：{Path.GetFullPath(jsonFileName)}");
        Console.WriteLine($"HTML 報表已產生：{Path.GetFullPath(htmlFileName)}");
        Console.WriteLine($"總共 {groups.Count} 組，{totalFiles} 個檔案（存在：{totalExistingFiles}，遺失：{totalMissingFiles}）");

        // 5. 自動開啟 HTML 報表
        OpenHtmlReport(htmlFileName);
    }

    /// <summary>
    /// 統一的資料載入方法（從資料庫載入檔案群組）
    /// </summary>
    /// <param name="tableName">資料表名稱</param>
    /// <param name="timestampColumn">時間戳記欄位名稱</param>
    /// <returns>按 Hash 分組的檔案清單</returns>
    static Dictionary<string, List<(string path, string timestamp)>> LoadFilesGroupedByHash(
        string tableName,
        string timestampColumn)
    {
        try
        {
            return DatabaseHelper.ExecuteQuery(
                $"SELECT Hash, FilePath, {timestampColumn} FROM {tableName} WHERE Hash IS NOT NULL ORDER BY Hash, {timestampColumn}",
                reader =>
                {
                    var groups = new Dictionary<string, List<(string, string)>>();
                    while (reader.Read())
                    {
                        var hash = reader.GetString(0);
                        var filePath = reader.GetString(1);
                        var timestamp = reader.GetString(2);

                        if (!groups.ContainsKey(hash))
                        {
                            groups[hash] = new List<(string, string)>();
                        }

                        groups[hash].Add((filePath, timestamp));
                    }
                    return groups;
                }
            );
        }
        catch
        {
            // 資料表可能不存在或沒有相應欄位，忽略錯誤
            return new Dictionary<string, List<(string, string)>>();
        }
    }

    static void ScanAndWriteDuplicates(string folderPath, Dictionary<string, List<string>> existingHashes,
        HashSet<string> processedFiles)
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
        var stats = new ProcessingStats
        {
            SkippedCount = skippedCount
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

            // 標記所有需要寫入的 hash（包含非重複檔案）
            if (!lastWrittenCount.TryGetValue(hash, out var lastCount) || lastCount != fileList.Count)
            {
                dirtyHashes.Add(hash);
                lastWrittenCount[hash] = fileList.Count;
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

    static void WriteBatch(HashSet<string> dirtyHashes, Dictionary<string, List<string>> hashGroups,
        bool isLast = false)
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
        if (!File.Exists(DatabaseFileName))
        {
            return (new Dictionary<string, List<string>>(), new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        }

        return DatabaseHelper.ExecuteQuery(
            "SELECT Hash, FilePath FROM DuplicateFiles WHERE IsDeleted = 0 OR IsDeleted IS NULL",
            reader =>
            {
                var hashGroups = new Dictionary<string, List<string>>();
                var processedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                while (reader.Read())
                {
                    var hash = reader.GetString(0);
                    var filePath = reader.GetString(1);

                    // 記錄所有已處理的檔案路徑（包含已刪除的，避免重複掃描）
                    processedFiles.Add(filePath);

                    if (!hashGroups.ContainsKey(hash))
                    {
                        hashGroups[hash] = new List<string>();
                    }

                    // 只加入未刪除的檔案
                    hashGroups[hash].Add(filePath);
                }

                return (hashGroups, processedFiles);
            });
    }

    /// <summary>
    /// 使用系統預設程式預覽檔案
    /// </summary>
    static void PreviewFiles(List<string> filePaths)
    {
        foreach (var filePath in filePaths)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"檔案不存在，無法預覽: {filePath}");
                    continue;
                }

                Console.WriteLine($"正在開啟: {Path.GetFileName(filePath)}");

                // 使用 ProcessStartInfo 來開啟檔案
                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true // 使用系統預設程式開啟
                };

                System.Diagnostics.Process.Start(processInfo);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"開啟檔案失敗 [{filePath}]: {ex.Message}");
            }
        }

        if (filePaths.Count > 1)
        {
            Console.WriteLine($"已開啟 {filePaths.Count} 個檔案，請在檢視完畢後返回繼續操作");
        }
    }

    /// <summary>
    /// 使用系統預設瀏覽器開啟 HTML 報表
    /// </summary>
    static void OpenHtmlReport(string htmlFileName)
    {
        try
        {
            var fullPath = Path.GetFullPath(htmlFileName);
            Console.WriteLine();
            Console.WriteLine("正在開啟 HTML 報表...");

            var processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = fullPath,
                UseShellExecute = true // 使用系統預設瀏覽器開啟
            };

            System.Diagnostics.Process.Start(processInfo);
            Console.WriteLine("HTML 報表已在瀏覽器中開啟");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"無法開啟 HTML 報表: {ex.Message}");
            Console.WriteLine("請手動開啟報表檔案");
        }
    }

    static void InitializeDatabase()
    {
        using var connection = new SqliteConnection($"Data Source={DatabaseFileName}");
        connection.Open();

        // 第一步：建立基本資料表（不包含 IsDeleted 和 DeletedAt）
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

            CREATE TABLE IF NOT EXISTS FilesToDelete (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Hash TEXT,
                FilePath TEXT NOT NULL UNIQUE,
                MarkedAt TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS SkippedHashes (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Hash TEXT NOT NULL,
                FilePath TEXT NOT NULL,
                SkippedAt TEXT NOT NULL,
                UNIQUE(Hash, FilePath)
            );
        ";
        command.ExecuteNonQuery();

        // 第二步：檢查並新增 IsDeleted 和 DeletedAt 欄位
        var checkCommand = connection.CreateCommand();
        checkCommand.CommandText = "PRAGMA table_info(DuplicateFiles)";
        var existingColumns = new HashSet<string>();

        using (var reader = checkCommand.ExecuteReader())
        {
            while (reader.Read())
            {
                existingColumns.Add(reader.GetString(1)); // column name is at index 1
            }
        }

        // 如果 IsDeleted 欄位不存在，則新增
        if (!existingColumns.Contains("IsDeleted"))
        {
            try
            {
                var alterCommand1 = connection.CreateCommand();
                alterCommand1.CommandText = "ALTER TABLE DuplicateFiles ADD COLUMN IsDeleted INTEGER NOT NULL DEFAULT 0";
                alterCommand1.ExecuteNonQuery();
                Console.WriteLine("已新增 IsDeleted 欄位到 DuplicateFiles 資料表");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"警告：無法新增 IsDeleted 欄位: {ex.Message}");
            }
        }

        // 如果 DeletedAt 欄位不存在，則新增
        if (!existingColumns.Contains("DeletedAt"))
        {
            try
            {
                var alterCommand2 = connection.CreateCommand();
                alterCommand2.CommandText = "ALTER TABLE DuplicateFiles ADD COLUMN DeletedAt TEXT";
                alterCommand2.ExecuteNonQuery();
                Console.WriteLine("已新增 DeletedAt 欄位到 DuplicateFiles 資料表");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"警告：無法新增 DeletedAt 欄位: {ex.Message}");
            }
        }

        // 第三步：建立索引（在欄位確定存在後）
        var indexCommand = connection.CreateCommand();
        indexCommand.CommandText = @"
            CREATE INDEX IF NOT EXISTS idx_hash ON DuplicateFiles(Hash);
            CREATE INDEX IF NOT EXISTS idx_is_deleted ON DuplicateFiles(IsDeleted);
        ";
        indexCommand.ExecuteNonQuery();
    }

    /// <summary>
    /// 將重複檔案資訊寫入 SQLite 資料庫
    /// </summary>
    static void WriteToDatabase(Dictionary<string, List<string>> duplicates)
    {
        var createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        DatabaseHelper.ExecuteTransaction((connection, transaction) =>
        {
            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                INSERT INTO DuplicateFiles (Hash, FilePath, FileName, FileSize, FileCreatedTime, FileCount, CreatedAt)
                VALUES ($hash, $filePath, $fileName, $fileSize, $fileCreatedTime, $fileCount, $createdAt)
                ON CONFLICT(Hash, FilePath) DO UPDATE SET
                    FileName = excluded.FileName,
                    FileSize = excluded.FileSize,
                    FileCreatedTime = excluded.FileCreatedTime,
                    FileCount = excluded.FileCount,
                    CreatedAt = excluded.CreatedAt";

            var hashParam = new SqliteParameter("$hash", "");
            var filePathParam = new SqliteParameter("$filePath", "");
            var fileNameParam = new SqliteParameter("$fileName", "");
            var fileSizeParam = new SqliteParameter("$fileSize", 0L);
            var fileCreatedTimeParam = new SqliteParameter("$fileCreatedTime", "");
            var fileCountParam = new SqliteParameter("$fileCount", 0);
            var createdAtParam = new SqliteParameter("$createdAt", createdAt);

            command.Parameters.Add(hashParam);
            command.Parameters.Add(filePathParam);
            command.Parameters.Add(fileNameParam);
            command.Parameters.Add(fileSizeParam);
            command.Parameters.Add(fileCreatedTimeParam);
            command.Parameters.Add(fileCountParam);
            command.Parameters.Add(createdAtParam);

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

                    command.ExecuteNonQuery();
                }
            }
        });
    }

    /// <summary>
    /// 產生重複檔案分析報表
    /// </summary>
    static void GenerateDuplicateAnalysisReport()
    {
        InitializeDatabase();

        // 載入所有未刪除的重複檔案
        var (duplicateData, totalFiles, totalDuplicates, totalDuplicateSize, fileSizeDistribution, folderStats) =
            AnalyzeDuplicateFiles();

        if (totalFiles == 0)
        {
            Console.WriteLine("資料庫中沒有重複檔案記錄！");
            return;
        }

        // 產生 JSON 和 HTML 報表
        SaveDuplicateAnalysisReport(duplicateData, totalFiles, totalDuplicates, totalDuplicateSize, fileSizeDistribution, folderStats);
    }

    /// <summary>
    /// 儲存重複檔案分析報表（JSON 和 HTML）
    /// </summary>
    static void SaveDuplicateAnalysisReport(
        List<DuplicateGroup> duplicateData,
        int totalFiles,
        int totalDuplicates,
        long totalDuplicateSize,
        FileSizeDistribution fileSizeDistribution,
        List<FolderStatistics> folderStats)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

        // 1. 建立報表資料
        var reportData = new
        {
            GeneratedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            Summary = new
            {
                TotalGroups = duplicateData.Count,
                TotalFiles = totalFiles,
                TotalDuplicates = totalDuplicates,
                TotalDuplicateSize = totalDuplicateSize,
                TotalDuplicateSizeFormatted = FormatFileSize(totalDuplicateSize),
                PotentialSavings = totalDuplicateSize,
                PotentialSavingsFormatted = FormatFileSize(totalDuplicateSize)
            },
            FileSizeDistribution = new
            {
                Small = fileSizeDistribution.Small,
                SmallPercentage = fileSizeDistribution.Small * 100.0 / duplicateData.Count,
                Medium = fileSizeDistribution.Medium,
                MediumPercentage = fileSizeDistribution.Medium * 100.0 / duplicateData.Count,
                Large = fileSizeDistribution.Large,
                LargePercentage = fileSizeDistribution.Large * 100.0 / duplicateData.Count,
                VeryLarge = fileSizeDistribution.VeryLarge,
                VeryLargePercentage = fileSizeDistribution.VeryLarge * 100.0 / duplicateData.Count
            },
            TopFoldersByDuplicateRate = folderStats
                .OrderByDescending(f => f.DuplicateRate)
                .ThenByDescending(f => f.DuplicateCount)
                .Take(50)
                .Select(f => new
                {
                    f.Path,
                    f.TotalCount,
                    f.DuplicateCount,
                    DuplicateRate = f.DuplicateRate,
                    DuplicateRateFormatted = $"{f.DuplicateRate:P2}",
                    DuplicateSize = f.DuplicateSize,
                    DuplicateSizeFormatted = FormatFileSize(f.DuplicateSize)
                }),
            LargeFileDuplicates = duplicateData
                .Where(g => g.FileSize >= 10 * 1024 * 1024)
                .OrderByDescending(g => g.FileSize * (g.FileCount - 1))
                .Take(100)
                .Select(g => new
                {
                    g.Hash,
                    FileSize = g.FileSize,
                    FileSizeFormatted = FormatFileSize(g.FileSize),
                    g.FileCount,
                    WastedSpace = g.FileSize * (g.FileCount - 1),
                    WastedSpaceFormatted = FormatFileSize(g.FileSize * (g.FileCount - 1)),
                    FilePaths = g.FilePaths
                })
        };

        // 2. 產生 JSON 檔案
        var jsonFileName = $"Reports/DuplicateAnalysisReport_{timestamp}.json";
        var json = SerializeReportData(reportData, indent: true);
        File.WriteAllText(jsonFileName, json, Encoding.UTF8);

        // 3. 產生 HTML 檔案
        var jsonCompact = SerializeReportData(reportData, indent: false);
        var template = File.ReadAllText("Templates/DuplicateAnalysisReport.html", Encoding.UTF8);
        var html = template.Replace("{{REPORT_DATA}}", jsonCompact);

        var htmlFileName = $"Reports/DuplicateAnalysisReport_{timestamp}.html";
        File.WriteAllText(htmlFileName, html, Encoding.UTF8);

        // 4. 顯示統計資訊
        Console.WriteLine($"JSON 報表已產生：{Path.GetFullPath(jsonFileName)}");
        Console.WriteLine($"HTML 報表已產生：{Path.GetFullPath(htmlFileName)}");
        Console.WriteLine($"總共 {duplicateData.Count} 組重複檔案");
        Console.WriteLine($"總檔案數：{totalFiles}，重複數：{totalDuplicates}");
        Console.WriteLine($"可節省空間：{FormatFileSize(totalDuplicateSize)}");
        Console.WriteLine();
        Console.WriteLine("重複率最高的資料夾（前 10 名）：");
        foreach (var folder in folderStats.OrderByDescending(f => f.DuplicateRate).Take(10))
        {
            Console.WriteLine($"  {folder.Path}");
            Console.WriteLine($"    重複率：{folder.DuplicateRate:P2} ({folder.DuplicateCount}/{folder.TotalCount})");
            Console.WriteLine($"    重複大小：{FormatFileSize(folder.DuplicateSize)}");
        }
        Console.WriteLine();
        Console.WriteLine("大檔案重複（前 5 名）：");
        foreach (var largeFile in duplicateData
            .Where(g => g.FileSize >= 10 * 1024 * 1024)
            .OrderByDescending(g => g.FileSize * (g.FileCount - 1))
            .Take(5))
        {
            Console.WriteLine($"  大小：{FormatFileSize(largeFile.FileSize)}，重複 {largeFile.FileCount} 次");
            Console.WriteLine($"    浪費空間：{FormatFileSize(largeFile.FileSize * (largeFile.FileCount - 1))}");
            Console.WriteLine($"    範例：{largeFile.FilePaths.FirstOrDefault()}");
        }

        // 5. 自動開啟 HTML 報表
        OpenHtmlReport(htmlFileName);
    }

    /// <summary>
    /// 分析重複檔案並計算統計資料
    /// </summary>
    static (
        List<DuplicateGroup> duplicateData,
        int totalFiles,
        int totalDuplicates,
        long totalDuplicateSize,
        FileSizeDistribution fileSizeDistribution,
        List<FolderStatistics> folderStats
    ) AnalyzeDuplicateFiles()
    {
        return DatabaseHelper.ExecuteQuery(
            @"SELECT Hash, FilePath, FileSize, FileCount
              FROM DuplicateFiles
              WHERE (IsDeleted = 0 OR IsDeleted IS NULL) AND FileCount > 1
              ORDER BY Hash",
            reader =>
            {
                var duplicateGroups = new Dictionary<string, DuplicateGroup>();
                var folderFileCount = new Dictionary<string, int>();
                var folderDuplicateCount = new Dictionary<string, int>();
                var folderDuplicateSize = new Dictionary<string, long>();

                while (reader.Read())
                {
                    var hash = reader.GetString(0);
                    var filePath = reader.GetString(1);
                    var fileSize = reader.GetInt64(2);
                    var fileCount = reader.GetInt32(3);

                    // 收集重複群組資料
                    if (!duplicateGroups.ContainsKey(hash))
                    {
                        duplicateGroups[hash] = new DuplicateGroup
                        {
                            Hash = hash,
                            FileSize = fileSize,
                            FileCount = fileCount,
                            FilePaths = new List<string>()
                        };
                    }

                    duplicateGroups[hash].FilePaths.Add(filePath);

                    // 統計資料夾資訊
                    var folder = Path.GetDirectoryName(filePath) ?? "";

                    if (!folderFileCount.ContainsKey(folder))
                    {
                        folderFileCount[folder] = 0;
                        folderDuplicateCount[folder] = 0;
                        folderDuplicateSize[folder] = 0;
                    }

                    folderFileCount[folder]++;
                    folderDuplicateCount[folder]++;
                    folderDuplicateSize[folder] += fileSize;
                }

                var duplicateData = duplicateGroups.Values.ToList();
                var totalFiles = duplicateData.Sum(g => g.FileCount);
                var totalDuplicates = duplicateData.Sum(g => g.FileCount - 1);
                var totalDuplicateSize = duplicateData.Sum(g => g.FileSize * (g.FileCount - 1));

                // 計算檔案大小分布
                var fileSizeDistribution = new FileSizeDistribution
                {
                    Small = duplicateData.Count(g => g.FileSize < 1024 * 1024),
                    Medium = duplicateData.Count(g => g.FileSize >= 1024 * 1024 && g.FileSize < 10 * 1024 * 1024),
                    Large = duplicateData.Count(g => g.FileSize >= 10 * 1024 * 1024 && g.FileSize < 100 * 1024 * 1024),
                    VeryLarge = duplicateData.Count(g => g.FileSize >= 100 * 1024 * 1024)
                };

                // 建立資料夾統計資料
                var folderStats = folderFileCount
                    .Select(kvp => new FolderStatistics
                    {
                        Path = kvp.Key,
                        TotalCount = kvp.Value,
                        DuplicateCount = folderDuplicateCount[kvp.Key],
                        DuplicateSize = folderDuplicateSize[kvp.Key],
                        DuplicateRate = (double)folderDuplicateCount[kvp.Key] / kvp.Value
                    })
                    .ToList();

                return (duplicateData, totalFiles, totalDuplicates, totalDuplicateSize, fileSizeDistribution, folderStats);
            });
    }

    /// <summary>
    /// 儲存詳細分析報表到 JSON 檔案
    /// </summary>
    static void SaveDetailedAnalysisReport(
        List<DuplicateGroup> duplicateData,
        List<FolderStatistics> folderStats,
        FileSizeDistribution fileSizeDistribution)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var reportData = new
        {
            GeneratedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            Summary = new
            {
                TotalGroups = duplicateData.Count,
                TotalFiles = duplicateData.Sum(g => g.FileCount),
                TotalDuplicates = duplicateData.Sum(g => g.FileCount - 1),
                TotalDuplicateSize = duplicateData.Sum(g => g.FileSize * (g.FileCount - 1)),
                PotentialSavings = duplicateData.Sum(g => g.FileSize * (g.FileCount - 1))
            },
            FileSizeDistribution = fileSizeDistribution,
            TopFoldersByDuplicateRate = folderStats
                .OrderByDescending(f => f.DuplicateRate)
                .ThenByDescending(f => f.DuplicateCount)
                .Take(50)
                .Select(f => new
                {
                    f.Path,
                    f.TotalCount,
                    f.DuplicateCount,
                    DuplicateRate = $"{f.DuplicateRate:P2}",
                    DuplicateSize = f.DuplicateSize,
                    DuplicateSizeFormatted = FormatFileSize(f.DuplicateSize)
                }),
            LargeFileDuplicates = duplicateData
                .Where(g => g.FileSize > 10 * 1024 * 1024)
                .OrderByDescending(g => g.FileSize * (g.FileCount - 1))
                .Take(100)
                .Select(g => new
                {
                    g.Hash,
                    FileSize = g.FileSize,
                    FileSizeFormatted = FormatFileSize(g.FileSize),
                    g.FileCount,
                    WastedSpace = g.FileSize * (g.FileCount - 1),
                    WastedSpaceFormatted = FormatFileSize(g.FileSize * (g.FileCount - 1)),
                    SamplePath = g.FilePaths.FirstOrDefault()
                })
        };

        var jsonFileName = $"Reports/DuplicateAnalysisReport_{timestamp}.json";
        var json = System.Text.Json.JsonSerializer.Serialize(reportData, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        File.WriteAllText(jsonFileName, json, Encoding.UTF8);
        Console.WriteLine($"詳細分析報表已產生：{Path.GetFullPath(jsonFileName)}");
    }

    /// <summary>
    /// 重複檔案群組資料
    /// </summary>
    class DuplicateGroup
    {
        public string Hash { get; set; } = "";
        public long FileSize { get; set; }
        public int FileCount { get; set; }
        public List<string> FilePaths { get; set; } = new();
    }

    /// <summary>
    /// 檔案大小分布統計
    /// </summary>
    class FileSizeDistribution
    {
        public int Small { get; set; }      // < 1 MB
        public int Medium { get; set; }     // 1-10 MB
        public int Large { get; set; }      // 10-100 MB
        public int VeryLarge { get; set; }  // > 100 MB
    }

    /// <summary>
    /// 資料夾統計資料
    /// </summary>
    class FolderStatistics
    {
        public string Path { get; set; } = "";
        public int TotalCount { get; set; }
        public int DuplicateCount { get; set; }
        public long DuplicateSize { get; set; }
        public double DuplicateRate { get; set; }
    }
}