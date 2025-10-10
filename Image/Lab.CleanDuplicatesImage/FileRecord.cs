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