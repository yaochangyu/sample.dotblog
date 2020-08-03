using System;

/// <summary>
/// The TypeConverters namespace.
/// </summary>

namespace Lab.DynamicAccessor.TypeConverter
{
    /// <summary>
    ///     Class EnumConverter.
    /// </summary>
    public class EnumConverter : ITypeConverter
    {
        /// <summary>
        ///     Converts the specified enum type.
        /// </summary>
        /// <param name="type">Type of the enum.</param>
        /// <param name="sourceValue">The source value.</param>
        /// <returns>System.Object.</returns>
        /// <exception cref="System.InvalidOperationException">ERROR_TYPE_IS_NOT_ENUMERATION</exception>
        public object Convert(Type type, object sourceValue)
        {
            if (sourceValue == null || sourceValue == DBNull.Value)
            {
                return null;
            }

            if (type.IsNullableEnum() == false && type.IsEnum == false)
            {
                throw new InvalidOperationException("ERROR_TYPE_IS_NOT_ENUMERATION");
            }

            var nullableType = Nullable.GetUnderlyingType(type);
            if (nullableType != null)
            {
                return Enum.Parse(nullableType, sourceValue.ToString());

                //return System.Convert.ChangeType(Enum.Parse(nullableType, sourceValue.ToString()), nullableType);
            }

            return Enum.Parse(type, sourceValue.ToString());
        }

        /// <summary>
        ///     Converts the specified source value.
        /// </summary>
        /// <param name="sourceValue">The source value.</param>
        /// <returns>System.Object.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public object Convert(object sourceValue)
        {
            throw new NotImplementedException();
        }
    }
}