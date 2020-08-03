namespace Lab.DynamicAccessor.TypeConverter
{
    /// <summary>
    ///     Interface ITypeConverter
    /// </summary>
    public interface ITypeConverter
    {
        /// <summary>
        ///     Converts the specified source value.
        /// </summary>
        /// <param name="sourceValue">The source value.</param>
        /// <returns>System.Object.</returns>
        object Convert(object sourceValue);
    }
}