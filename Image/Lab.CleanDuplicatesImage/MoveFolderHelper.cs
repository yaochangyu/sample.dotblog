using System.Security.Cryptography;

namespace Lab.CleanDuplicatesImage;

/// <summary>
/// 檔案移動資訊記錄
/// </summary>
/// <param name="SourcePath">來源檔案完整路徑</param>
/// <param name="TargetPath">目標檔案完整路徑</param>
/// <param name="FileName">檔案名稱</param>
/// <param name="LastModifiedTime">檔案最後修改時間</param>
/// <param name="FileSize">檔案大小（位元組）</param>
/// <param name="Hash">檔案 SHA-256 雜湊值</param>
record FileMoveInfo(
    string SourcePath,
    string TargetPath,
    string FileName,
    DateTime LastModifiedTime,
    long FileSize,
    string Hash
);

/// <summary>
/// 資料夾移動輔助類別
/// </summary>
public class MoveFolderHelper
{
    private readonly AppSettings _settings;

    /// <summary>
    /// 建構函式
    /// </summary>
    /// <param name="settings">應用程式設定</param>
    public MoveFolderHelper(AppSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <summary>
    /// 移動資料夾到預設位置（根據檔案修改日期分類）
    /// </summary>
    public void RunMoveFolderToDefaultLocation()
    {
        Console.WriteLine("=== 移動資料夾到預設位置 ===");
        Console.WriteLine($"目標基礎路徑: {_settings.DefaultMoveTargetBasePath}");
        Console.WriteLine();

        // 輸入來源資料夾路徑
        Console.Write("請輸入要移動的資料夾路徑: ");
        var sourceFolderPath = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(sourceFolderPath))
        {
            Console.WriteLine("路徑不可為空！");
            return;
        }

        // 驗證來源路徑是否存在
        if (!Directory.Exists(sourceFolderPath))
        {
            Console.WriteLine($"資料夾不存在: {sourceFolderPath}");
            return;
        }

        // 驗證目標基礎路徑是否存在
        if (!Directory.Exists(_settings.DefaultMoveTargetBasePath))
        {
            Console.WriteLine($"目標基礎路徑不存在: {_settings.DefaultMoveTargetBasePath}");
            Console.Write("是否要建立此路徑？(Y/n): ");
            var createPath = Console.ReadLine()?.Trim();
            if (!string.Equals(createPath, "Y", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(createPath, "y", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("已取消操作");
                return;
            }

            try
            {
                Directory.CreateDirectory(_settings.DefaultMoveTargetBasePath);
                Console.WriteLine($"已建立目標路徑: {_settings.DefaultMoveTargetBasePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"建立目標路徑失敗: {ex.Message}");
                return;
            }
        }

        Console.WriteLine();
        Console.WriteLine("開始掃描資料夾中的檔案...");
        Console.WriteLine();

        // 掃描所有檔案
        var filesToMove = ScanFilesForMove(sourceFolderPath, _settings.DefaultMoveTargetBasePath);

        if (filesToMove.Count == 0)
        {
            Console.WriteLine("資料夾中沒有找到任何檔案！");
            return;
        }

        // 顯示掃描結果
        Console.WriteLine($"找到 {filesToMove.Count} 個檔案");
        Console.WriteLine();

        // 預覽移動操作
        PreviewMoveOperations(filesToMove);

        // 確認是否執行
        Console.WriteLine();
        if (!ConfirmAction("確定要執行移動嗎？"))
        {
            Console.WriteLine("已取消操作");
            return;
        }

        // 執行移動
        Console.WriteLine();
        ExecuteFolderMove(filesToMove);
    }

    /// <summary>
    /// 掃描資料夾中的所有檔案並計算目標路徑
    /// </summary>
    /// <param name="folderPath">來源資料夾路徑</param>
    /// <param name="baseTargetPath">目標基礎路徑</param>
    /// <returns>檔案移動資訊清單</returns>
    private List<FileMoveInfo> ScanFilesForMove(string folderPath, string baseTargetPath)
    {
        var filesToMove = new List<FileMoveInfo>();

        try
        {
            // 遞迴掃描所有檔案
            var allFiles = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);

            foreach (var filePath in allFiles)
            {
                try
                {
                    var fileInfo = new FileInfo(filePath);
                    var lastModified = fileInfo.LastWriteTime;

                    // 根據修改日期計算目標路徑 (yyyy-MM)
                    var targetSubFolder = lastModified.ToString("yyyy-MM");
                    var targetFolderPath = Path.Combine(baseTargetPath, targetSubFolder);
                    var targetFilePath = Path.Combine(targetFolderPath, fileInfo.Name);

                    // 計算檔案 Hash（用於後續衝突檢測）
                    var hash = CalculateSHA256(filePath);

                    filesToMove.Add(new FileMoveInfo(
                        SourcePath: filePath,
                        TargetPath: targetFilePath,
                        FileName: fileInfo.Name,
                        LastModifiedTime: lastModified,
                        FileSize: fileInfo.Length,
                        Hash: hash
                    ));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"處理檔案時發生錯誤 ({filePath}): {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"掃描資料夾時發生錯誤: {ex.Message}");
        }

        return filesToMove;
    }

    /// <summary>
    /// 預覽移動操作
    /// </summary>
    /// <param name="filesToMove">檔案移動資訊清單</param>
    private void PreviewMoveOperations(List<FileMoveInfo> filesToMove)
    {
        Console.WriteLine(new string('=', 100));
        Console.WriteLine("移動操作預覽：");
        Console.WriteLine(new string('=', 100));

        // 按目標資料夾分組
        var groupedByFolder = filesToMove.GroupBy(f => Path.GetDirectoryName(f.TargetPath))
                                         .OrderBy(g => g.Key);

        foreach (var group in groupedByFolder)
        {
            Console.WriteLine();
            Console.WriteLine($"目標資料夾: {group.Key}");
            Console.WriteLine($"  檔案數量: {group.Count()} 個");

            // 顯示前 5 個檔案作為範例
            var samples = group.Take(5).ToList();
            foreach (var file in samples)
            {
                Console.WriteLine($"  - {file.FileName} ({FormatFileSize(file.FileSize)}) [{file.LastModifiedTime:yyyy-MM-dd HH:mm:ss}]");
            }

            if (group.Count() > 5)
            {
                Console.WriteLine($"  ... 還有 {group.Count() - 5} 個檔案");
            }
        }

        Console.WriteLine();
        Console.WriteLine(new string('=', 100));
    }

    /// <summary>
    /// 執行資料夾移動操作
    /// </summary>
    /// <param name="filesToMove">檔案移動資訊清單</param>
    private void ExecuteFolderMove(List<FileMoveInfo> filesToMove)
    {
        Console.WriteLine("開始執行移動操作...");
        Console.WriteLine();

        var successCount = 0;
        var overwriteCount = 0;
        var renameCount = 0;
        var errorCount = 0;

        foreach (var fileInfo in filesToMove)
        {
            try
            {
                // 確保目標資料夾存在
                var targetFolder = Path.GetDirectoryName(fileInfo.TargetPath);
                if (!string.IsNullOrEmpty(targetFolder) && !Directory.Exists(targetFolder))
                {
                    Directory.CreateDirectory(targetFolder);
                }

                // 檢查目標檔案是否存在
                if (File.Exists(fileInfo.TargetPath))
                {
                    // 計算目標檔案的 Hash
                    var targetHash = CalculateSHA256(fileInfo.TargetPath);

                    if (targetHash == fileInfo.Hash)
                    {
                        // Hash 相同，直接覆蓋（刪除目標後移動來源）
                        File.Delete(fileInfo.TargetPath);
                        File.Move(fileInfo.SourcePath, fileInfo.TargetPath);
                        Console.WriteLine($"✓ 覆蓋 (相同內容): {fileInfo.FileName}");
                        overwriteCount++;
                    }
                    else
                    {
                        // Hash 不同，檔名加上 Guid
                        var extension = Path.GetExtension(fileInfo.FileName);
                        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileInfo.FileName);
                        var newFileName = $"{fileNameWithoutExt}_{Guid.NewGuid():N}{extension}";
                        var newTargetPath = Path.Combine(Path.GetDirectoryName(fileInfo.TargetPath)!, newFileName);

                        File.Move(fileInfo.SourcePath, newTargetPath);
                        Console.WriteLine($"✓ 重新命名: {fileInfo.FileName} → {newFileName}");
                        renameCount++;
                    }
                }
                else
                {
                    // 目標檔案不存在，直接移動
                    File.Move(fileInfo.SourcePath, fileInfo.TargetPath);
                    Console.WriteLine($"✓ 移動: {fileInfo.FileName}");
                    successCount++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 失敗: {fileInfo.FileName} - {ex.Message}");
                errorCount++;
            }
        }

        Console.WriteLine();
        Console.WriteLine(new string('=', 80));
        Console.WriteLine("執行結果：");
        Console.WriteLine($"  成功移動: {successCount} 個");
        Console.WriteLine($"  覆蓋檔案: {overwriteCount} 個");
        Console.WriteLine($"  重新命名: {renameCount} 個");
        Console.WriteLine($"  失敗: {errorCount} 個");
        Console.WriteLine(new string('=', 80));
    }

    /// <summary>
    /// 計算檔案的 SHA-256 雜湊值
    /// </summary>
    /// <param name="filePath">檔案路徑</param>
    /// <returns>SHA-256 雜湊值（小寫十六進位字串）</returns>
    private string CalculateSHA256(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hash = sha256.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    /// <summary>
    /// 格式化檔案大小
    /// </summary>
    /// <param name="bytes">位元組數</param>
    /// <returns>格式化後的檔案大小字串</returns>
    private string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    /// <summary>
    /// 確認動作
    /// </summary>
    /// <param name="message">確認訊息</param>
    /// <returns>使用者是否確認</returns>
    private bool ConfirmAction(string message)
    {
        Console.Write($"{message} (Y/n): ");
        var response = Console.ReadLine()?.Trim();
        return string.Equals(response, "Y", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(response, "y", StringComparison.OrdinalIgnoreCase);
    }
}
