using System;

/// <summary>
/// The TypeConverters namespace.
/// </summary>

namespace Lab.DynamicAccessor.TypeConverter
{
    /// <summary>
    ///     Class CharConverter.
    /// </summary>
    public class CharConverter : ITypeConverter
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

            return System.Convert.ToChar(sourceValue);
        }
    }
}