using System;

/// <summary>
/// The TypeConverters namespace.
/// </summary>

namespace Lab.DynamicAccessor.TypeConverter
{
    /// <summary>
    ///     Class DecimalConverter.
    /// </summary>
    public class DecimalConverter : ITypeConverter
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

            return System.Convert.ToDecimal(sourceValue);
        }
    }
}