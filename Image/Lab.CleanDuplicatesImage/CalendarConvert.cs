using System.Globalization;

namespace Lab.CleanDuplicatesImage;

/// <summary>
/// 民國年與西元年轉換工具類別
/// </summary>
public static class CalendarConvert
{
    /// <summary>
    /// 將民國年資料夾名稱轉換為西元年格式
    /// </summary>
    /// <param name="folderName">原始資料夾名稱</param>
    /// <returns>轉換後的名稱，若不符合規則則回傳 null</returns>
    public static string? ConvertROCToADFolderName(string folderName)
    {
        if (string.IsNullOrWhiteSpace(folderName))
            return null;

        var taiwanCalendar = new TaiwanCalendar();

        // 嘗試匹配完整日期格式 (YYYMMDD 或 YYMMDD)
        // 範例: 1000101, 990101, 990101-1, 990101帥爆了
        var patternFullDate = @"^(\d{2,3})(\d{2})(\d{2})(-\d+)?(.*)$";
        var matchFullDate = System.Text.RegularExpressions.Regex.Match(folderName, patternFullDate);

        if (matchFullDate.Success)
        {
            var rocYear = int.Parse(matchFullDate.Groups[1].Value);
            var month = int.Parse(matchFullDate.Groups[2].Value);
            var day = int.Parse(matchFullDate.Groups[3].Value);
            var sequence = matchFullDate.Groups[4].Value; // 流水號 (如 -1, -2)
            var suffix = matchFullDate.Groups[5].Value;   // 後綴文字

            try
            {
                // 使用 TaiwanCalendar 驗證並轉換日期
                var adDate = taiwanCalendar.ToDateTime(rocYear, month, day, 0, 0, 0, 0);

                // 組合新名稱: 2011-0101 或 2010-0101-1
                var newName = $"{adDate.Year}-{month:D2}{day:D2}{sequence}";

                // 如果有後綴文字且不是以空白開頭，自動加入空格
                if (!string.IsNullOrEmpty(suffix) && !suffix.StartsWith(" "))
                {
                    newName += " " + suffix;
                }
                else
                {
                    newName += suffix;
                }

                return newName;
            }
            catch (ArgumentOutOfRangeException)
            {
                // 無效的日期，忽略此資料夾
                return null;
            }
        }

        // 嘗試匹配年月格式 (YYYMM 或 YYMM)
        // 範例: 10001, 9901, 9901帥爆了
        var patternYearMonth = @"^(\d{2,3})(\d{2})(?!-?\d)(.*)$";
        var matchYearMonth = System.Text.RegularExpressions.Regex.Match(folderName, patternYearMonth);

        if (matchYearMonth.Success)
        {
            var rocYear = int.Parse(matchYearMonth.Groups[1].Value);
            var month = int.Parse(matchYearMonth.Groups[2].Value);
            var suffix = matchYearMonth.Groups[3].Value;

            // 基本範圍檢查（避免誤判）
            if (rocYear < 50 || rocYear > 150 || month < 1 || month > 12)
                return null;

            try
            {
                // 使用每月第一天來驗證年月
                var adDate = taiwanCalendar.ToDateTime(rocYear, month, 1, 0, 0, 0, 0);

                // 組合新名稱: 2011-01 或 2010-01
                var newName = $"{adDate.Year}-{month:D2}";

                // 如果有後綴文字且不是以空白開頭，自動加入空格
                if (!string.IsNullOrEmpty(suffix) && !suffix.StartsWith(" "))
                {
                    newName += " " + suffix;
                }
                else
                {
                    newName += suffix;
                }

                return newName;
            }
            catch (ArgumentOutOfRangeException)
            {
                // 無效的年月，忽略此資料夾
                return null;
            }
        }

        return null;
    }
}
