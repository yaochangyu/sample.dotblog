using System.Collections.Generic;
using System.Data;

namespace Lab.DynamicAccessor
{
    public static class DataTableExtensions
    {
        private static readonly PropertyAccessor s_propertyAccessor;

        static DataTableExtensions()
        {
            s_propertyAccessor = new PropertyAccessor();
        }

        public static List<T> ToList<T>(this DataTable source) where T : class, new()
        {
            var targets = new List<T>();
            var columns = source.Columns;

            foreach (var row in source.AsEnumerable())
            {
                var targetInstance = new T();

                //var targetInstance     = Activator.CreateInstance<T>();
                var targetType = targetInstance.GetType();
                foreach (var propertyInfo in targetType.GetProperties())
                {
                    var propertyName = propertyInfo.Name;
                    var hasColumn    = columns.Contains(propertyName);
                    if (hasColumn == false)
                    {
                        continue;
                    }

                    var sourceValue = row[propertyName];
                    s_propertyAccessor.SetValue(targetInstance, propertyInfo, sourceValue);
                }

                targets.Add(targetInstance);
            }

            return targets;
        }

        //private static void NewMethod<T>(PropertyInfo propertyInfo, object sourceValue, T targetInstance, string propertyName)
        //    where T : class, new()
        //{
        //    var    propertyType  = propertyInfo.PropertyType;
        //    var    typeConverter = s_typeConverterFactory.GetTypeConverter(propertyType);
        //    object targetValue;

        //    if (propertyType.IsEnum)
        //    {
        //        var enumConverter = (EnumConverter) typeConverter;
        //        targetValue = enumConverter.Convert(propertyType, sourceValue);
        //    }
        //    else
        //    {
        //        targetValue = typeConverter.Convert(sourceValue);
        //    }

        //    //property.SetValue(targetInstance, targetValue);// 反射reflection
        //    s_memberAccessor.SetValue(targetInstance, propertyName, targetValue); //Expression Tree
        //}
    }
}