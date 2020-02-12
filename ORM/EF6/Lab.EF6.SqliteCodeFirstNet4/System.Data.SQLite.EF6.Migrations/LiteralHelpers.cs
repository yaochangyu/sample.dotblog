using System.Text;

namespace System.Data.SQLite.EF6.Migrations
{
    internal static class LiteralHelpers
    {
        private static readonly char[] HexDigits =
            {'0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F'};

        /// <summary>
        ///     Transform the given <see cref="System.DateTime" /> value in a string formatted in a valid format,
        ///     that represents a date and a time
        /// </summary>
        /// <param name="time">The <see cref="System.DateTime" /> value to format</param>
        /// <returns>The string that represents the today time, to include in a where clause</returns>
        public static string SqlDateTime(DateTime time)
        {
            return string.Format("#{0:MM/dd/yyyy} {0:HH.mm.ss}#", time);
        }

        /// <summary>
        ///     Transform the given <see cref="System.TimeSpan" /> value in a string formatted in a valid Jet format,
        ///     that represents the today time
        /// </summary>
        /// <param name="time">The <see cref="System.TimeSpan" /> value to format</param>
        /// <returns>The string that represents the today time, to include in a where clause</returns>
        public static string SqlDayTime(TimeSpan time)
        {
            return string.Format("#{0:00}.{1:00}.{2:00}#", time.Hours, time.Minutes, time.Seconds);
        }

        /// <summary>
        ///     Transform the given <see cref="System.TimeSpan" /> value in a string formatted in a valid Jet format,
        ///     that represents the today time
        /// </summary>
        /// <param name="time">The <see cref="System.TimeSpan" /> value to format</param>
        /// <returns>The string that represents the today time, to include in a where clause</returns>
        public static string SqlDayTime(DateTime time)
        {
            return string.Format("#{0:00}.{1:00}.{2:00}#", time.Hour, time.Minute, time.Second);
        }

        public static string ToSqlString(byte[] value)
        {
            return " X'" + ByteArrayToBinaryString(value) + "' ";
        }

        public static string ToSqlString(bool value)
        {
            return value ? "true" : "false";
        }

        public static string ToSqlString(string value)
        {
            // In SQLite everything's unicode
            return "'" + value.Replace("'", "''") + "'";
        }

        public static string ToSqlString(Guid value)
        {
            // In SQLite everything's unicode
            return "'P" + value + "'";
        }

        private static string ByteArrayToBinaryString(byte[] binaryArray)
        {
            var sb = new StringBuilder(binaryArray.Length * 2);
            for (var i = 0; i < binaryArray.Length; i++)
            {
                sb.Append(HexDigits[(binaryArray[i] & 0xF0) >> 4]).Append(HexDigits[binaryArray[i] & 0x0F]);
            }

            return sb.ToString();
        }
    }
}