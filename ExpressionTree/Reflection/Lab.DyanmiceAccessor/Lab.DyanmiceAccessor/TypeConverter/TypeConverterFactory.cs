using System;
using System.Collections.Generic;

namespace Lab.DynamicAccessor.TypeConverter
{
    /// <summary>
    ///     型別轉型工廠
    /// </summary>
    public class TypeConverterFactory
    {
        /// <summary>
        ///     The s_ container
        /// </summary>
        private static Dictionary<Type, ITypeConverter> s_container;

        /// <summary>
        ///     取得ITypeConverter物件
        /// </summary>
        /// <param name="type">The t.</param>
        /// <returns>ITypeConverter.</returns>
        public ITypeConverter GetTypeConverter(Type type)
        {
            if (s_container == null)
            {
                s_container = new Dictionary<Type, ITypeConverter>();
            }

            var sourceType = type;
            if (s_container.ContainsKey(sourceType))
            {
                return s_container[sourceType];
            }

            if (sourceType.IsNullableEnum() || sourceType.IsEnum)
            {
                s_container.Add(sourceType, new EnumConverter());
                return s_container[sourceType];
            }

            if (sourceType == typeof(int))
            {
                s_container.Add(sourceType, new IntegerConverter());
            }
            else if (sourceType == typeof(int?))
            {
                s_container.Add(sourceType, new IntegerConverter());
            }
            else if (sourceType == typeof(uint))
            {
                s_container.Add(sourceType, new UIntegerConverter());
            }
            else if (sourceType == typeof(uint?))
            {
                s_container.Add(sourceType, new UIntegerConverter());
            }
            else if (sourceType == typeof(long))
            {
                s_container.Add(sourceType, new LongConverter());
            }
            else if (sourceType == typeof(long?))
            {
                s_container.Add(sourceType, new LongConverter());
            }
            else if (sourceType == typeof(ulong))
            {
                s_container.Add(sourceType, new ULongConverter());
            }
            else if (sourceType == typeof(ulong?))
            {
                s_container.Add(sourceType, new ULongConverter());
            }
            else if (sourceType == typeof(short))
            {
                s_container.Add(sourceType, new ShortConverter());
            }
            else if (sourceType == typeof(short?))
            {
                s_container.Add(sourceType, new ShortConverter());
            }
            else if (sourceType == typeof(ushort))
            {
                s_container.Add(sourceType, new UShortConverter());
            }
            else if (sourceType == typeof(ushort?))
            {
                s_container.Add(sourceType, new UShortConverter());
            }
            else if (sourceType == typeof(float))
            {
                s_container.Add(sourceType, new FloatConverter());
            }
            else if (sourceType == typeof(float?))
            {
                s_container.Add(sourceType, new FloatConverter());
            }
            else if (sourceType == typeof(double))
            {
                s_container.Add(sourceType, new DoubleConverter());
            }
            else if (sourceType == typeof(double?))
            {
                s_container.Add(sourceType, new DoubleConverter());
            }
            else if (sourceType == typeof(decimal))
            {
                s_container.Add(sourceType, new DecimalConverter());
            }
            else if (sourceType == typeof(decimal?))
            {
                s_container.Add(sourceType, new DecimalConverter());
            }
            else if (sourceType == typeof(bool))
            {
                s_container.Add(sourceType, new BooleanConverter());
            }
            else if (sourceType == typeof(bool?))
            {
                s_container.Add(sourceType, new BooleanConverter());
            }
            else if (sourceType == typeof(char))
            {
                s_container.Add(sourceType, new CharConverter());
            }
            else if (sourceType == typeof(char?))
            {
                s_container.Add(sourceType, new CharConverter());
            }
            else if (sourceType == typeof(string))
            {
                s_container.Add(sourceType, new StringConverter());
            }
            else if (sourceType == typeof(Guid))
            {
                s_container.Add(sourceType, new GuidConverter());
            }
            else if (sourceType == typeof(Guid?))
            {
                s_container.Add(sourceType, new GuidConverter());
            }
            else if (sourceType == typeof(DateTime))
            {
                s_container.Add(sourceType, new DateTimeConverter());
            }
            else if (sourceType == typeof(DateTime?))
            {
                s_container.Add(sourceType, new DateTimeConverter());
            }
            else if (sourceType == typeof(byte))
            {
                s_container.Add(sourceType, new ByteConverter());
            }
            else if (sourceType == typeof(byte?))
            {
                s_container.Add(sourceType, new ByteConverter());
            }
            else if (sourceType == typeof(sbyte))
            {
                s_container.Add(sourceType, new SByteConverter());
            }
            else if (sourceType == typeof(sbyte?))
            {
                s_container.Add(sourceType, new SByteConverter());
            }
            else
            {
                throw new NotSupportedException(nameof(type));
            }

            return s_container[sourceType];
        }

        /// <summary>
        ///     取得ITypeConverter物件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>ITypeConverter.</returns>
        public ITypeConverter GetTypeConverter<T>()
        {
            var result = this.GetTypeConverter(typeof(T));
            return result;
        }
    }
}