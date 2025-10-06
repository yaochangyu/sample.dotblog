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
            Console.WriteLine("8. 離開");
            Console.Write("請輸入選項 (1-8): ");

            var menuChoice = Console.ReadLine()?.Trim();
            Console.WriteLine();

            if (menuChoice == "8")
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
                var (existingHashes, _) = LoadExistingHashes();

                if (existingHashes.Count == 0)
                {
                    Console.WriteLine("資料庫中沒有重複檔案記錄，請先執行掃描！");
                    Console.WriteLine();
                    continue;
                }

                InteractiveDeleteDuplicates(existingHashes);
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
        }
    }

    static void RunScanMode()
    {
        // 接收使用者輸入多個資料夾路徑
        var folderPaths = new List<string>();

        while (true)
        {
            Console.Write($"請輸入要掃描的資料夾路徑 ({folderPaths.Count + 1}): ");
            var input = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(input))
            {
                if (folderPaths.Count == 0)
                {
                    Console.WriteLine("錯誤：至少需要輸入一個路徑！");
                    continue;
                }

                break;
            }

            if (!Directory.Exists(input))
            {
                Console.WriteLine($"錯誤：路徑不存在或無效，請重新輸入！");
                continue;
            }

            folderPaths.Add(input);
            Console.WriteLine($"已加入路徑: {input}");
            Console.WriteLine();

            Console.Write("是否要繼續加入路徑？(y/N): ");
            var continueInput = Console.ReadLine()?.Trim().ToUpper();

            if (string.IsNullOrEmpty(continueInput) || (continueInput != "Y" && continueInput != "YES"))
            {
                break;
            }

            Console.WriteLine();
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

    static void InteractiveDeleteDuplicates(Dictionary<string, List<string>> hashGroups)
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

                var validFiles = g.Value.Where(File.Exists).ToList();
                var unmarkedFiles = validFiles.Where(f => !markedFiles.Contains(f)).ToList();

                // 檢查此 hash 是否已經完全處理過
                // 如果該 hash 在 FilesToDelete 中已有標記，且只剩一個檔案未標記，表示已處理完成
                if (markedFilesByHash.ContainsKey(hash))
                {
                    var markedCount = markedFilesByHash[hash].Count;
                    var totalCount = validFiles.Count;

                    // 如果已標記數量 >= 總數量 - 1，表示已經處理完成（保留了一個檔案）
                    if (markedCount >= totalCount - 1)
                    {
                        return false;
                    }
                }

                // 至少要有一個未標記的檔案
                return unmarkedFiles.Count > 0;
            })
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

            var validFiles = group.Value.Where(File.Exists).ToList();
            if (validFiles.Count == 0)
            {
                Console.WriteLine("此組所有檔案都已不存在，跳過...");
                Console.WriteLine();
                groupIndex++;
                continue;
            }

            // 顯示檔案清單
            for (int i = 0; i < validFiles.Count; i++)
            {
                var fileInfo = new FileInfo(validFiles[i]);
                var isMarked = markedFiles.Contains(validFiles[i]);
                var markIndicator = isMarked ? " [已標記]" : "";

                Console.WriteLine($"[{i + 1}] {validFiles[i]}{markIndicator}");
                Console.WriteLine($"    大小: {FormatFileSize(fileInfo.Length)}");
                Console.WriteLine($"    建立時間: {fileInfo.CreationTime:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"    修改時間: {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine();
            }

            // 詢問操作
            while (true)
            {
                Console.WriteLine("操作選項:");
                Console.WriteLine("  輸入編號標記該檔案為待刪除（可用逗號分隔多個，例如: 1,3,5）");
                Console.WriteLine("  輸入 'p' 或 'p 編號' 預覽檔案（例如: p 1,2 或 p 預覽所有）");
                Console.WriteLine("  輸入 'k' 保留所有檔案並跳過此組");
                Console.WriteLine("  輸入 'a' 自動保留最舊的檔案，標記其他為待刪除");
                Console.WriteLine("  輸入 'q' 結束作業");
                Console.Write("請選擇: ");

                var choice = Console.ReadLine()?.Trim().ToLower();

                if (choice == "q")
                {
                    Console.WriteLine("已結束標記作業");
                    return;
                }

                // 處理預覽指令
                if (choice?.StartsWith("p") == true)
                {
                    var previewPart = choice.Substring(1).Trim();

                    if (string.IsNullOrEmpty(previewPart))
                    {
                        // 預覽所有檔案
                        Console.WriteLine("正在開啟所有檔案進行預覽...");
                        PreviewFiles(validFiles);
                        Console.WriteLine();
                        continue;
                    }

                    // 解析要預覽的檔案編號
                    var previewIndices = previewPart.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .Where(s => int.TryParse(s, out _))
                        .Select(int.Parse)
                        .Where(i => i >= 1 && i <= validFiles.Count)
                        .Distinct()
                        .ToList();

                    if (previewIndices.Count > 0)
                    {
                        var filesToPreview = previewIndices.Select(i => validFiles[i - 1]).ToList();
                        Console.WriteLine($"正在開啟 {filesToPreview.Count} 個檔案進行預覽...");
                        PreviewFiles(filesToPreview);
                    }
                    else
                    {
                        Console.WriteLine("無效的預覽編號!");
                    }

                    Console.WriteLine();
                    continue;
                }

                if (choice == "k")
                {
                    // 記錄跳過的 hash 及所有檔案路徑
                    MarkHashAsSkipped(group.Key, validFiles);
                    Console.WriteLine("已跳過此組並記錄，下次不會再顯示此組");
                    Console.WriteLine();
                    break;
                }

                if (choice == "a")
                {
                    // 自動保留最舊的檔案
                    var oldestFile = validFiles
                        .Select(f => new { Path = f, Info = new FileInfo(f) })
                        .OrderBy(x => x.Info.CreationTime)
                        .First();

                    var autoDeleteFiles = validFiles.Where(f => f != oldestFile.Path).ToList();

                    Console.WriteLine($"保留最舊的檔案: {oldestFile.Path}");
                    Console.WriteLine($"建立時間: {oldestFile.Info.CreationTime:yyyy-MM-dd HH:mm:ss}");
                    Console.WriteLine();
                    Console.WriteLine($"將標記以下 {autoDeleteFiles.Count} 個檔案為待刪除：");
                    foreach (var file in autoDeleteFiles)
                    {
                        var fileInfo = new FileInfo(file);
                        Console.WriteLine($"  - {file}");
                        Console.WriteLine($"    建立時間: {fileInfo.CreationTime:yyyy-MM-dd HH:mm:ss}");
                    }

                    Console.WriteLine();
                    Console.Write("確認標記？(Y/n): ");

                    var autoConfirm = Console.ReadLine()?.Trim().ToUpper();
                    if (string.IsNullOrEmpty(autoConfirm) || autoConfirm == "Y" || autoConfirm == "YES")
                    {
                        if (MarkFilesForDeletion(group.Key, autoDeleteFiles))
                        {
                            Console.WriteLine("標記完成！");

                            // 更新本地標記清單
                            foreach (var file in autoDeleteFiles)
                            {
                                markedFiles.Add(file);
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("已取消標記");
                    }

                    Console.WriteLine();
                    break;
                }

                // 解析編號
                var indices = choice?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => int.TryParse(s, out _))
                    .Select(int.Parse)
                    .Where(i => i >= 1 && i <= validFiles.Count)
                    .Distinct()
                    .ToList() ?? new List<int>();

                if (indices.Count == 0)
                {
                    Console.WriteLine("無效的選擇，請重新輸入！");
                    Console.WriteLine();
                    continue;
                }

                if (indices.Count == validFiles.Count)
                {
                    Console.WriteLine("錯誤：不能刪除所有檔案，至少要保留一個！");
                    Console.WriteLine();
                    continue;
                }

                var filesToDelete = indices.Select(i => validFiles[i - 1]).ToList();

                // 檢查是否有已標記的檔案
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

                Console.Write("確認標記？(Y/n): ");

                var confirm = Console.ReadLine()?.Trim().ToUpper();
                if (string.IsNullOrEmpty(confirm) || confirm == "Y" || confirm == "YES")
                {
                    // 先取消已標記的檔案
                    if (alreadyMarked.Count > 0)
                    {
                        UnmarkFiles(alreadyMarked);
                        foreach (var file in alreadyMarked)
                        {
                            markedFiles.Remove(file);
                        }
                    }

                    // 標記所有檔案
                    if (MarkFilesForDeletion(group.Key, filesToDelete))
                    {
                        Console.WriteLine("標記完成！");

                        // 更新本地標記清單
                        foreach (var file in filesToDelete)
                        {
                            markedFiles.Add(file);
                        }
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
        using var connection = new SqliteConnection($"Data Source={DatabaseFileName}");
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT OR IGNORE INTO FilesToDelete (Hash, FilePath, MarkedAt)
            VALUES ($hash, $filePath, $markedAt);
        ";

        var filePathParam = command.CreateParameter();
        filePathParam.ParameterName = "$filePath";
        filePathParam.Value = filePath;
        command.Parameters.Add(filePathParam);

        var hashParam = command.CreateParameter();
        hashParam.ParameterName = "$hash";
        hashParam.Value = (object)hash ?? DBNull.Value;
        command.Parameters.Add(hashParam);

        var markedAtParam = command.CreateParameter();
        markedAtParam.ParameterName = "$markedAt";
        markedAtParam.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        command.Parameters.Add(markedAtParam);

        command.ExecuteNonQuery();
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
            Console.Write("是否要清除這些記錄？(Y/n): ");
            var clearChoice = Console.ReadLine()?.Trim().ToUpper();

            if (string.IsNullOrEmpty(clearChoice) || clearChoice == "Y" || clearChoice == "YES")
            {
                ClearDeletedMarks();
            }

            return;
        }

        Console.Write($"確認要刪除這 {existingFiles.Count} 個檔案嗎？(Y/n): ");
        var confirm = Console.ReadLine()?.Trim().ToUpper();

        if (!string.IsNullOrEmpty(confirm) && confirm != "Y" && confirm != "YES")
        {
            Console.WriteLine("已取消刪除操作");
            return;
        }

        Console.WriteLine();
        Console.WriteLine("開始刪除檔案...");

        int successCount = 0;
        int failCount = 0;

        foreach (var (path, _) in existingFiles)
        {
            try
            {
                File.Delete(path);
                Console.WriteLine($"✓ 已刪除: {path}");
                successCount++;

                // 從標記表中移除
                var deleteCommand = connection.CreateCommand();
                deleteCommand.CommandText = "DELETE FROM FilesToDelete WHERE FilePath = $path";
                var pathParam = deleteCommand.CreateParameter();
                pathParam.ParameterName = "$path";
                pathParam.Value = path;
                deleteCommand.Parameters.Add(pathParam);
                deleteCommand.ExecuteNonQuery();
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
            Console.Write($"是否要清除 {missingFiles.Count} 個已不存在的檔案記錄？(Y/n): ");
            var clearChoice = Console.ReadLine()?.Trim().ToUpper();

            if (string.IsNullOrEmpty(clearChoice) || clearChoice == "Y" || clearChoice == "YES")
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

            var existingFiles = files.Where(f => File.Exists(f.path)).ToList();
            var missingFiles = files.Where(f => !File.Exists(f.path)).ToList();

            if (existingFiles.Count > 0)
            {
                Console.WriteLine($"    存在的檔案 ({existingFiles.Count} 個):");
                foreach (var (path, _) in existingFiles)
                {
                    var fileInfo = new FileInfo(path);
                    Console.WriteLine($"      - {path}");
                    Console.WriteLine($"        大小: {FormatFileSize(fileInfo.Length)}");
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

            Console.WriteLine();
            groupIndex++;
        }

        // 操作選單
        while (true)
        {
            Console.WriteLine("操作選項：");
            Console.WriteLine("  輸入編號取消略過該群組（可用逗號分隔多個，例如: 1,3,5）");
            Console.WriteLine("  輸入 'p' 或 'p 編號' 預覽群組檔案（例如: p 1,2 或 p 預覽所有）");
            Console.WriteLine("  輸入 'a' 清除所有略過標記");
            Console.WriteLine("  輸入 'q' 返回主選單");
            Console.Write("請選擇: ");

            var choice = Console.ReadLine()?.Trim().ToLower();
            Console.WriteLine();

            if (choice == "q")
            {
                return;
            }

            // 處理預覽指令
            if (choice?.StartsWith("p") == true)
            {
                var previewPart = choice.Substring(1).Trim();

                if (string.IsNullOrEmpty(previewPart))
                {
                    // 預覽所有群組的檔案
                    var allFiles = groupList
                        .SelectMany(g => g.files)
                        .Where(f => File.Exists(f.path))
                        .Select(f => f.path)
                        .ToList();

                    if (allFiles.Count == 0)
                    {
                        Console.WriteLine("沒有存在的檔案可以預覽！");
                        Console.WriteLine();
                        continue;
                    }

                    Console.WriteLine($"正在開啟所有群組的 {allFiles.Count} 個檔案進行預覽...");
                    PreviewFiles(allFiles);
                    Console.WriteLine();
                    continue;
                }

                // 解析要預覽的群組編號
                var previewIndices = previewPart.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => int.TryParse(s, out _))
                    .Select(int.Parse)
                    .Where(i => i >= 1 && i <= groupList.Count)
                    .Distinct()
                    .ToList();

                if (previewIndices.Count > 0)
                {
                    var filesToPreview = previewIndices
                        .SelectMany(i => groupList[i - 1].files)
                        .Where(f => File.Exists(f.path))
                        .Select(f => f.path)
                        .ToList();

                    if (filesToPreview.Count == 0)
                    {
                        Console.WriteLine("選擇的群組中沒有存在的檔案可以預覽！");
                        Console.WriteLine();
                        continue;
                    }

                    Console.WriteLine($"正在開啟 {previewIndices.Count} 組共 {filesToPreview.Count} 個檔案進行預覽...");
                    PreviewFiles(filesToPreview);
                }
                else
                {
                    Console.WriteLine("無效的預覽編號!");
                }

                Console.WriteLine();
                continue;
            }

            if (choice == "a")
            {
                Console.Write($"確認要清除所有 {skippedGroups.Count} 組略過標記嗎？(Y/n): ");
                var clearConfirm = Console.ReadLine()?.Trim().ToUpper();

                if (string.IsNullOrEmpty(clearConfirm) || clearConfirm == "Y" || clearConfirm == "YES")
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

            var indices = choice?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => int.TryParse(s, out _))
                .Select(int.Parse)
                .Where(i => i >= 1 && i <= groupList.Count)
                .Distinct()
                .ToList() ?? new List<int>();

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

            Console.Write("確認取消略過標記？(Y/n): ");

            var confirm = Console.ReadLine()?.Trim().ToUpper();
            if (string.IsNullOrEmpty(confirm) || confirm == "Y" || confirm == "YES")
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
            Console.WriteLine("操作選項：");
            Console.WriteLine("  輸入編號取消標記（可用逗號分隔多個，例如: 1,3,5）");
            Console.WriteLine("  輸入 'p' 或 'p 編號' 預覽檔案（例如: p 1,2 或 p 預覽所有）");
            Console.WriteLine("  輸入 'a' 清除所有標記");
            Console.WriteLine("  輸入 'q' 返回主選單");
            Console.Write("請選擇: ");

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

                var previewPart = choice.Substring(1).Trim();

                if (string.IsNullOrEmpty(previewPart))
                {
                    // 預覽所有檔案
                    var allPaths = existingFiles.Select(f => f.path).ToList();
                    Console.WriteLine("正在開啟所有檔案進行預覽...");
                    PreviewFiles(allPaths);
                    Console.WriteLine();
                    continue;
                }

                // 解析要預覽的檔案編號
                var previewIndices = previewPart.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => int.TryParse(s, out _))
                    .Select(int.Parse)
                    .Where(i => i >= 1 && i <= existingFiles.Count)
                    .Distinct()
                    .ToList();

                if (previewIndices.Count > 0)
                {
                    var filesToPreview = previewIndices.Select(i => existingFiles[i - 1].path).ToList();
                    Console.WriteLine($"正在開啟 {filesToPreview.Count} 個檔案進行預覽...");
                    PreviewFiles(filesToPreview);
                }
                else
                {
                    Console.WriteLine("無效的預覽編號!");
                }

                Console.WriteLine();
                continue;
            }

            if (choice == "a")
            {
                Console.Write($"確認要清除所有 {markedFiles.Count} 個標記嗎？(Y/n): ");
                var clearConfirm = Console.ReadLine()?.Trim().ToUpper();

                if (string.IsNullOrEmpty(clearConfirm) || clearConfirm == "Y" || clearConfirm == "YES")
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

            var indices = choice?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => int.TryParse(s, out _))
                .Select(int.Parse)
                .Where(i => i >= 1 && i <= existingFiles.Count)
                .Distinct()
                .ToList() ?? new List<int>();

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

            Console.Write("確認取消標記？(Y/n): ");

            var confirm = Console.ReadLine()?.Trim().ToUpper();
            if (string.IsNullOrEmpty(confirm) || confirm == "Y" || confirm == "YES")
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
        var markedFiles = new List<(string, string)>();

        try
        {
            using var connection = new SqliteConnection($"Data Source={DatabaseFileName}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT FilePath, MarkedAt FROM FilesToDelete ORDER BY MarkedAt";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                markedFiles.Add((reader.GetString(0), reader.GetString(1)));
            }
        }
        catch
        {
            // 資料表可能不存在，忽略錯誤
        }

        return markedFiles;
    }

    static Dictionary<string, List<(string path, string markedAt)>> LoadMarkedForDeletionFilesGroupedByHash()
    {
        var markedGroups = new Dictionary<string, List<(string, string)>>();

        try
        {
            using var connection = new SqliteConnection($"Data Source={DatabaseFileName}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT Hash, FilePath, MarkedAt FROM FilesToDelete WHERE Hash IS NOT NULL ORDER BY Hash, MarkedAt";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var hash = reader.GetString(0);
                var filePath = reader.GetString(1);
                var markedAt = reader.GetString(2);

                if (!markedGroups.ContainsKey(hash))
                {
                    markedGroups[hash] = new List<(string, string)>();
                }

                markedGroups[hash].Add((filePath, markedAt));
            }
        }
        catch
        {
            // 資料表可能不存在或沒有 Hash 欄位，忽略錯誤
        }

        return markedGroups;
    }

    static void UnmarkFiles(List<string> filePaths)
    {
        using var connection = new SqliteConnection($"Data Source={DatabaseFileName}");
        connection.Open();

        using var transaction = connection.BeginTransaction();

        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM FilesToDelete WHERE FilePath = $path";
        var pathParam = command.CreateParameter();
        pathParam.ParameterName = "$path";
        command.Parameters.Add(pathParam);

        foreach (var path in filePaths)
        {
            pathParam.Value = path;
            command.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    static void ClearAllMarks()
    {
        using var connection = new SqliteConnection($"Data Source={DatabaseFileName}");
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM FilesToDelete";
        command.ExecuteNonQuery();
    }

    static HashSet<string> LoadMarkedFiles()
    {
        var markedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            using var connection = new SqliteConnection($"Data Source={DatabaseFileName}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT FilePath FROM FilesToDelete";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                markedFiles.Add(reader.GetString(0));
            }
        }
        catch
        {
            // 資料表可能不存在，忽略錯誤
        }

        return markedFiles;
    }

    static Dictionary<string, HashSet<string>> LoadMarkedFilesByHash()
    {
        var markedFilesByHash = new Dictionary<string, HashSet<string>>();

        try
        {
            using var connection = new SqliteConnection($"Data Source={DatabaseFileName}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT Hash, FilePath FROM FilesToDelete WHERE Hash IS NOT NULL";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var hash = reader.GetString(0);
                var filePath = reader.GetString(1);

                if (!markedFilesByHash.ContainsKey(hash))
                {
                    markedFilesByHash[hash] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                }

                markedFilesByHash[hash].Add(filePath);
            }
        }
        catch
        {
            // 資料表可能不存在或沒有 Hash 欄位，忽略錯誤
        }

        return markedFilesByHash;
    }

    static HashSet<string> LoadSkippedHashes()
    {
        var skippedHashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            using var connection = new SqliteConnection($"Data Source={DatabaseFileName}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT Hash FROM SkippedHashes";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                skippedHashes.Add(reader.GetString(0));
            }
        }
        catch
        {
            // 資料表可能不存在，忽略錯誤
        }

        return skippedHashes;
    }

    static void MarkHashAsSkipped(string hash, List<string> filePaths)
    {
        using var connection = new SqliteConnection($"Data Source={DatabaseFileName}");
        connection.Open();

        using var transaction = connection.BeginTransaction();

        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT OR IGNORE INTO SkippedHashes (Hash, FilePath, SkippedAt)
            VALUES ($hash, $filePath, $skippedAt);
        ";

        var hashParam = command.CreateParameter();
        hashParam.ParameterName = "$hash";
        command.Parameters.Add(hashParam);

        var filePathParam = command.CreateParameter();
        filePathParam.ParameterName = "$filePath";
        command.Parameters.Add(filePathParam);

        var skippedAtParam = command.CreateParameter();
        skippedAtParam.ParameterName = "$skippedAt";
        skippedAtParam.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        command.Parameters.Add(skippedAtParam);

        foreach (var filePath in filePaths)
        {
            hashParam.Value = hash;
            filePathParam.Value = filePath;
            command.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    static Dictionary<string, List<(string path, string skippedAt)>> LoadSkippedFilesGroupedByHash()
    {
        var skippedGroups = new Dictionary<string, List<(string, string)>>();

        try
        {
            using var connection = new SqliteConnection($"Data Source={DatabaseFileName}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT Hash, FilePath, SkippedAt FROM SkippedHashes ORDER BY Hash, SkippedAt";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var hash = reader.GetString(0);
                var filePath = reader.GetString(1);
                var skippedAt = reader.GetString(2);

                if (!skippedGroups.ContainsKey(hash))
                {
                    skippedGroups[hash] = new List<(string, string)>();
                }

                skippedGroups[hash].Add((filePath, skippedAt));
            }
        }
        catch
        {
            // 資料表可能不存在，忽略錯誤
        }

        return skippedGroups;
    }

    static void UnskipHashes(List<string> hashes)
    {
        using var connection = new SqliteConnection($"Data Source={DatabaseFileName}");
        connection.Open();

        using var transaction = connection.BeginTransaction();

        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM SkippedHashes WHERE Hash = $hash";
        var hashParam = command.CreateParameter();
        hashParam.ParameterName = "$hash";
        command.Parameters.Add(hashParam);

        foreach (var hash in hashes)
        {
            hashParam.Value = hash;
            command.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    static void ClearAllSkippedMarks()
    {
        using var connection = new SqliteConnection($"Data Source={DatabaseFileName}");
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM SkippedHashes";
        command.ExecuteNonQuery();
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

        GenerateHtmlReport(skippedGroups);
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

        GenerateMarkedForDeletionHtmlReport(markedFilesGrouped);
    }

    static string GenerateJsonReport(Dictionary<string, List<(string path, string skippedAt)>> skippedGroups, string fileName = null)
    {
        var reportData = new
        {
            GeneratedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            TotalGroups = skippedGroups.Count,
            Groups = skippedGroups.Select(g => new
            {
                Hash = g.Key,
                SkippedAt = g.Value.First().skippedAt,
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

        var json = System.Text.Json.JsonSerializer.Serialize(reportData, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        if (string.IsNullOrEmpty(fileName))
        {
            fileName = $"SkippedFilesReport_{DateTime.Now:yyyyMMdd_HHmmss}.json";
        }

        var fullPath = $"Reports/{fileName}";
        File.WriteAllText(fullPath, json, Encoding.UTF8);

        return fullPath;
    }

    static string GenerateMarkedForDeletionJsonReport(Dictionary<string, List<(string path, string markedAt)>> markedGroups, string fileName = null)
    {
        var reportData = new
        {
            GeneratedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            TotalGroups = markedGroups.Count,
            Groups = markedGroups.Select(g => new
            {
                Hash = g.Key,
                MarkedAt = g.Value.First().markedAt,
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

        var json = System.Text.Json.JsonSerializer.Serialize(reportData, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        if (string.IsNullOrEmpty(fileName))
        {
            fileName = $"MarkedForDeletionReport_{DateTime.Now:yyyyMMdd_HHmmss}.json";
        }

        var fullPath = $"Reports/{fileName}";
        File.WriteAllText(fullPath, json, Encoding.UTF8);

        return fullPath;
    }

    static void GenerateHtmlReport(Dictionary<string, List<(string path, string skippedAt)>> skippedGroups)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

        // 1. 先產生 JSON 檔案（供獨立使用）
        var jsonFileName = GenerateJsonReport(skippedGroups, $"SkippedFilesReport_{timestamp}.json");

        // 2. 建立報表資料物件
        var reportData = new
        {
            GeneratedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            TotalGroups = skippedGroups.Count,
            Groups = skippedGroups.Select(g => new
            {
                Hash = g.Key,
                SkippedAt = g.Value.First().skippedAt,
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

        // 3. 序列化為 JSON（不縮排，減少檔案大小）
        var json = System.Text.Json.JsonSerializer.Serialize(reportData, new System.Text.Json.JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        // 4. 讀取 HTML 模板並替換 JSON 資料
        var template = File.ReadAllText("Templates/SkippedFilesReport.html", Encoding.UTF8);
        var html = template.Replace("{{REPORT_DATA}}", json);

        // 5. 產生 HTML 檔案
        var htmlFileName = $"Reports/SkippedFilesReport_{timestamp}.html";
        File.WriteAllText(htmlFileName, html, Encoding.UTF8);

        var totalFiles = skippedGroups.Sum(g => g.Value.Count);
        var totalExistingFiles = skippedGroups.Sum(g => g.Value.Count(f => File.Exists(f.path)));
        var totalMissingFiles = totalFiles - totalExistingFiles;

        Console.WriteLine($"JSON 報表已產生：{Path.GetFullPath(jsonFileName)}");
        Console.WriteLine($"HTML 報表已產生：{Path.GetFullPath(htmlFileName)}");
        Console.WriteLine($"總共 {skippedGroups.Count} 組，{totalFiles} 個檔案（存在：{totalExistingFiles}，遺失：{totalMissingFiles}）");
    }

    static void GenerateMarkedForDeletionHtmlReport(Dictionary<string, List<(string path, string markedAt)>> markedGroups)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

        // 1. 先產生 JSON 檔案（供獨立使用）
        var jsonFileName = GenerateMarkedForDeletionJsonReport(markedGroups, $"MarkedForDeletionReport_{timestamp}.json");

        // 2. 建立報表資料物件
        var reportData = new
        {
            GeneratedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            TotalGroups = markedGroups.Count,
            Groups = markedGroups.Select(g => new
            {
                Hash = g.Key,
                MarkedAt = g.Value.First().markedAt,
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

        // 3. 序列化為 JSON（不縮排，減少檔案大小）
        var json = System.Text.Json.JsonSerializer.Serialize(reportData, new System.Text.Json.JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        // 4. 讀取 HTML 模板並替換 JSON 資料
        var template = File.ReadAllText("Templates/MarkedForDeletionReport.html", Encoding.UTF8);
        var html = template.Replace("{{REPORT_DATA}}", json);

        // 5. 產生 HTML 檔案
        var htmlFileName = $"Reports/MarkedForDeletionReport_{timestamp}.html";
        File.WriteAllText(htmlFileName, html, Encoding.UTF8);

        var totalFiles = markedGroups.Sum(g => g.Value.Count);
        var totalExistingFiles = markedGroups.Sum(g => g.Value.Count(f => File.Exists(f.path)));
        var totalMissingFiles = totalFiles - totalExistingFiles;

        Console.WriteLine($"JSON 報表已產生：{Path.GetFullPath(jsonFileName)}");
        Console.WriteLine($"HTML 報表已產生：{Path.GetFullPath(htmlFileName)}");
        Console.WriteLine($"總共 {markedGroups.Count} 組，{totalFiles} 個檔案（存在：{totalExistingFiles}，已刪除/遺失：{totalMissingFiles}）");
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