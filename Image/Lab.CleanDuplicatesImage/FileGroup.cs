namespace Lab.CleanDuplicatesImage;

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