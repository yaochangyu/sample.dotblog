using System.Globalization;

namespace IdempotencyKey.WebApi;

public static class DateTimeExtensions
{
    public const string DefaultDateTimeFormat = "o";
    public static readonly CultureInfo DefaultDateTimeCultureInfo = CultureInfo.InvariantCulture;

    public static IEnumerable<(DateTime start, DateTime end)> Each(this DateTime inputStart, DateTime inputEnd,
        TimeSpan step)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(step, TimeSpan.Zero, nameof(step));
        DateTime dtStart = inputStart;
        while (dtStart < inputEnd)
        {
            var dtEnd = dtStart + step;
            if (dtEnd > inputEnd)
                dtEnd = inputEnd;

            yield return (dtStart, dtEnd);

            dtStart += step;
        }
    }

    public static string ToUtcString(this DateTimeOffset dto)
    {
        return dto.UtcDateTime.ToString(DefaultDateTimeFormat, DefaultDateTimeCultureInfo);
    }

    public static string ToUtcString(this DateTime dt)
    {
        return dt.ToString(DefaultDateTimeFormat, DefaultDateTimeCultureInfo);
    }
}
