using System.Security.Cryptography;
using System.Text;
using System.Net;
using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace Lab.CleanDuplicatesImage;

/// <summary>
/// 應用程式設定
/// </summary>
public record AppSettings
{
    public string DefaultMoveTargetBasePath { get; init; } = @"C:\Users\clove\OneDrive\圖片";
}

/// <summary>
/// 命令處理結果
/// </summary>
enum CommandResult
{
    Continue,    // 繼續等待下一個命令
    NextGroup,   // 移至下一個檔案組
    Quit         // 結束整個互動流程
}

/// <summary>
/// 資料夾重命名動作記錄
/// </summary>
/// <param name="OriginalName">原始資料夾名稱</param>
/// <param name="NewName">新資料夾名稱</param>
/// <param name="FullPath">完整路徑</param>
/// <param name="ParentPath">父資料夾路徑</param>
/// <param name="HasConflict">是否有命名衝突</param>
record RenameAction(
    string OriginalName,
    string NewName,
    string FullPath,
    string ParentPath,
    bool HasConflict = false
);

class Program
{
    /// <summary>
    /// 設定檔案名稱
    /// </summary>
    private const string SettingsFileName = "appsettings.json";

    /// <summary>
    /// 應用程式設定實例
    /// </summary>
    private static AppSettings _settings = LoadSettings();

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

    /// <summary>
    /// 載入應用程式設定
    /// </summary>
    static AppSettings LoadSettings()
    {
        try
        {
            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(SettingsFileName, optional: true, reloadOnChange: false);

            var configRoot = configBuilder.Build();
            var settings = configRoot.GetSection("AppSettings").Get<AppSettings>();

            if (settings != null)
            {
                Console.WriteLine($"已載入設定檔: {SettingsFileName}");
                Console.WriteLine($"預設移動目標路徑: {settings.DefaultMoveTargetBasePath}");
                Console.WriteLine();
                return settings;
            }
            else
            {
                Console.WriteLine($"找不到設定檔 {SettingsFileName}，使用預設設定");
                Console.WriteLine();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"載入設定檔失敗: {ex.Message}");
            Console.WriteLine("使用預設設定");
            Console.WriteLine();
        }

        return new AppSettings();
    }

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
            Console.WriteLine("3. 查看並標記重複檔案 by HashCode");
            Console.WriteLine("4. 查看已標記刪除檔案（可取消標記）");
            Console.WriteLine("5. 查看已標記移動檔案（可取消標記）");
            Console.WriteLine("6. 查看已標記略過檔案（可取消略過）");
            Console.WriteLine("7. 執行刪除（刪除已標記的檔案）");
            Console.WriteLine("8. 執行移動（移動已標記的檔案）");
            Console.WriteLine("9. 查看檔案標記狀態綜合報表");
            Console.WriteLine("10. 移動資料夾到預設位置");
            Console.WriteLine("11. 資料夾民國年轉西元年");
            Console.WriteLine("12. 自動歸檔重複檔案（依權重自動標記）");
            Console.WriteLine("13. 自動清除所有標記（回到未標記狀態）");
            Console.WriteLine("14. 離開");
            Console.Write("請輸入選項 (1-14): ");

            var menuChoice = Console.ReadLine()?.Trim();
            Console.WriteLine();

