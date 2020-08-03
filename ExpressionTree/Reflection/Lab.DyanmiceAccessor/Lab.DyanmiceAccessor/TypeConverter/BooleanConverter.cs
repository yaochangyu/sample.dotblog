using System;

/// <summary>
/// The TypeConverters namespace.
/// </summary>

namespace Lab.DynamicAccessor.TypeConverter
{
    /// <summary>
    ///     Class BooleanConverter.
    /// </summary>
    public class BooleanConverter : ITypeConverter
    {
        /// <summary>
        ///     Converts the specified source value.
        /// </summary>
        /// <param name="sourceValue">The source value.</param>
        /// <returns>System.Object.</returns>
        public object Convert(object sourceValue)
        {
            if (sourceValue == null || sourceValue == DBNull.Value)
            {
                return null;
            }

            if (string.IsNullOrEmpty(sourceValue.ToString()))
            {
                return null;
            }

            if (sourceValue.ToString() == "0")
            {
                return false;
            }

            if (sourceValue.ToString() == "1")
            {
                return true;
            }

            return System.Convert.ToBoolean(sourceValue);
        }
    }
}