            if (menuChoice == "14")
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
                // 依 Hash 查看並標記重複檔案
                InteractiveDeleteDuplicatesByHash();
                Console.WriteLine();
                Console.WriteLine("提示：標記為待刪除的檔案儲存在資料庫的 FilesToDelete 資料表中");
                Console.WriteLine("您可以查看該資料表後再決定是否實際刪除檔案");
                Console.WriteLine();
                continue;
            }

            if (menuChoice == "4")
            {
                // 查看已標記刪除檔案
                ViewMarkedForDeletionFiles();
                Console.WriteLine();
                continue;
            }

            if (menuChoice == "5")
            {
                // 查看已標記移動檔案
                ViewMarkedForMoveFiles();
                Console.WriteLine();
                continue;
            }

            if (menuChoice == "6")
            {
                // 查看已標記略過檔案
                ViewSkippedFiles();
                Console.WriteLine();
                continue;
            }

            if (menuChoice == "7")
            {
                // 執行實際刪除
                ExecuteMarkedDeletions();
                Console.WriteLine();
                continue;
            }

            if (menuChoice == "8")
            {
                // 執行標記的移動
                ExecuteMarkedMoves();
                Console.WriteLine();
                continue;
            }

            if (menuChoice == "9")
            {
                // 檔案標記狀態綜合報表
                GenerateComprehensiveFileStatusReport();
                Console.WriteLine();
                continue;
            }

            if (menuChoice == "10")
            {
                // 移動資料夾到預設位置
                var moveFolderHelper = new MoveFolderHelper(_settings);
                moveFolderHelper.RunMoveFolderToDefaultLocation();
                Console.WriteLine();
                continue;
            }

            if (menuChoice == "11")
            {
                // 資料夾民國年轉西元年
                RunROCFolderRename();
                Console.WriteLine();
                continue;
            }

            if (menuChoice == "12")
            {
                // 自動歸檔重複檔案（依權重自動標記）
                AutoArchiveDuplicates();
                Console.WriteLine();
                continue;
            }

            if (menuChoice == "13")
            {
                // 清除所有標記（回到未標記狀態）
                ResetAllMarksToUnmarked();
                Console.WriteLine();
                continue;
            }
        }
    }

    record FileDetails(string Path, long Size, string CreatedTime, string LastModifiedTime);

    static Dictionary<string, List<FileDetails>> LoadDuplicateGroupsWithDetails()
    {
        if (!File.Exists(DatabaseFileName))
        {
            return new Dictionary<string, List<FileDetails>>();
        }

        return DatabaseHelper.ExecuteQuery(
            @"SELECT Hash, FilePath, FileSize, FileCreatedTime, FileLastModifiedTime
              FROM DuplicateFiles
              WHERE MarkType = 0",
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

                    var lastModifiedTime = reader.IsDBNull(4) ? "-" : reader.GetString(4);
                    hashGroups[hash].Add(new FileDetails(
                        reader.GetString(1),
                        reader.GetInt64(2),
                        reader.GetString(3),
                        lastModifiedTime
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

        // 載入已處理的檔案及其 Hash
        var handledFiles = LoadHandledFiles();
        var handledFilesByHash = LoadHandledFilesByHash();
        var skippedHashes = LoadSkippedHashes();

        // 過濾出尚未完全處理的群組（至少保留一個檔案未處理）
        var groupsWithUnhandeledFiles = duplicateGroups
            .Where(g =>
            {
                var hash = g.Key;

                // 檢查是否已經被跳過
                if (skippedHashes.Contains(hash))
                {
                    return false;
                }

                var allFilePaths = g.Value.Select(f => f.Path).ToList();
                var unhandledFiles = allFilePaths.Where(f => !handledFiles.Contains(f)).ToList();

                // 檢查此 hash 是否已經完全處理過
                if (handledFilesByHash.ContainsKey(hash))
                {
                    var handledCount = handledFilesByHash[hash].Count;
                    var totalCount = allFilePaths.Count;

                    if (handledCount >= totalCount - 1)
                    {
                        return false;
                    }
                }

                return unhandledFiles.Count > 0;
            })
            .Select(g => new
            {
                Group = g,
                FileSize = g.Value.Select(f => f.Size).FirstOrDefault()
            })
            .OrderByDescending(x => x.FileSize) // 依檔案大小降序排列（大的優先）
            .Select(x => x.Group)
            .ToList();

        if (groupsWithUnhandeledFiles.Count == 0)
        {
            Console.WriteLine("所有重複檔案都已處理完成！");
            Console.WriteLine($"已處理 {handledFiles.Count} 個檔案");
            Console.WriteLine("請使用選項 3 或 4 查看或管理已標記的檔案");
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"找到 {groupsWithUnhandeledFiles.Count} 組尚有未處理檔案的重複檔案");
        if (handledFiles.Count > 0)
        {
            Console.WriteLine($"已處理 {handledFiles.Count} 個檔案");
        }

        Console.WriteLine();

        int groupIndex = 1;
        foreach (var group in groupsWithUnhandeledFiles)
        {
            Console.WriteLine($"=== 重複組 {groupIndex}/{groupsWithUnhandeledFiles.Count} ===");
            Console.WriteLine($"Hash: {group.Key}");
            Console.WriteLine($"共 {group.Value.Count} 個檔案:");
            Console.WriteLine();

            var filesInGroup = group.Value;
            
            // 顯示檔案清單
            for (int i = 0; i < filesInGroup.Count; i++)
            {
                var fileDetails = filesInGroup[i];
                var isHandled = handledFiles.Contains(fileDetails.Path);
                var markIndicator = isHandled ? " [已處理]" : "";

                Console.WriteLine($"[{i + 1}] {fileDetails.Path}{markIndicator}");
                Console.WriteLine($"    大小: {FormatFileSize(fileDetails.Size)}");
                Console.WriteLine($"    建立時間: {fileDetails.CreatedTime}");
                Console.WriteLine($"    最後修改日期: {fileDetails.LastModifiedTime}");
                Console.WriteLine();
            }

            // 詢問操作
            while (true)
            {
                var validFilePaths = filesInGroup.Select(f => f.Path).ToList();
                DisplayMenu(
                    "輸入 'd' 或 'd 編號' 標記該檔案為刪除（例如: d 1,2 或 d 標記所有）",
                    "輸入 'm' 或 'm 編號' 標記該檔案為移動（例如: m 1,2 或 m 標記所有）",
                    "輸入 'p' 或 'p 編號' 預覽檔案（例如: p 1,2 或 p 預覽所有）",
                    "輸入 'k' 保留所有檔案並跳過此組",
                    "輸入 'a' 自動保留最舊的檔案，標記其他為待刪除",
                    "輸入 'q' 結束作業",
                    "支援多命令：用分號分隔（例如: d 1,2;m 3,4）"
                );

                var input = Console.ReadLine()?.Trim();
                if (string.IsNullOrWhiteSpace(input))
                {
                    continue;
                }

                // 用分號分割多個命令
                var commands = input.Split(';')
                    .Select(cmd => cmd.Trim())
                    .Where(cmd => !string.IsNullOrWhiteSpace(cmd))
                    .ToList();

                CommandResult finalResult = CommandResult.Continue;

                // 逐一執行每個命令
                for (int i = 0; i < commands.Count; i++)
                {
                    var command = commands[i];
                    var isLastCommand = (i == commands.Count - 1);

                    var result = ProcessSingleCommand(
                        command,
                        group.Key,
                        filesInGroup,
                        validFilePaths,
                        handledFiles,
                        isLastCommand);

                    // 如果遇到 Quit 或 NextGroup，記錄並停止處理後續命令
                    if (result == CommandResult.Quit || result == CommandResult.NextGroup)
                    {
                        finalResult = result;
                        break;
                    }
                }

                // 根據最終結果決定流程
                if (finalResult == CommandResult.Quit)
                {
                    return;
                }
                else if (finalResult == CommandResult.NextGroup)
                {
                    break;
                }
                // CommandResult.Continue: 繼續等待下一個輸入
            }

            groupIndex++;
        }

        Console.WriteLine("所有重複檔案處理完成！");
    }

    /// <summary>
    /// 依 Hash 查看並標記重複檔案（互動模式）
    /// </summary>
    static void InteractiveDeleteDuplicatesByHash()
    {
        Console.WriteLine("=== 依 Hash 查看並標記重複檔案 ===");
        Console.WriteLine();

        Console.Write("請輸入 Hash (64 字元 SHA-256): ");
        string? hashInput = Console.ReadLine()?.Trim();

        if (!ValidateHashInput(hashInput))
        {
            Console.WriteLine("錯誤：無效的 Hash 格式。Hash 必須為 64 字元的 SHA-256 (0-9, a-f, A-F)");
            return;
        }

        InitializeDatabase();

        while (true)
        {
            // 查詢該 hash 的所有檔案（包含標記狀態）
            var filesWithStatus = DatabaseHelper.ExecuteQuery(
                @"SELECT FilePath, FileSize, FileCreatedTime, FileLastModifiedTime, MarkType
                  FROM DuplicateFiles
                  WHERE Hash = $hash",
                reader =>
                {
                    var fileList = new List<(string path, long size, string created, string modified, int markType)>();
                    while (reader.Read())
                    {
                        fileList.Add((
                            reader.GetString(0),
                            reader.GetInt64(1),
                            reader.GetString(2),
                            reader.IsDBNull(3) ? "-" : reader.GetString(3),
                            reader.GetInt32(4)
                        ));
                    }
                    return fileList;
                },
                cmd => cmd.Parameters.Add(new SqliteParameter("$hash", hashInput!))
            );

            if (filesWithStatus.Count == 0)
            {
                Console.WriteLine($"找不到 Hash 為 {hashInput} 的檔案");
                return;
            }

            Console.WriteLine($"=== 重複檔案群組 ===");
            Console.WriteLine($"Hash: {hashInput}");
            Console.WriteLine($"共 {filesWithStatus.Count} 個檔案:");
            Console.WriteLine();

            // 顯示檔案清單及標記狀態
            for (int i = 0; i < filesWithStatus.Count; i++)
            {
                var (path, size, created, modified, markType) = filesWithStatus[i];
                string statusText = markType switch
                {
                    0 => "[未標記]",
                    1 => "[已標記刪除]",
                    2 => "[已標記移動]",
                    3 => "[已標記略過]",
                    _ => "[未知狀態]"
                };

                Console.WriteLine($"[{i + 1}] {path} {statusText}");
                Console.WriteLine($"    大小: {FormatFileSize(size)}");
                Console.WriteLine($"    建立時間: {created}");
                Console.WriteLine($"    最後修改日期: {modified}");

                // 如果是已標記移動，顯示目標路徑
                if (markType == 2)
                {
                    var targetPath = CalculateTargetPath(path, _settings.DefaultMoveTargetBasePath);
                    Console.WriteLine($"    目標路徑: {targetPath}");
                }

                Console.WriteLine();
            }

            // 操作選單
            DisplayMenu(
                "輸入 'd 編號' 標記為刪除（例如: d 1,2）",
                "輸入 'm 編號' 標記為移動（例如: m 1,2）",
                "輸入 'k' 保留所有檔案並標記為略過",
                "輸入 'c 編號' 取消標記（例如: c 1,2）",
                "輸入 'p 編號' 預覽檔案（例如: p 1,2）",
                "輸入 'r' 重新載入檔案狀態",
                "輸入 'q' 返回主選單"
            );

            var choice = Console.ReadLine()?.Trim().ToLower();
            Console.WriteLine();

            if (choice == "q")
            {
                return;
            }

            if (choice == "r")
            {
                Console.WriteLine("重新載入檔案狀態...");
                Console.WriteLine();
                continue; // 重新查詢並顯示
            }

            // 處理預覽指令
            if (choice?.StartsWith("p") == true)
            {
                var existingPaths = filesWithStatus
                    .Where(f => File.Exists(f.path))
                    .Select(f => f.path)
                    .ToList();

                if (existingPaths.Count == 0)
                {
                    Console.WriteLine("沒有存在的檔案可以預覽！");
                    Console.WriteLine();
                    continue;
                }

                if (HandlePreviewCommand(choice, existingPaths))
                {
                    continue;
                }
            }

            // 處理保留並略過指令
            if (choice == "k")
            {
                var allPaths = filesWithStatus.Select(f => f.path).ToList();
                if (ConfirmAction($"確認要將這 {allPaths.Count} 個檔案標記為略過嗎？"))
                {
                    MarkHashAsSkipped(hashInput!, allPaths);
                    Console.WriteLine("已標記為略過！");
                    Console.WriteLine();
                    continue; // 重新載入顯示
                }
                else
                {
                    Console.WriteLine("已取消操作");
                    Console.WriteLine();
                    continue;
                }
            }

            // 處理標記為刪除指令 (d 1,2,3)
            if (choice?.StartsWith("d ") == true)
            {
                var indicesStr = choice.Substring(2).Trim();
                var indices = ParseIndices(indicesStr, filesWithStatus.Count);

                if (indices.Count == 0)
                {
                    Console.WriteLine("無效的編號，請重新輸入！");
                    Console.WriteLine();
                    continue;
                }

                // 只能標記未標記的檔案
                var filesToMark = indices
                    .Select(i => filesWithStatus[i - 1])
                    .Where(f => f.markType == 0)
                    .Select(f => f.path)
                    .ToList();

                var alreadyMarked = indices
                    .Select(i => filesWithStatus[i - 1])
                    .Where(f => f.markType != 0)
                    .ToList();

                if (alreadyMarked.Count > 0)
                {
                    Console.WriteLine($"以下 {alreadyMarked.Count} 個檔案已被標記，請先取消標記：");
                    foreach (var file in alreadyMarked)
                    {
                        Console.WriteLine($"  - {file.path}");
                    }
                    Console.WriteLine();
                }

                if (filesToMark.Count == 0)
                {
                    Console.WriteLine("沒有可標記的檔案！");
                    Console.WriteLine();
                    continue;
                }

                Console.WriteLine($"將標記以下 {filesToMark.Count} 個檔案為待刪除：");
                foreach (var file in filesToMark)
                {
                    Console.WriteLine($"  - {file}");
                }

                if (ConfirmAction("確認標記為刪除？"))
                {
                    if (MarkFilesForDeletion(hashInput!, filesToMark))
                    {
                        Console.WriteLine("標記完成！");
                        Console.WriteLine();
                        continue; // 重新載入顯示
                    }
                }
                else
                {
                    Console.WriteLine("已取消標記");
                    Console.WriteLine();
                }
                continue;
            }

            // 處理標記為移動指令 (m 1,2,3)
            if (choice?.StartsWith("m ") == true)
            {
                var indicesStr = choice.Substring(2).Trim();
                var indices = ParseIndices(indicesStr, filesWithStatus.Count);

                if (indices.Count == 0)
                {
                    Console.WriteLine("無效的編號，請重新輸入！");
                    Console.WriteLine();
                    continue;
                }

                // 只能標記未標記的檔案
                var filesToMark = indices
                    .Select(i => filesWithStatus[i - 1])
                    .Where(f => f.markType == 0)
                    .Select(f => f.path)
                    .ToList();

                var alreadyMarked = indices
                    .Select(i => filesWithStatus[i - 1])
                    .Where(f => f.markType != 0)
                    .ToList();

                if (alreadyMarked.Count > 0)
                {
                    Console.WriteLine($"以下 {alreadyMarked.Count} 個檔案已被標記，請先取消標記：");
                    foreach (var file in alreadyMarked)
                    {
                        Console.WriteLine($"  - {file.path}");
                    }
                    Console.WriteLine();
                }

                if (filesToMark.Count == 0)
                {
                    Console.WriteLine("沒有可標記的檔案！");
                    Console.WriteLine();
                    continue;
                }

                Console.WriteLine($"將標記以下 {filesToMark.Count} 個檔案為待移動：");
                foreach (var file in filesToMark)
                {
                    var targetPath = CalculateTargetPath(file, _settings.DefaultMoveTargetBasePath);
                    Console.WriteLine($"  - 來源: {file}");
                    Console.WriteLine($"  - 目標: {targetPath}");
                    Console.WriteLine();
                }

                if (ConfirmAction("確認標記為移動？"))
                {
                    if (MarkFilesForMove(hashInput!, filesToMark))
                    {
                        Console.WriteLine("標記移動完成！");
                        Console.WriteLine();
                        continue; // 重新載入顯示
                    }
                }
                else
                {
                    Console.WriteLine("已取消標記");
                    Console.WriteLine();
                }
                continue;
            }

            // 處理取消標記指令 (c 1,2,3)
            if (choice?.StartsWith("c ") == true)
            {
                var indicesStr = choice.Substring(2).Trim();
                var indices = ParseIndices(indicesStr, filesWithStatus.Count);

                if (indices.Count == 0)
                {
                    Console.WriteLine("無效的編號，請重新輸入！");
                    Console.WriteLine();
                    continue;
                }

                // 只能取消已標記的檔案
                var filesToUnmark = indices
                    .Select(i => filesWithStatus[i - 1])
                    .Where(f => f.markType != 0)
                    .Select(f => f.path)
                    .ToList();

                var notMarked = indices
                    .Select(i => filesWithStatus[i - 1])
                    .Where(f => f.markType == 0)
                    .ToList();

                if (notMarked.Count > 0)
                {
                    Console.WriteLine($"以下 {notMarked.Count} 個檔案未被標記，無需取消：");
                    foreach (var file in notMarked)
                    {
                        Console.WriteLine($"  - {file.path}");
                    }
                    Console.WriteLine();
                }

                if (filesToUnmark.Count == 0)
                {
                    Console.WriteLine("沒有可取消標記的檔案！");
                    Console.WriteLine();
                    continue;
                }

                Console.WriteLine($"將取消以下 {filesToUnmark.Count} 個檔案的標記：");
                foreach (var file in filesToUnmark)
                {
                    Console.WriteLine($"  - {file}");
                }

                if (ConfirmAction("確認取消標記？"))
                {
                    UnmarkFiles(filesToUnmark);
                    Console.WriteLine("已取消標記！");
                    Console.WriteLine();
                    continue; // 重新載入顯示
                }
                else
                {
                    Console.WriteLine("已取消操作");
                    Console.WriteLine();
                }
                continue;
            }

            Console.WriteLine("無效的命令！請查看上方選單使用正確的命令格式。");
            Console.WriteLine();
        }
    }

    /// <summary>
    /// 處理單一命令
    /// </summary>
    /// <param name="isLastCommand">是否為最後一個命令（多命令場景下使用）</param>
    static CommandResult ProcessSingleCommand(
        string command,
        string hash,
        List<FileDetails> filesInGroup,
        List<string> validFilePaths,
        HashSet<string> handledFiles,
        bool isLastCommand = true)
    {
        command = command?.Trim().ToLower() ?? "";

        // 處理退出命令
        if (command == "q")
        {
            Console.WriteLine("已結束標記作業");
            return CommandResult.Quit;
        }

        // 處理預覽命令
        if (HandlePreviewCommand(command, validFilePaths))
        {
            return CommandResult.Continue;
        }

        // 處理跳過命令
        if (command == "k")
        {
            MarkHashAsSkipped(hash, validFilePaths);
            Console.WriteLine("已跳過此組並記錄，下次不會再顯示此組");
            Console.WriteLine();
            return CommandResult.NextGroup;
        }

        // 處理自動標記命令
        if (command == "a")
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
            Console.WriteLine($"最後修改日期: {oldestFile.LastModifiedTime}");
            Console.WriteLine();
            Console.WriteLine($"將標記以下 {autoDeletePaths.Count} 個檔案為待刪除：");
            foreach (var filePath in autoDeletePaths)
            {
                var fileToMark = filesInGroup.First(f => f.Path == filePath);
                Console.WriteLine($"  - {filePath}");
                Console.WriteLine($"    建立時間: {fileToMark.CreatedTime}");
                Console.WriteLine($"    最後修改日期: {fileToMark.LastModifiedTime}");
            }

            Console.WriteLine();
            if (ConfirmAction("確認標記？"))
            {
                if (MarkFilesForDeletion(hash, autoDeletePaths))
                {
                    Console.WriteLine("標記完成！");
                    handledFiles.UnionWith(autoDeletePaths);
                }
            }
            else
            {
                Console.WriteLine("已取消標記");
            }

            Console.WriteLine();
            return isLastCommand ? CommandResult.NextGroup : CommandResult.Continue;
        }

        // 處理移動標記命令（m 或 m 1,3,5）
        if (command.StartsWith("m"))
        {
            var indicesStr = command.Trim();
            List<int> moveIndices;

            // 處理 "m" (標記所有檔案)
            if (indicesStr == "m")
            {
                moveIndices = Enumerable.Range(1, filesInGroup.Count).ToList();
            }
            // 處理 "m 1,3,5" 格式
            else if (indicesStr.StartsWith("m "))
            {
                indicesStr = indicesStr.Substring(2).Trim();
                moveIndices = ParseIndices(indicesStr, filesInGroup.Count);
            }
            else
            {
                Console.WriteLine("無效的命令格式，請使用 'm 編號' 或 'm' 格式（例如: m 1,2,3 或 m）");
                Console.WriteLine();
                return CommandResult.Continue;
            }

            if (moveIndices.Count == 0)
            {
                Console.WriteLine("無效的編號，請重新輸入！");
                Console.WriteLine();
                return CommandResult.Continue;
            }

            var filesToMove = moveIndices.Select(i => filesInGroup[i - 1]).ToList();

            Console.WriteLine($"將標記以下 {filesToMove.Count} 個檔案為待移動：");
            foreach (var file in filesToMove)
            {
                var targetPath = CalculateTargetPath(file.Path, _settings.DefaultMoveTargetBasePath);
                Console.WriteLine($"  - 來源: {file.Path}");
                Console.WriteLine($"  - 目標: {targetPath}");
                Console.WriteLine();
            }

            if (ConfirmAction("確認標記為移動？"))
            {
                var moveFilePaths = filesToMove.Select(f => f.Path).ToList();
                if (MarkFilesForMove(hash, moveFilePaths))
                {
                    Console.WriteLine("標記移動完成！");
                    handledFiles.UnionWith(moveFilePaths);
                    Console.WriteLine();
                    return isLastCommand ? CommandResult.NextGroup : CommandResult.Continue;
                }
                else
                {
                    Console.WriteLine("標記移動失敗，請重試。");
                }
            }
            else
            {
                Console.WriteLine("已取消標記");
            }

            Console.WriteLine();
            return CommandResult.Continue;
        }

        // 處理刪除標記命令（d 或 d 1,3,5）
        if (command.StartsWith("d"))
        {
            var indicesStr = command.Trim();
            List<int> indices;

            // 處理 "d" (標記所有檔案)
            if (indicesStr == "d")
            {
                indices = Enumerable.Range(1, filesInGroup.Count).ToList();
            }
            // 處理 "d 1,3,5" 格式
            else if (indicesStr.StartsWith("d "))
            {
                indicesStr = indicesStr.Substring(2).Trim();
                indices = ParseIndices(indicesStr, filesInGroup.Count);
            }
            else
            {
                Console.WriteLine("無效的命令格式，請使用 'd 編號' 或 'd' 格式（例如: d 1,2,3 或 d）");
                Console.WriteLine();
                return CommandResult.Continue;
            }

            if (indices.Count == 0)
            {
                Console.WriteLine("無效的編號，請重新輸入！");
                Console.WriteLine();
                return CommandResult.Continue;
            }

            var filesToDelete = indices.Select(i => filesInGroup[i - 1].Path).ToList();

            var alreadyHandled = filesToDelete.Where(f => handledFiles.Contains(f)).ToList();
            var newMarks = filesToDelete.Where(f => !handledFiles.Contains(f)).ToList();

            if (alreadyHandled.Count > 0)
            {
                Console.WriteLine($"以下 {alreadyHandled.Count} 個檔案已經被標記：");
                foreach (var file in alreadyHandled)
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
                if (alreadyHandled.Count > 0)
                {
                    UnmarkFiles(alreadyHandled);
                    handledFiles.ExceptWith(alreadyHandled);
                }

                if (MarkFilesForDeletion(hash, filesToDelete))
                {
                    Console.WriteLine("標記完成！");
                    handledFiles.UnionWith(filesToDelete);
                }

                Console.WriteLine();
                return isLastCommand ? CommandResult.NextGroup : CommandResult.Continue;
            }
            else
            {
                Console.WriteLine("已取消標記");
                Console.WriteLine();
            }

            return CommandResult.Continue;
        }

        // 無效的命令
        Console.WriteLine("無效的命令！請查看上方選單使用正確的命令格式。");
        Console.WriteLine();
        return CommandResult.Continue;
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
        var markedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        DatabaseHelper.ExecuteTransaction((connection, transaction) =>
        {
            // 插入到 FilesToDelete 資料表
            var insertCommand = connection.CreateCommand();
            insertCommand.Transaction = transaction;
            insertCommand.CommandText = "INSERT OR IGNORE INTO FilesToDelete (Hash, FilePath, MarkedAt) VALUES ($hash, $filePath, $markedAt)";
            insertCommand.Parameters.Add(new SqliteParameter("$hash", (object)hash ?? DBNull.Value));
            insertCommand.Parameters.Add(new SqliteParameter("$filePath", filePath));
            insertCommand.Parameters.Add(new SqliteParameter("$markedAt", markedAt));
            insertCommand.ExecuteNonQuery();

            // 更新 DuplicateFiles 的 MarkType
            var updateCommand = connection.CreateCommand();
            updateCommand.Transaction = transaction;
            updateCommand.CommandText = "UPDATE DuplicateFiles SET MarkType = 1 WHERE FilePath = $filePath";
            updateCommand.Parameters.Add(new SqliteParameter("$filePath", filePath));
            updateCommand.ExecuteNonQuery();
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

                // 更新 FilesToDelete 的 IsProcessed 旗標
                var updateProcessedCommand = connection.CreateCommand();
                updateProcessedCommand.CommandText = @"
                    UPDATE FilesToDelete
                    SET IsProcessed = 1, ProcessedAt = $processedAt
                    WHERE FilePath = $path";
                var processedAtParam = updateProcessedCommand.CreateParameter();
                processedAtParam.ParameterName = "$processedAt";
                processedAtParam.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                updateProcessedCommand.Parameters.Add(processedAtParam);
                var pathParam = updateProcessedCommand.CreateParameter();
                pathParam.ParameterName = "$path";
                pathParam.Value = path;
                updateProcessedCommand.Parameters.Add(pathParam);
                updateProcessedCommand.ExecuteNonQuery();

                // 注意：不再清除 DuplicateFiles 的 MarkType，保持 MarkType = 1
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
                    // 從 FilesToDelete 移除
                    var deleteCommand = connection.CreateCommand();
                    deleteCommand.CommandText = "DELETE FROM FilesToDelete WHERE FilePath = $path";
                    var pathParam = deleteCommand.CreateParameter();
                    pathParam.ParameterName = "$path";
                    pathParam.Value = path;
                    deleteCommand.Parameters.Add(pathParam);
                    deleteCommand.ExecuteNonQuery();

                    // 清除 DuplicateFiles 的 MarkType
                    var updateCommand = connection.CreateCommand();
                    updateCommand.CommandText = "UPDATE DuplicateFiles SET MarkType = 0 WHERE FilePath = $path";
                    var updatePathParam = updateCommand.CreateParameter();
                    updatePathParam.ParameterName = "$path";
                    updatePathParam.Value = path;
                    updateCommand.Parameters.Add(updatePathParam);
                    updateCommand.ExecuteNonQuery();
                }

                Console.WriteLine("已清除記錄！");
            }
        }
    }

    /// <summary>
    /// 計算檔案的歸檔權重
    /// </summary>
    /// <param name="filePath">檔案路徑</param>
    /// <returns>權重分數 (基礎分 + 加分項 - 減分項)</returns>
    /// <remarks>
    /// 基礎權重：
    /// - 80分：所有檔案的起點權重
    ///
    /// 加分項（適用於所有檔案）：
    /// - 完整目錄路徑長度每 5 個字元 +1 分（不含檔名）
    /// - 描述性長路徑會獲得更高權重
    /// - 範例："C:\Users\clove\OneDrive\安茹蕾烘焙工作-配方本\安如蕾烘焙工作" (42字元) = +8 分
    /// - 範例："C:\Users\clove\OneDrive\圖片\2022-11" (34字元) = +6 分
    ///
    /// 減分項：
    /// - 檔名包含 "(1)"：-10 分
    /// - 檔名包含 "-DESKTOP-0M8E5B6"：-10 分
    /// </remarks>
    static int CalculateFileWeight(string filePath)
    {
        int baseWeight = 80; // 所有檔案統一起點
        int bonusScore = 0;
        int penaltyScore = 0;

        // === 加分項：完整目錄路徑長度 ===
        // 取得完整目錄路徑（不含檔名）
        string? directoryPath = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directoryPath))
        {
            // 每 5 個字元給 1 分（描述性長路徑如 "C:\Users\clove\OneDrive\安茹蕾烘焙工作-配方本\安如蕾烘焙工作" 比短路徑更有價值）
            bonusScore = directoryPath.Length / 5;
        }

        // === 減分項：檔名判斷 ===
        string fileName = Path.GetFileName(filePath);

        // 檔名包含 "(1)" 扣 10 分
        if (fileName.Contains("(1)", StringComparison.OrdinalIgnoreCase))
        {
            penaltyScore += 10;
        }

        // 檔名包含 "-DESKTOP-0M8E5B6" 扣 10 分
        if (fileName.Contains("-DESKTOP-0M8E5B6", StringComparison.OrdinalIgnoreCase))
        {
            penaltyScore += 10;
        }

        return baseWeight + bonusScore - penaltyScore;
    }

    /// <summary>
    /// 自動歸檔重複檔案（依權重自動標記）
    /// </summary>
    static void AutoArchiveDuplicates()
    {
        Console.WriteLine("=== 自動歸檔重複檔案 ===");
        Console.WriteLine();

        InitializeDatabase();
        var duplicateFileGroups = LoadDuplicateGroupsWithDetails();

        if (duplicateFileGroups.Count == 0)
        {
            Console.WriteLine("資料庫中沒有重複檔案記錄，請先執行掃描！");
            return;
        }

        var duplicateGroups = duplicateFileGroups.Where(g => g.Value.Count > 1).ToList();

        if (duplicateGroups.Count == 0)
        {
            Console.WriteLine("沒有找到重複檔案！");
            return;
        }

        Console.WriteLine($"找到 {duplicateGroups.Count} 組重複檔案");
        Console.WriteLine("開始分析權重並自動標記...");
        Console.WriteLine();

        int totalGroups = duplicateGroups.Count;
        int processedGroups = 0;
        int keptFiles = 0;
        int markedForDeletion = 0;
        int lastReportedPercentage = 0;

        foreach (var group in duplicateGroups)
        {
            var hash = group.Key;
            var files = group.Value;

            // 計算每個檔案的權重和修改時間
            var fileWeights = files.Select(f => new
            {
                File = f,
                Weight = CalculateFileWeight(f.Path),
                ModifiedTime = DateTime.TryParse(f.LastModifiedTime, out var dt) ? dt : DateTime.MaxValue
            }).ToList();

            // 找出權重最高的檔案
            var maxWeight = fileWeights.Max(fw => fw.Weight);
            var candidatesWithMaxWeight = fileWeights.Where(fw => fw.Weight == maxWeight).ToList();

            // 如果有多個相同權重，保留修改日期最舊的
            // 如果修改日期也相同，保留路徑最長的（通常表示資料夾名稱更詳細）
            var fileToKeep = candidatesWithMaxWeight
                .OrderBy(fw => fw.ModifiedTime)
                .ThenByDescending(fw => fw.File.Path.Length)
                .First();

            keptFiles++;

            // 確保至少保留一個檔案
            if (files.Count == 1)
            {
                processedGroups++;
                continue;
            }

            // 處理其餘檔案
            var filesToProcess = fileWeights.Where(fw => fw.File.Path != fileToKeep.File.Path).ToList();

            foreach (var fw in filesToProcess)
            {
                // 所有不保留的檔案都標記為刪除
                try
                {
                    MarkFileForDeletion(hash, fw.File.Path);
                    markedForDeletion++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine();
                    Console.WriteLine($"[失敗] 標記刪除失敗: {fw.File.Path}, 錯誤: {ex.Message}");
                }
            }

            processedGroups++;

            // 單行進度顯示：每 50 組或每 5% 更新一次（使用 \r 在同一行更新）
            var percentage = (double)processedGroups / totalGroups * 100;
            var currentPercentage = (int)(percentage / 5) * 5; // 每 5% 為一個區間

            // 每 50 組或每達到新的 5% 區間時更新
            if (processedGroups % 50 == 0 || (currentPercentage > lastReportedPercentage && currentPercentage % 5 == 0))
            {
                Console.Write($"\r[{currentPercentage}%] 已處理 {processedGroups}/{totalGroups} 組 | 保留: {keptFiles} | 標記刪除: {markedForDeletion}  ");
                lastReportedPercentage = currentPercentage;
            }
        }

        // 完成處理後換行
        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine("=== 自動歸檔完成 ===");
        Console.WriteLine($"處理組數: {processedGroups}");
        Console.WriteLine($"保留檔案: {keptFiles}");
        Console.WriteLine($"標記刪除: {markedForDeletion}");
        Console.WriteLine();
        Console.WriteLine("您可以使用選項 3 查看標記結果，並使用選項 6 執行刪除。");
    }

    static void ExecuteMarkedMoves()
    {
        InitializeDatabase();

        using var connection = new SqliteConnection($"Data Source={DatabaseFileName}");
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT SourcePath, TargetPath, MarkedAt FROM FileToMove ORDER BY MarkedAt";

        var markedFiles = new List<(string sourcePath, string targetPath, string markedAt)>();

        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                markedFiles.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2)));
            }
        }

        if (markedFiles.Count == 0)
        {
            Console.WriteLine("沒有標記為待移動的檔案！");
            return;
        }

        Console.WriteLine($"找到 {markedFiles.Count} 個標記為待移動的檔案：");
        Console.WriteLine();

        var existingFiles = markedFiles.Where(f => File.Exists(f.sourcePath)).ToList();
        var missingFiles = markedFiles.Where(f => !File.Exists(f.sourcePath)).ToList();

        if (existingFiles.Count > 0)
        {
            Console.WriteLine("存在的檔案：");
            foreach (var (sourcePath, targetPath, markedAt) in existingFiles)
            {
                var fileInfo = new FileInfo(sourcePath);
                Console.WriteLine($"  - 來源: {sourcePath}");
                Console.WriteLine($"    目標: {targetPath}");
                Console.WriteLine($"    大小: {FormatFileSize(fileInfo.Length)}，標記時間: {markedAt}");
                Console.WriteLine();
            }
        }

        if (missingFiles.Count > 0)
        {
            Console.WriteLine($"已不存在的檔案 ({missingFiles.Count} 個)：");
            foreach (var (sourcePath, targetPath, _) in missingFiles)
            {
                Console.WriteLine($"  - 來源: {sourcePath}");
                Console.WriteLine($"    目標: {targetPath}");
            }

            Console.WriteLine();
        }

        if (existingFiles.Count == 0)
        {
            Console.WriteLine("所有標記的檔案都已不存在！");
            if (ConfirmAction("是否要清除這些記錄？"))
            {
                ClearMoveMarks();
            }

            return;
        }

        if (!ConfirmAction($"確認要移動這 {existingFiles.Count} 個檔案嗎？"))
        {
            Console.WriteLine("已取消移動操作");
            return;
        }

        Console.WriteLine();
        Console.WriteLine("開始移動檔案...");

        int successCount = 0;
        int failCount = 0;

        foreach (var (sourcePath, targetPath, _) in existingFiles)
        {
            try
            {
                // 確保目標目錄存在
                var targetDir = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                // 檢查目標檔案是否已存在
                if (File.Exists(targetPath))
                {
                    Console.WriteLine($"⚠ 目標檔案已存在，跳過: {targetPath}");
                    failCount++;
                    continue;
                }

                // 移動檔案
                File.Move(sourcePath, targetPath);
                Console.WriteLine($"✓ 已移動: {Path.GetFileName(sourcePath)} → {targetPath}");
                successCount++;

                // 更新處理狀態
                var updateCommand = connection.CreateCommand();
                updateCommand.CommandText = "UPDATE FileToMove SET IsProcessed = 1, ProcessedAt = $processedAt WHERE SourcePath = $path";
                var processedAtParam = updateCommand.CreateParameter();
                processedAtParam.ParameterName = "$processedAt";
                processedAtParam.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                updateCommand.Parameters.Add(processedAtParam);
                var pathParam = updateCommand.CreateParameter();
                pathParam.ParameterName = "$path";
                pathParam.Value = sourcePath;
                updateCommand.Parameters.Add(pathParam);
                updateCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ 移動失敗 [{sourcePath}]: {ex.Message}");
                failCount++;
            }
        }

        Console.WriteLine();
        Console.WriteLine($"移動完成！成功: {successCount}，失敗: {failCount}");

        if (missingFiles.Count > 0)
        {
            if (ConfirmAction($"是否要清除 {missingFiles.Count} 個已不存在的檔案記錄？"))
            {
                foreach (var (sourcePath, _, _) in missingFiles)
                {
                    // 更新 FileToMove 處理狀態
                    var updateCommand = connection.CreateCommand();
                    updateCommand.CommandText = "UPDATE FileToMove SET IsProcessed = 1, ProcessedAt = $processedAt WHERE SourcePath = $path";
                    var processedAtParam = updateCommand.CreateParameter();
                    processedAtParam.ParameterName = "$processedAt";
                    processedAtParam.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    updateCommand.Parameters.Add(processedAtParam);
                    var pathParam = updateCommand.CreateParameter();
                    pathParam.ParameterName = "$path";
                    pathParam.Value = sourcePath;
                    updateCommand.Parameters.Add(pathParam);
                    updateCommand.ExecuteNonQuery();
                }

                Console.WriteLine("已標記為已處理！");
            }
        }
    }

    static void ClearMoveMarks()
    {
        DatabaseHelper.ExecuteTransaction((connection, transaction) =>
        {
            // 清除所有 FileToMove 記錄
            var deleteCommand = connection.CreateCommand();
            deleteCommand.Transaction = transaction;
            deleteCommand.CommandText = "DELETE FROM FileToMove";
            var count = deleteCommand.ExecuteNonQuery();

            // 清除所有 MarkType = 2 的標記
            var updateCommand = connection.CreateCommand();
            updateCommand.Transaction = transaction;
            updateCommand.CommandText = "UPDATE DuplicateFiles SET MarkType = 0 WHERE MarkType = 2";
            updateCommand.ExecuteNonQuery();

            Console.WriteLine($"已清除 {count} 筆移動標記記錄");
        });
    }

    static void ClearDeletedMarks()
    {
        DatabaseHelper.ExecuteTransaction((connection, transaction) =>
        {
            // 清除所有 FilesToDelete 記錄
            var deleteCommand = connection.CreateCommand();
            deleteCommand.Transaction = transaction;
            deleteCommand.CommandText = "DELETE FROM FilesToDelete";
            var count = deleteCommand.ExecuteNonQuery();

            // 清除所有 MarkType = 1 的標記
            var updateCommand = connection.CreateCommand();
            updateCommand.Transaction = transaction;
            updateCommand.CommandText = "UPDATE DuplicateFiles SET MarkType = 0 WHERE MarkType = 1";
            updateCommand.ExecuteNonQuery();

            Console.WriteLine($"已清除 {count} 筆記錄");
        });
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
                "輸入 'c' 或 'c 編號' 取消略過該群組（例如: c 1,2 或 c 取消所有）",
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

            // 處理取消略過指令
            if (choice?.StartsWith("c") == true)
            {
                // 如果只輸入 'c'，取消所有略過標記
                if (choice == "c")
                {
                    if (ConfirmAction($"確認要取消所有 {groupList.Count} 組的略過標記嗎？"))
                    {
                        var allHashes = groupList.Select(g => g.hash).ToList();
                        UnskipHashes(allHashes);
                        Console.WriteLine("已取消所有略過標記！");
                        return;
                    }
                    else
                    {
                        Console.WriteLine("已取消操作");
                        Console.WriteLine();
                        continue;
                    }
                }

                // 處理 'c 1,2,3' 格式
                var indicesStr = choice.Substring(1).Trim();
                var indices = ParseIndices(indicesStr, groupList.Count);

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
                continue;
            }

            Console.WriteLine("無效的選擇，請重新輸入！");
            Console.WriteLine();
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

        // 為所有檔案添加連續編號（包括存在和不存在的檔案）
        var allFilesWithIndex = markedFiles.Select((f, index) => (index: index + 1, file: f, exists: File.Exists(f.path))).ToList();
        var existingFiles = allFilesWithIndex.Where(f => f.exists).ToList();
        var missingFiles = allFilesWithIndex.Where(f => !f.exists).ToList();

        if (existingFiles.Count > 0)
        {
            Console.WriteLine($"存在的檔案 ({existingFiles.Count} 個)：");
            foreach (var (index, file, _) in existingFiles)
            {
                var (path, markedAt) = file;
                var fileInfo = new FileInfo(path);
                Console.WriteLine($"[{index}] {path}");
                Console.WriteLine($"    大小: {FormatFileSize(fileInfo.Length)}，標記時間: {markedAt}");
                Console.WriteLine();
            }
        }

        if (missingFiles.Count > 0)
        {
            Console.WriteLine($"檔案不存在 ({missingFiles.Count} 個)：");
            foreach (var (index, file, _) in missingFiles)
            {
                var (path, markedAt) = file;
                Console.WriteLine($"[{index}] {path} (標記時間: {markedAt})");
            }

            Console.WriteLine();
        }

        // 操作選單
        while (true)
        {
            DisplayMenu(
                "輸入 'c' 或 'c 編號' 取消標記（例如: c 1,2 或 c 取消所有）",
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

                var existingPaths = existingFiles.Select(f => f.file.path).ToList();
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

            // 處理取消標記指令
            if (choice?.StartsWith("c") == true)
            {
                // 如果只輸入 'c'，取消所有標記
                if (choice == "c")
                {
                    if (ConfirmAction($"確認要取消所有 {markedFiles.Count} 個檔案的標記嗎？"))
                    {
                        var allPaths = markedFiles.Select(f => f.path).ToList();
                        UnmarkFiles(allPaths);
                        Console.WriteLine("已取消所有標記！");
                        return;
                    }
                    else
                    {
                        Console.WriteLine("已取消操作");
                        Console.WriteLine();
                        continue;
                    }
                }

                // 處理 'c 1,2,3' 格式
                var indicesStr = choice.Substring(1).Trim();
                var indices = ParseIndices(indicesStr, allFilesWithIndex.Count);

                if (indices.Count == 0)
                {
                    Console.WriteLine("無效的選擇，請重新輸入！");
                    Console.WriteLine();
                    continue;
                }

                var filesToUnmark = indices.Select(i => allFilesWithIndex[i - 1].file.path).ToList();

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
                continue;
            }

            Console.WriteLine("無效的選擇，請重新輸入！");
            Console.WriteLine();
        }
    }

    static void ViewMarkedForMoveFiles()
    {
        InitializeDatabase();

        var markedFiles = LoadMarkedForMoveFilesWithDetails();

        if (markedFiles.Count == 0)
        {
            Console.WriteLine("目前沒有已標記移動的檔案！");
            return;
        }

        Console.WriteLine($"=== 已標記移動檔案清單 (共 {markedFiles.Count} 個) ===");
        Console.WriteLine();

        // 為所有檔案添加連續編號（包括存在和不存在的檔案）
        var allFilesWithIndex = markedFiles.Select((f, index) => (index: index + 1, file: f, exists: File.Exists(f.sourcePath))).ToList();
        var existingFiles = allFilesWithIndex.Where(f => f.exists).ToList();
        var missingFiles = allFilesWithIndex.Where(f => !f.exists).ToList();

        if (existingFiles.Count > 0)
        {
            Console.WriteLine($"存在的檔案 ({existingFiles.Count} 個)：");
            foreach (var (index, file, _) in existingFiles)
            {
                var (sourcePath, targetPath, markedAt) = file;
                var fileInfo = new FileInfo(sourcePath);
                Console.WriteLine($"[{index}] 來源: {sourcePath}");
                Console.WriteLine($"    目標: {targetPath}");
                Console.WriteLine($"    大小: {FormatFileSize(fileInfo.Length)}，標記時間: {markedAt}");
                Console.WriteLine();
            }
        }

        if (missingFiles.Count > 0)
        {
            Console.WriteLine($"檔案不存在 ({missingFiles.Count} 個)：");
            foreach (var (index, file, _) in missingFiles)
            {
                var (sourcePath, targetPath, markedAt) = file;
                Console.WriteLine($"[{index}] 來源: {sourcePath}");
                Console.WriteLine($"    目標: {targetPath} (標記時間: {markedAt})");
            }

            Console.WriteLine();
        }

        // 操作選單
        while (true)
        {
            DisplayMenu(
                "輸入 'c' 或 'c 編號' 取消標記（例如: c 1,2 或 c 取消所有）",
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

                var existingPaths = existingFiles.Select(f => f.file.sourcePath).ToList();
                if (HandlePreviewCommand(choice, existingPaths))
                {
                    continue;
                }
            }

            if (choice == "a")
            {
                if (ConfirmAction($"確認要清除所有 {markedFiles.Count} 個移動標記嗎？"))
                {
                    var allSourcePaths = markedFiles.Select(f => f.sourcePath).ToList();
                    UnmarkMoveFiles(allSourcePaths);
                    Console.WriteLine("已清除所有移動標記！");
                    return;
                }
                else
                {
                    Console.WriteLine("已取消操作");
                    Console.WriteLine();
                    continue;
                }
            }

            // 處理取消標記指令
            if (choice?.StartsWith("c") == true)
            {
                // 如果只輸入 'c'，取消所有標記
                if (choice == "c")
                {
                    if (ConfirmAction($"確認要取消所有 {markedFiles.Count} 個檔案的移動標記嗎？"))
                    {
                        var allSourcePaths = markedFiles.Select(f => f.sourcePath).ToList();
                        UnmarkMoveFiles(allSourcePaths);
                        Console.WriteLine("已取消所有移動標記！");
                        return;
                    }
                    else
                    {
                        Console.WriteLine("已取消操作");
                        Console.WriteLine();
                        continue;
                    }
                }

                // 處理 'c 1,2,3' 格式
                var indicesStr = choice.Substring(1).Trim();
                var indices = ParseIndices(indicesStr, allFilesWithIndex.Count);

                if (indices.Count == 0)
                {
                    Console.WriteLine("無效的選擇，請重新輸入！");
                    Console.WriteLine();
                    continue;
                }

                var filesToUnmark = indices.Select(i => allFilesWithIndex[i - 1].file.sourcePath).ToList();

                Console.WriteLine($"確認要取消以下 {filesToUnmark.Count} 個檔案的移動標記：");
                foreach (var file in filesToUnmark)
                {
                    Console.WriteLine($"  - {file}");
                }

                if (ConfirmAction("確認取消移動標記？"))
                {
                    UnmarkMoveFiles(filesToUnmark);
                    Console.WriteLine("已取消移動標記！");
                    return;
                }
                else
                {
                    Console.WriteLine("已取消操作");
                    Console.WriteLine();
                }
                continue;
            }

            Console.WriteLine("無效的選擇，請重新輸入！");
            Console.WriteLine();
        }
    }

    static List<(string path, string markedAt)> LoadMarkedForDeletionFilesWithDetails()
    {
        try
        {
            return DatabaseHelper.ExecuteQuery(
                "SELECT FilePath, MarkedAt FROM FilesToDelete WHERE IsProcessed = 0 ORDER BY MarkedAt",
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

    static List<(string sourcePath, string targetPath, string markedAt)> LoadMarkedForMoveFilesWithDetails()
    {
        try
        {
            return DatabaseHelper.ExecuteQuery(
                "SELECT SourcePath, TargetPath, MarkedAt FROM FileToMove WHERE IsProcessed = 0 ORDER BY MarkedAt",
                reader =>
                {
                    var files = new List<(string, string, string)>();
                    while (reader.Read())
                    {
                        files.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2)));
                    }
                    return files;
                });
        }
        catch
        {
            // 資料表可能不存在，忽略錯誤
            return new List<(string, string, string)>();
        }
    }

    static void UnmarkFiles(List<string> filePaths)
    {
        DatabaseHelper.ExecuteTransaction((connection, transaction) =>
        {
            // 從 FilesToDelete 移除
            var deleteCommand = connection.CreateCommand();
            deleteCommand.Transaction = transaction;
            deleteCommand.CommandText = "DELETE FROM FilesToDelete WHERE FilePath = $path";
            var deletePathParam = new SqliteParameter("$path", "");
            deleteCommand.Parameters.Add(deletePathParam);

            // 清除 DuplicateFiles 的 MarkType
            var updateCommand = connection.CreateCommand();
            updateCommand.Transaction = transaction;
            updateCommand.CommandText = "UPDATE DuplicateFiles SET MarkType = 0 WHERE FilePath = $path AND MarkType = 1";
            var updatePathParam = new SqliteParameter("$path", "");
            updateCommand.Parameters.Add(updatePathParam);

            foreach (var path in filePaths)
            {
                deletePathParam.Value = path;
                deleteCommand.ExecuteNonQuery();

                updatePathParam.Value = path;
                updateCommand.ExecuteNonQuery();
            }
        });
    }

    static void UnmarkMoveFiles(List<string> sourcePaths)
    {
        DatabaseHelper.ExecuteTransaction((connection, transaction) =>
        {
            // 從 FileToMove 移除
            var deleteCommand = connection.CreateCommand();
            deleteCommand.Transaction = transaction;
            deleteCommand.CommandText = "DELETE FROM FileToMove WHERE SourcePath = $path";
            var deletePathParam = new SqliteParameter("$path", "");
            deleteCommand.Parameters.Add(deletePathParam);

            // 清除 DuplicateFiles 的 MarkType
            var updateCommand = connection.CreateCommand();
            updateCommand.Transaction = transaction;
            updateCommand.CommandText = "UPDATE DuplicateFiles SET MarkType = 0 WHERE FilePath = $path AND MarkType = 2";
            var updatePathParam = new SqliteParameter("$path", "");
            updateCommand.Parameters.Add(updatePathParam);

            foreach (var path in sourcePaths)
            {
                deletePathParam.Value = path;
                deleteCommand.ExecuteNonQuery();

                updatePathParam.Value = path;
                updateCommand.ExecuteNonQuery();
            }
        });
    }

    static void ClearAllMarks()
    {
        DatabaseHelper.ExecuteNonQuery("DELETE FROM FilesToDelete");
    }

    static HashSet<string> LoadHandledFiles()
    {
        try
        {
            return DatabaseHelper.ExecuteQuery(
                "SELECT FilePath FROM DuplicateFiles WHERE MarkType > 0",
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

    static Dictionary<string, HashSet<string>> LoadHandledFilesByHash()
    {
        try
        {
            return DatabaseHelper.ExecuteQuery(
                "SELECT Hash, FilePath FROM DuplicateFiles WHERE MarkType > 0 AND Hash IS NOT NULL",
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

            // 更新 DuplicateFiles 的 MarkType 為 3 (跳過標記)
            var updateCommand = connection.CreateCommand();
            updateCommand.Transaction = transaction;
            updateCommand.CommandText = "UPDATE DuplicateFiles SET MarkType = 3 WHERE FilePath = $filePath";
            var updateFilePathParam = new SqliteParameter("$filePath", "");
            updateCommand.Parameters.Add(updateFilePathParam);

            foreach (var filePath in filePaths)
            {
                updateFilePathParam.Value = filePath;
                updateCommand.ExecuteNonQuery();
            }
        });
    }

    static Dictionary<string, List<(string path, string skippedAt)>> LoadSkippedFilesGroupedByHash()
    {
        return LoadFilesGroupedByHash("SkippedHashes", "SkippedAt");
    }

    /// <summary>
    /// 重複檔案群組統計資訊
    /// </summary>
    record DuplicateGroupInfo(
        string Hash,
        int DuplicateCount,
        long TotalSize,
        long MaxFileSize
    );

    /// <summary>
    /// 綜合報表的檔案資料結構
    /// </summary>
    record ComprehensiveFileInfo(
        string FilePath,
        string Hash,
        long FileSize,
        string FileCreatedTime,
        string FileLastModifiedTime,
        bool Exists,
        int MarkType,
        int DuplicateCount = 1,
        string? MarkedAt = null,
        int? IsProcessed = null,
        string? ProcessedAt = null,
        string? TargetPath = null
    );

    /// <summary>
    /// 載入未標記的檔案 (MarkType = 0)
    /// </summary>
    static List<ComprehensiveFileInfo> LoadUnmarkedFiles()
    {
        try
        {
            return DatabaseHelper.ExecuteQuery(
                @"SELECT df.FilePath, df.Hash, df.FileSize, df.FileCreatedTime, df.FileLastModifiedTime,
                         (SELECT COUNT(*) FROM DuplicateFiles WHERE Hash = df.Hash) as DuplicateCount
                  FROM DuplicateFiles df
                  WHERE df.MarkType = 0
                  ORDER BY df.FileSize DESC",
                reader =>
                {
                    var files = new List<ComprehensiveFileInfo>();
                    while (reader.Read())
                    {
                        var filePath = reader.GetString(0);
                        files.Add(new ComprehensiveFileInfo(
                            FilePath: filePath,
                            Hash: reader.GetString(1),
                            FileSize: reader.GetInt64(2),
                            FileCreatedTime: reader.GetString(3),
                            FileLastModifiedTime: reader.GetString(4),
                            Exists: File.Exists(filePath),
                            MarkType: 0,
                            DuplicateCount: reader.GetInt32(5)
                        ));
                    }
                    return files;
                }
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"載入未標記檔案時發生錯誤: {ex.Message}");
            return new List<ComprehensiveFileInfo>();
        }
    }

    /// <summary>
    /// 載入已標記刪除的檔案 (MarkType = 1) 及執行狀態
    /// </summary>
    static List<ComprehensiveFileInfo> LoadMarkedForDeletionFiles()
    {
        try
        {
            return DatabaseHelper.ExecuteQuery(
                @"SELECT df.FilePath, df.Hash, df.FileSize, df.FileCreatedTime, df.FileLastModifiedTime,
                         ftd.MarkedAt, ftd.IsProcessed, ftd.ProcessedAt,
                         (SELECT COUNT(*) FROM DuplicateFiles WHERE Hash = df.Hash) as DuplicateCount
                  FROM DuplicateFiles df
                  LEFT JOIN FilesToDelete ftd ON df.FilePath = ftd.FilePath
                  WHERE df.MarkType = 1
                  ORDER BY df.FileSize DESC",
                reader =>
                {
                    var files = new List<ComprehensiveFileInfo>();
                    while (reader.Read())
                    {
                        var filePath = reader.GetString(0);
                        files.Add(new ComprehensiveFileInfo(
                            FilePath: filePath,
                            Hash: reader.GetString(1),
                            FileSize: reader.GetInt64(2),
                            FileCreatedTime: reader.GetString(3),
                            FileLastModifiedTime: reader.GetString(4),
                            Exists: File.Exists(filePath),
                            MarkType: 1,
                            DuplicateCount: reader.GetInt32(8),
                            MarkedAt: reader.IsDBNull(5) ? null : reader.GetString(5),
                            IsProcessed: reader.IsDBNull(6) ? null : reader.GetInt32(6),
                            ProcessedAt: reader.IsDBNull(7) ? null : reader.GetString(7)
                        ));
                    }
                    return files;
                }
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"載入已標記刪除檔案時發生錯誤: {ex.Message}");
            return new List<ComprehensiveFileInfo>();
        }
    }

    /// <summary>
    /// 載入已標記移動的檔案 (MarkType = 2) 及執行狀態
    /// </summary>
    static List<ComprehensiveFileInfo> LoadMarkedForMoveFiles()
    {
        try
        {
            return DatabaseHelper.ExecuteQuery(
                @"SELECT df.FilePath, df.Hash, df.FileSize, df.FileCreatedTime, df.FileLastModifiedTime,
                         ftm.MarkedAt, ftm.IsProcessed, ftm.ProcessedAt, ftm.TargetPath,
                         (SELECT COUNT(*) FROM DuplicateFiles WHERE Hash = df.Hash) as DuplicateCount
                  FROM DuplicateFiles df
                  LEFT JOIN FileToMove ftm ON df.FilePath = ftm.SourcePath
                  WHERE df.MarkType = 2
                  ORDER BY df.FileSize DESC",
                reader =>
                {
                    var files = new List<ComprehensiveFileInfo>();
                    while (reader.Read())
                    {
                        var filePath = reader.GetString(0);
                        files.Add(new ComprehensiveFileInfo(
                            FilePath: filePath,
                            Hash: reader.GetString(1),
                            FileSize: reader.GetInt64(2),
                            FileCreatedTime: reader.GetString(3),
                            FileLastModifiedTime: reader.GetString(4),
                            Exists: File.Exists(filePath),
                            MarkType: 2,
                            DuplicateCount: reader.GetInt32(9),
                            MarkedAt: reader.IsDBNull(5) ? null : reader.GetString(5),
                            IsProcessed: reader.IsDBNull(6) ? null : reader.GetInt32(6),
                            ProcessedAt: reader.IsDBNull(7) ? null : reader.GetString(7),
                            TargetPath: reader.IsDBNull(8) ? null : reader.GetString(8)
                        ));
                    }
                    return files;
                }
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"載入已標記移動檔案時發生錯誤: {ex.Message}");
            return new List<ComprehensiveFileInfo>();
        }
    }

    /// <summary>
    /// 載入已標記略過的檔案 (MarkType = 3)
    /// </summary>
    static List<ComprehensiveFileInfo> LoadSkippedFiles()
    {
        try
        {
            return DatabaseHelper.ExecuteQuery(
                @"SELECT df.FilePath, df.Hash, df.FileSize, df.FileCreatedTime, df.FileLastModifiedTime,
                         sh.SkippedAt,
                         (SELECT COUNT(*) FROM DuplicateFiles WHERE Hash = df.Hash) as DuplicateCount
                  FROM DuplicateFiles df
                  LEFT JOIN SkippedHashes sh ON df.FilePath = sh.FilePath AND df.Hash = sh.Hash
                  WHERE df.MarkType = 3
                  ORDER BY df.FileSize DESC",
                reader =>
                {
                    var files = new List<ComprehensiveFileInfo>();
                    while (reader.Read())
                    {
                        var filePath = reader.GetString(0);
                        files.Add(new ComprehensiveFileInfo(
                            FilePath: filePath,
                            Hash: reader.GetString(1),
                            FileSize: reader.GetInt64(2),
                            FileCreatedTime: reader.GetString(3),
                            FileLastModifiedTime: reader.GetString(4),
                            Exists: File.Exists(filePath),
                            MarkType: 3,
                            DuplicateCount: reader.GetInt32(6),
                            MarkedAt: reader.IsDBNull(5) ? null : reader.GetString(5)
                        ));
                    }
                    return files;
                }
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"載入已標記略過檔案時發生錯誤: {ex.Message}");
            return new List<ComprehensiveFileInfo>();
        }
    }

    /// <summary>
    /// 載入重複檔案群組統計資訊（按最大檔案大小降序排列）
    /// </summary>
    static Dictionary<string, DuplicateGroupInfo> LoadDuplicateGroupStatistics()
    {
        try
        {
            return DatabaseHelper.ExecuteQuery(
                @"SELECT Hash, COUNT(*) as DuplicateCount, SUM(FileSize) as TotalSize, MAX(FileSize) as MaxFileSize
                  FROM DuplicateFiles
                  GROUP BY Hash
                  ORDER BY MaxFileSize DESC",
                reader =>
                {
                    var groups = new Dictionary<string, DuplicateGroupInfo>();
                    while (reader.Read())
                    {
                        var hash = reader.GetString(0);
                        var count = reader.GetInt32(1);
                        var totalSize = reader.GetInt64(2);
                        var maxFileSize = reader.GetInt64(3);

                        groups[hash] = new DuplicateGroupInfo(
                            Hash: hash,
                            DuplicateCount: count,
                            TotalSize: totalSize,
                            MaxFileSize: maxFileSize
                        );
                    }
                    return groups;
                }
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"載入重複檔案群組統計時發生錯誤: {ex.Message}");
            return new Dictionary<string, DuplicateGroupInfo>();
        }
    }

    static void UnskipHashes(List<string> hashes)
    {
        DatabaseHelper.ExecuteTransaction((connection, transaction) =>
        {
            // 從 SkippedHashes 移除
            var deleteCommand = connection.CreateCommand();
            deleteCommand.Transaction = transaction;
            deleteCommand.CommandText = "DELETE FROM SkippedHashes WHERE Hash = $hash";
            var deleteHashParam = new SqliteParameter("$hash", "");
            deleteCommand.Parameters.Add(deleteHashParam);

            // 清除 DuplicateFiles 的 MarkType
            var updateCommand = connection.CreateCommand();
            updateCommand.Transaction = transaction;
            updateCommand.CommandText = "UPDATE DuplicateFiles SET MarkType = 0 WHERE Hash = $hash AND MarkType = 3";
            var updateHashParam = new SqliteParameter("$hash", "");
            updateCommand.Parameters.Add(updateHashParam);

            foreach (var hash in hashes)
            {
                deleteHashParam.Value = hash;
                deleteCommand.ExecuteNonQuery();

                updateHashParam.Value = hash;
                updateCommand.ExecuteNonQuery();
            }
        });
    }

    /// <summary>
    /// 計算目標檔案路徑（根據檔案修改日期 yyyy-MM）
    /// </summary>
    static string CalculateTargetPath(string sourceFilePath, string baseTargetPath)
    {
        try
        {
            var fileInfo = new FileInfo(sourceFilePath);
            if (!fileInfo.Exists)
            {
                // 如果檔案不存在，使用當前時間
                var currentFolder = DateTime.Now.ToString("yyyy-MM");
                var fileName = Path.GetFileName(sourceFilePath);
                return Path.Combine(baseTargetPath, currentFolder, fileName);
            }

            // 使用檔案的修改時間
            var lastModified = fileInfo.LastWriteTime;
            var folderName = lastModified.ToString("yyyy-MM");
            var targetFolder = Path.Combine(baseTargetPath, folderName);
            var targetPath = Path.Combine(targetFolder, fileInfo.Name);

            return targetPath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"計算目標路徑時發生錯誤: {ex.Message}");
            // 發生錯誤時，使用當前時間
            var currentFolder = DateTime.Now.ToString("yyyy-MM");
            var fileName = Path.GetFileName(sourceFilePath);
            return Path.Combine(baseTargetPath, currentFolder, fileName);
        }
    }

    /// <summary>
    /// 標記檔案為待移動
    /// </summary>
    static bool MarkFilesForMove(string hash, List<string> files)
    {
        var success = true;
        var markedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        DatabaseHelper.ExecuteTransaction((connection, transaction) =>
        {
            var insertCommand = connection.CreateCommand();
            insertCommand.Transaction = transaction;
            insertCommand.CommandText = @"
                INSERT OR REPLACE INTO FileToMove (Hash, SourcePath, TargetPath, MarkedAt)
                VALUES ($hash, $sourcePath, $targetPath, $markedAt)";

            var hashParam = new SqliteParameter("$hash", hash);
            var sourcePathParam = new SqliteParameter("$sourcePath", "");
            var targetPathParam = new SqliteParameter("$targetPath", "");
            var markedAtParam = new SqliteParameter("$markedAt", markedAt);

            insertCommand.Parameters.Add(hashParam);
            insertCommand.Parameters.Add(sourcePathParam);
            insertCommand.Parameters.Add(targetPathParam);
            insertCommand.Parameters.Add(markedAtParam);

            var updateCommand = connection.CreateCommand();
            updateCommand.Transaction = transaction;
            updateCommand.CommandText = "UPDATE DuplicateFiles SET MarkType = 2 WHERE FilePath = $filePath";
            var updatePathParam = new SqliteParameter("$filePath", "");
            updateCommand.Parameters.Add(updatePathParam);

            foreach (var file in files)
            {
                try
                {
                    var targetPath = CalculateTargetPath(file, _settings.DefaultMoveTargetBasePath);
                    sourcePathParam.Value = file;
                    targetPathParam.Value = targetPath;
                    insertCommand.ExecuteNonQuery();

                    updatePathParam.Value = file;
                    updateCommand.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"標記移動檔案失敗: {file}, 錯誤: {ex.Message}");
                    success = false;
                }
            }
        });

        return success;
    }

    static void ClearAllSkippedMarks()
    {
        DatabaseHelper.ExecuteTransaction((connection, transaction) =>
        {
            // 清除所有 SkippedHashes 記錄
            var deleteCommand = connection.CreateCommand();
            deleteCommand.Transaction = transaction;
            deleteCommand.CommandText = "DELETE FROM SkippedHashes";
            var count = deleteCommand.ExecuteNonQuery();

            // 清除所有 MarkType = 3 的標記
            var updateCommand = connection.CreateCommand();
            updateCommand.Transaction = transaction;
            updateCommand.CommandText = "UPDATE DuplicateFiles SET MarkType = 0 WHERE MarkType = 3";
            updateCommand.ExecuteNonQuery();

            Console.WriteLine($"已清除 {count} 筆略過標記記錄");
        });
    }

    /// <summary>
    /// 清除所有標記，將所有檔案回到未標記狀態
    /// </summary>
    static void ResetAllMarksToUnmarked()
    {
        Console.WriteLine("=== 清除所有標記 ===");
        Console.WriteLine();
        Console.WriteLine("此操作將清除：");
        Console.WriteLine("  - 所有待刪除標記（FilesToDelete + MarkType = 1）");
        Console.WriteLine("  - 所有待移動標記（FileToMove + MarkType = 2）");
        Console.WriteLine("  - 所有略過標記（SkippedHashes + MarkType = 3）");
        Console.WriteLine("  - 將所有檔案回到未標記狀態（MarkType = 0）");
        Console.WriteLine();

        if (!ConfirmAction("確認要清除所有標記嗎？"))
        {
            Console.WriteLine("已取消操作");
            return;
        }

        InitializeDatabase();

        Console.WriteLine();
        Console.WriteLine("正在清除標記...");
        Console.WriteLine();

        // 清除刪除標記
        Console.WriteLine("1. 清除待刪除標記...");
        ClearDeletedMarks();

        // 清除移動標記
        Console.WriteLine("2. 清除待移動標記...");
        ClearMoveMarks();

        // 清除略過標記
        Console.WriteLine("3. 清除略過標記...");
        ClearAllSkippedMarks();

        Console.WriteLine();
        Console.WriteLine("=== 所有標記已清除完成 ===");
        Console.WriteLine("所有檔案已回到未標記狀態，您可以重新開始標記流程。");
    }

    /// <summary>
    /// 驗證 Hash 輸入格式
    /// </summary>
    /// <param name="input">使用者輸入的 hash 字串</param>
    /// <returns>是否為有效的 64 字元 SHA-256 hash</returns>
    static bool ValidateHashInput(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        // SHA-256 hash 長度為 64 字元
        if (input.Length != 64)
            return false;

        // 只能包含 0-9, a-f, A-F
        return input.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'));
    }

    /// <summary>
    /// 產生檔案標記狀態綜合報表
    /// </summary>
    static void GenerateComprehensiveFileStatusReport()
    {
        InitializeDatabase();

        Console.WriteLine("正在載入檔案資料...");

        // 載入四種類型的檔案
        var unmarkedFiles = LoadUnmarkedFiles();
        var markedForDeletion = LoadMarkedForDeletionFiles();
        var markedForMove = LoadMarkedForMoveFiles();
        var skippedFiles = LoadSkippedFiles();

        var totalFiles = unmarkedFiles.Count + markedForDeletion.Count + markedForMove.Count + skippedFiles.Count;

        if (totalFiles == 0)
        {
            Console.WriteLine("目前沒有任何檔案記錄！");
            return;
        }

        Console.WriteLine($"已載入 {totalFiles} 個檔案記錄");
        Console.WriteLine($"  - 未標記: {unmarkedFiles.Count}");
        Console.WriteLine($"  - 已標記刪除: {markedForDeletion.Count}");
        Console.WriteLine($"  - 已標記移動: {markedForMove.Count}");
        Console.WriteLine($"  - 已標記略過: {skippedFiles.Count}");
        Console.WriteLine();

        Console.WriteLine("正在建立報表資料...");

        // 建立報表資料
        object reportData;
        try
        {
            reportData = CreateComprehensiveReportData(unmarkedFiles, markedForDeletion, markedForMove, skippedFiles);
            Console.WriteLine("報表資料建立完成");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"建立報表資料時發生錯誤: {ex.Message}");
            Console.WriteLine($"錯誤堆疊: {ex.StackTrace}");
            return;
        }

        // 產生檔案名稱
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var jsonFileName = $"Reports/ComprehensiveFileStatusReport_{timestamp}.json";
        var htmlFileName = $"Reports/ComprehensiveFileStatusReport_{timestamp}.html";

        // 確保 Reports 目錄存在
        Directory.CreateDirectory("Reports");

        // 產生 JSON 檔案
        Console.WriteLine("正在產生 JSON 報表...");
        try
        {
            var json = SerializeReportData(reportData, indent: true);
            File.WriteAllText(jsonFileName, json, Encoding.UTF8);
            Console.WriteLine($"JSON 報表已產生：{Path.GetFullPath(jsonFileName)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"產生 JSON 報表時發生錯誤: {ex.Message}");
        }

        // 產生 HTML 檔案
        Console.WriteLine("正在產生 HTML 報表...");
        try
        {
            var jsonCompact = SerializeReportData(reportData, indent: false);
            var template = File.ReadAllText("Templates/ComprehensiveFileStatusReport.html", Encoding.UTF8);
            var html = template.Replace("{{REPORT_DATA}}", jsonCompact);
            File.WriteAllText(htmlFileName, html, Encoding.UTF8);
            Console.WriteLine($"HTML 報表已產生：{Path.GetFullPath(htmlFileName)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"產生 HTML 報表時發生錯誤: {ex.Message}");
            return;
        }

        // 顯示統計資訊
        Console.WriteLine($"\n總共 {totalFiles} 個檔案記錄");

        // 自動開啟 HTML 報表
        Console.WriteLine("正在開啟 HTML 報表...");
        OpenHtmlReport(htmlFileName);
    }

    /// <summary>
    /// 建立綜合報表資料結構
    /// </summary>
    static object CreateComprehensiveReportData(
        List<ComprehensiveFileInfo> unmarkedFiles,
        List<ComprehensiveFileInfo> markedForDeletion,
        List<ComprehensiveFileInfo> markedForMove,
        List<ComprehensiveFileInfo> skippedFiles)
    {
        Console.WriteLine("  載入重複檔案群組統計...");
        // 載入重複檔案群組統計
        var groupStats = LoadDuplicateGroupStatistics();

        // 計算群組相關統計（只計算重複數量 > 1 的群組，並按最大檔案大小降序排列）
        var duplicateGroups = groupStats.Values
            .Where(g => g.DuplicateCount > 1)
            .OrderByDescending(g => g.MaxFileSize)
            .ToList();

        // 按重複次數排序的群組列表（次要排序為總大小）
        var duplicateGroupsByCount = groupStats.Values
            .Where(g => g.DuplicateCount > 1)
            .OrderByDescending(g => g.DuplicateCount)
            .ThenByDescending(g => g.TotalSize)
            .ToList();

        var totalDuplicateGroups = duplicateGroups.Count;
        var maxDuplicateCount = duplicateGroups.Any() ? duplicateGroups.Max(g => g.DuplicateCount) : 0;

        Console.WriteLine($"  找到 {totalDuplicateGroups} 個重複檔案群組");

        // 合併所有檔案列表並按 Hash 分組（效能優化：避免重複查詢）
        Console.WriteLine("  合併檔案列表並分組...");
        var allFiles = new List<ComprehensiveFileInfo>();
        allFiles.AddRange(unmarkedFiles);
        allFiles.AddRange(markedForDeletion);
        allFiles.AddRange(markedForMove);
        allFiles.AddRange(skippedFiles);

        var filesByHash = allFiles.GroupBy(f => f.Hash)
                                   .ToDictionary(g => g.Key, g => g.OrderByDescending(f => f.FileSize).ToList());

        Console.WriteLine("  建立報表資料結構...");

        return new
        {
            GeneratedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            Summary = new
            {
                TotalFiles = unmarkedFiles.Count + markedForDeletion.Count + markedForMove.Count + skippedFiles.Count,
                UnmarkedCount = unmarkedFiles.Count,
                MarkedForDeletionCount = markedForDeletion.Count,
                MarkedForMoveCount = markedForMove.Count,
                SkippedCount = skippedFiles.Count,
                UnmarkedSize = unmarkedFiles.Sum(f => f.FileSize),
                MarkedForDeletionSize = markedForDeletion.Sum(f => f.FileSize),
                MarkedForMoveSize = markedForMove.Sum(f => f.FileSize),
                SkippedSize = skippedFiles.Sum(f => f.FileSize),
                // 新增：重複檔案群組統計
                TotalDuplicateGroups = totalDuplicateGroups,
                MaxDuplicateCount = maxDuplicateCount,
                TotalDuplicateGroupsSize = duplicateGroups.Sum(g => g.TotalSize)
            },
            // 新增：重複檔案群組列表（按最大檔案大小降序排列）
            DuplicateGroups = duplicateGroups.Select(g =>
            {
                var files = filesByHash.ContainsKey(g.Hash)
                    ? filesByHash[g.Hash].Select(f => (object)new
                    {
                        f.FilePath,
                        f.FileSize,
                        f.MarkType,
                        f.FileCreatedTime,
                        f.FileLastModifiedTime,
                        f.Exists
                    }).ToList()
                    : new List<object>();

                return new
                {
                    g.Hash,
                    g.DuplicateCount,
                    g.TotalSize,
                    g.MaxFileSize,
                    Files = files
                };
            }).ToList(),
            // 新增：重複檔案群組列表（按重複次數降序排列）
            DuplicateGroupsByCount = duplicateGroupsByCount.Select(g =>
            {
                var files = filesByHash.ContainsKey(g.Hash)
                    ? filesByHash[g.Hash].Select(f => (object)new
                    {
                        f.FilePath,
                        f.FileSize,
                        f.MarkType,
                        f.FileCreatedTime,
                        f.FileLastModifiedTime,
                        f.Exists
                    }).ToList()
                    : new List<object>();

                return new
                {
                    g.Hash,
                    g.DuplicateCount,
                    g.TotalSize,
                    g.MaxFileSize,
                    Files = files
                };
            }).ToList(),
            UnmarkedFiles = unmarkedFiles.Select(f => new
            {
                f.FilePath,
                f.Hash,
                f.FileSize,
                f.FileCreatedTime,
                f.FileLastModifiedTime,
                f.Exists,
                f.DuplicateCount
            }).ToList(),
            MarkedForDeletion = markedForDeletion.Select(f => new
            {
                f.FilePath,
                f.Hash,
                f.FileSize,
                f.FileCreatedTime,
                f.FileLastModifiedTime,
                f.Exists,
                f.DuplicateCount,
                f.MarkedAt,
                IsProcessed = f.IsProcessed ?? 0,
                f.ProcessedAt
            }).ToList(),
            MarkedForMove = markedForMove.Select(f => new
            {
                f.FilePath,
                f.Hash,
                f.FileSize,
                f.FileCreatedTime,
                f.FileLastModifiedTime,
                f.Exists,
                f.DuplicateCount,
                f.MarkedAt,
                IsProcessed = f.IsProcessed ?? 0,
                f.ProcessedAt,
                f.TargetPath
            }).ToList(),
            SkippedFiles = skippedFiles.Select(f => new
            {
                f.FilePath,
                f.Hash,
                f.FileSize,
                f.FileCreatedTime,
                f.FileLastModifiedTime,
                f.Exists,
                f.DuplicateCount,
                SkippedAt = f.MarkedAt
            }).ToList()
        };
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
            @"SELECT Hash, FilePath
              FROM DuplicateFiles
              WHERE MarkType = 0",
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

            CREATE TABLE IF NOT EXISTS FileToMove (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Hash TEXT NOT NULL,
                SourcePath TEXT NOT NULL UNIQUE,
                TargetPath TEXT NOT NULL,
                MarkedAt TEXT NOT NULL
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

        // 如果 MarkType 欄位不存在，則新增（0=無標記, 1=刪除標記, 2=移動標記, 3=跳過標記）
        if (!existingColumns.Contains("MarkType"))
        {
            try
            {
                var alterCommand3 = connection.CreateCommand();
                alterCommand3.CommandText = "ALTER TABLE DuplicateFiles ADD COLUMN MarkType INTEGER NOT NULL DEFAULT 0";
                alterCommand3.ExecuteNonQuery();
                Console.WriteLine("已新增 MarkType 欄位到 DuplicateFiles 資料表");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"警告：無法新增 MarkType 欄位: {ex.Message}");
            }
        }

        // MoveTo 欄位已由 FileToMove 資料表取代，因此移除
        if (existingColumns.Contains("MoveTo"))
        {
            // 這段程式碼是為了舊版資料庫相容性，未來可以移除
            try
            {
                var alterCommand = connection.CreateCommand();
                alterCommand.CommandText = "ALTER TABLE DuplicateFiles DROP COLUMN MoveTo";
                alterCommand.ExecuteNonQuery();
                Console.WriteLine("已從 DuplicateFiles 資料表移除舊的 MoveTo 欄位");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"警告：無法移除舊的 MoveTo 欄位: {ex.Message}");
            }
        }

        // 如果 FileLastModifiedTime 欄位不存在，則新增
        if (!existingColumns.Contains("FileLastModifiedTime"))
        {
            try
            {
                var alterCommand5 = connection.CreateCommand();
                alterCommand5.CommandText = "ALTER TABLE DuplicateFiles ADD COLUMN FileLastModifiedTime TEXT";
                alterCommand5.ExecuteNonQuery();
                Console.WriteLine("已新增 FileLastModifiedTime 欄位到 DuplicateFiles 資料表");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"警告：無法新增 FileLastModifiedTime 欄位: {ex.Message}");
            }
        }

        // 第三步：為 FilesToDelete 新增 IsProcessed 和 ProcessedAt 欄位
        var checkFilesToDeleteCommand = connection.CreateCommand();
        checkFilesToDeleteCommand.CommandText = "PRAGMA table_info(FilesToDelete)";
        var filesToDeleteColumns = new HashSet<string>();

        using (var reader = checkFilesToDeleteCommand.ExecuteReader())
        {
            while (reader.Read())
            {
                filesToDeleteColumns.Add(reader.GetString(1));
            }
        }

        if (!filesToDeleteColumns.Contains("IsProcessed"))
        {
            try
            {
                var alterCommand = connection.CreateCommand();
                alterCommand.CommandText = "ALTER TABLE FilesToDelete ADD COLUMN IsProcessed INTEGER NOT NULL DEFAULT 0";
                alterCommand.ExecuteNonQuery();
                Console.WriteLine("已新增 IsProcessed 欄位到 FilesToDelete 資料表");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"警告：無法新增 IsProcessed 欄位到 FilesToDelete: {ex.Message}");
            }
        }

        if (!filesToDeleteColumns.Contains("ProcessedAt"))
        {
            try
            {
                var alterCommand = connection.CreateCommand();
                alterCommand.CommandText = "ALTER TABLE FilesToDelete ADD COLUMN ProcessedAt TEXT";
                alterCommand.ExecuteNonQuery();
                Console.WriteLine("已新增 ProcessedAt 欄位到 FilesToDelete 資料表");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"警告：無法新增 ProcessedAt 欄位到 FilesToDelete: {ex.Message}");
            }
        }

        // 第四步：為 FileToMove 新增 IsProcessed 和 ProcessedAt 欄位
        var checkFileToMoveCommand = connection.CreateCommand();
        checkFileToMoveCommand.CommandText = "PRAGMA table_info(FileToMove)";
        var fileToMoveColumns = new HashSet<string>();

        using (var reader = checkFileToMoveCommand.ExecuteReader())
        {
            while (reader.Read())
            {
                fileToMoveColumns.Add(reader.GetString(1));
            }
        }

        if (!fileToMoveColumns.Contains("IsProcessed"))
        {
            try
            {
                var alterCommand = connection.CreateCommand();
                alterCommand.CommandText = "ALTER TABLE FileToMove ADD COLUMN IsProcessed INTEGER NOT NULL DEFAULT 0";
                alterCommand.ExecuteNonQuery();
                Console.WriteLine("已新增 IsProcessed 欄位到 FileToMove 資料表");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"警告：無法新增 IsProcessed 欄位到 FileToMove: {ex.Message}");
            }
        }

        if (!fileToMoveColumns.Contains("ProcessedAt"))
        {
            try
            {
                var alterCommand = connection.CreateCommand();
                alterCommand.CommandText = "ALTER TABLE FileToMove ADD COLUMN ProcessedAt TEXT";
                alterCommand.ExecuteNonQuery();
                Console.WriteLine("已新增 ProcessedAt 欄位到 FileToMove 資料表");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"警告：無法新增 ProcessedAt 欄位到 FileToMove: {ex.Message}");
            }
        }

        // 第五步：建立索引（在欄位確定存在後）
        var indexCommand = connection.CreateCommand();
        indexCommand.CommandText = @"
            CREATE INDEX IF NOT EXISTS idx_hash ON DuplicateFiles(Hash);
            CREATE INDEX IF NOT EXISTS idx_mark_type ON DuplicateFiles(MarkType);
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
                INSERT INTO DuplicateFiles (Hash, FilePath, FileName, FileSize, FileCreatedTime, FileLastModifiedTime, FileCount, CreatedAt)
                VALUES ($hash, $filePath, $fileName, $fileSize, $fileCreatedTime, $fileLastModifiedTime, $fileCount, $createdAt)
                ON CONFLICT(Hash, FilePath) DO UPDATE SET
                    FileName = excluded.FileName,
                    FileSize = excluded.FileSize,
                    FileCreatedTime = excluded.FileCreatedTime,
                    FileLastModifiedTime = excluded.FileLastModifiedTime,
                    FileCount = excluded.FileCount,
                    CreatedAt = excluded.CreatedAt";

            var hashParam = new SqliteParameter("$hash", "");
            var filePathParam = new SqliteParameter("$filePath", "");
            var fileNameParam = new SqliteParameter("$fileName", "");
            var fileSizeParam = new SqliteParameter("$fileSize", 0L);
            var fileCreatedTimeParam = new SqliteParameter("$fileCreatedTime", "");
            var fileLastModifiedTimeParam = new SqliteParameter("$fileLastModifiedTime", "");
            var fileCountParam = new SqliteParameter("$fileCount", 0);
            var createdAtParam = new SqliteParameter("$createdAt", createdAt);

            command.Parameters.Add(hashParam);
            command.Parameters.Add(filePathParam);
            command.Parameters.Add(fileNameParam);
            command.Parameters.Add(fileSizeParam);
            command.Parameters.Add(fileCreatedTimeParam);
            command.Parameters.Add(fileLastModifiedTimeParam);
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
                    fileLastModifiedTimeParam.Value = fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss");
                    fileCountParam.Value = files.Count;

                    command.ExecuteNonQuery();
                }
            }
        });
    }

    #region 資料夾民國年轉西元年功能

    /// <summary>
    /// 執行資料夾民國年轉西元年流程
    /// </summary>
    static void RunROCFolderRename()
    {
        Console.WriteLine("=== 資料夾民國年轉西元年 ===");
        Console.WriteLine();

        // 輸入資料夾路徑
        Console.Write("請輸入要掃描的資料夾路徑: ");
        var basePath = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(basePath))
        {
            Console.WriteLine("路徑不可為空！");
            return;
        }

        // 驗證路徑是否存在
        if (!Directory.Exists(basePath))
        {
            Console.WriteLine($"資料夾不存在: {basePath}");
            return;
        }

        Console.WriteLine();
        Console.WriteLine("開始掃描資料夾...");
        Console.WriteLine();

        // 掃描符合民國年格式的資料夾
        var renameActions = ScanROCFolders(basePath);

        if (renameActions.Count == 0)
        {
            Console.WriteLine("未找到符合民國年格式的資料夾！");
            Console.WriteLine();
            Console.WriteLine("支援格式範例:");
            Console.WriteLine("  【民國 99 年以前 (兩位數)】");
            Console.WriteLine("  - 990101 帥爆了 → 2010-0101 帥爆了 (6位數字 YYMMDD)");
            Console.WriteLine("  - 9901帥爆了 → 2010-01 帥爆了 (4位數字 YYMM)");
            Console.WriteLine("  - 990101-1 帥爆了 → 2010-0101-1 帥爆了 (含流水號)");
            Console.WriteLine();
            Console.WriteLine("  【民國 100 年以後 (三位數)】");
            Console.WriteLine("  - 1000101 帥爆了 → 2011-0101 帥爆了 (7位數字 YYYMMDD)");
            Console.WriteLine("  - 10001帥爆了 → 2011-01 帥爆了 (5位數字 YYYMM)");
            Console.WriteLine("  - 1140101-1 帥爆了 → 2025-0101-1 帥爆了 (含流水號)");
            return;
        }

        // 預覽轉換結果
        PreviewROCConversion(renameActions);

        // 確認是否執行
        Console.WriteLine();
        if (!ConfirmAction("確定要執行重命名嗎？"))
        {
            Console.WriteLine("已取消操作");
            return;
        }

        // 執行重命名
        Console.WriteLine();
        ExecuteROCFolderRename(renameActions);
    }

    /// <summary>
    /// 掃描指定路徑下所有符合民國年格式的資料夾
    /// </summary>
    static List<RenameAction> ScanROCFolders(string basePath)
    {
        var renameActions = new List<RenameAction>();

        try
        {
            // 獲取所有子資料夾（僅一層，不遞迴）
            var directories = Directory.GetDirectories(basePath);

            foreach (var dir in directories)
            {
                var dirInfo = new DirectoryInfo(dir);
                var originalName = dirInfo.Name;
                var newName = CalendarConvert.ConvertROCToADFolderName(originalName);

                if (newName != null)
                {
                    // 檢查目標名稱是否已存在
                    var targetPath = Path.Combine(dirInfo.Parent!.FullName, newName);
                    var hasConflict = Directory.Exists(targetPath) &&
                                     !string.Equals(targetPath, dir, StringComparison.OrdinalIgnoreCase);

                    renameActions.Add(new RenameAction(
                        OriginalName: originalName,
                        NewName: newName,
                        FullPath: dir,
                        ParentPath: dirInfo.Parent!.FullName,
                        HasConflict: hasConflict
                    ));
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"掃描資料夾時發生錯誤: {ex.Message}");
        }

        return renameActions;
    }

    /// <summary>
    /// 預覽轉換結果
    /// </summary>
    static void PreviewROCConversion(List<RenameAction> actions)
    {
        Console.WriteLine($"找到 {actions.Count} 個符合民國年格式的資料夾");
        Console.WriteLine();
        Console.WriteLine("轉換預覽：");
        Console.WriteLine(new string('=', 80));
        Console.WriteLine($"{"序號",-5} {"原始名稱",-30} {"新名稱",-30} {"狀態",-10}");
        Console.WriteLine(new string('-', 80));

        var validCount = 0;
        var conflictCount = 0;

        for (int i = 0; i < actions.Count; i++)
        {
            var action = actions[i];
            var status = action.HasConflict ? "❌ 衝突" : "✓ 可重命名";

            if (!action.HasConflict)
                validCount++;
            else
                conflictCount++;

            // 截斷過長的名稱
            var originalDisplay = action.OriginalName.Length > 28
                ? action.OriginalName.Substring(0, 25) + "..."
                : action.OriginalName;
            var newDisplay = action.NewName.Length > 28
                ? action.NewName.Substring(0, 25) + "..."
                : action.NewName;

            Console.WriteLine($"{i + 1,-5} {originalDisplay,-30} {newDisplay,-30} {status,-10}");
        }

        Console.WriteLine(new string('=', 80));
        Console.WriteLine();
        Console.WriteLine($"統計: 總共 {actions.Count} 個 | 可重命名 {validCount} 個 | 衝突 {conflictCount} 個");

        if (conflictCount > 0)
        {
            Console.WriteLine();
            Console.WriteLine("⚠️  注意: 有命名衝突的資料夾將被自動略過");
        }
    }

    /// <summary>
    /// 執行資料夾重命名
    /// </summary>
    static void ExecuteROCFolderRename(List<RenameAction> actions)
    {
        Console.WriteLine("開始執行重命名...");
        Console.WriteLine();

        var successCount = 0;
        var skipCount = 0;
        var errorCount = 0;

        foreach (var action in actions)
        {
            try
            {
                // 略過有衝突的項目
                if (action.HasConflict)
                {
                    Console.WriteLine($"略過 (衝突): {action.OriginalName}");
                    skipCount++;
                    continue;
                }

                // 執行重命名
                var targetPath = Path.Combine(action.ParentPath, action.NewName);
                Directory.Move(action.FullPath, targetPath);

                Console.WriteLine($"✓ {action.OriginalName} → {action.NewName}");
                successCount++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 失敗: {action.OriginalName} - {ex.Message}");
                errorCount++;
            }
        }

        Console.WriteLine();
        Console.WriteLine(new string('=', 80));
        Console.WriteLine("執行結果：");
        Console.WriteLine($"  成功: {successCount} 個");
        Console.WriteLine($"  略過: {skipCount} 個");
        Console.WriteLine($"  失敗: {errorCount} 個");
        Console.WriteLine(new string('=', 80));
    }

    #endregion

}