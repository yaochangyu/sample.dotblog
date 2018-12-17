using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Linq;
using System.Reflection;

namespace UnitTestProject1.EntityModel
{
    internal class ValidationDbContext : DbContext
    {
        private static readonly ConcurrentDictionary<Type, IEnumerable<PropertyInfo>>
            s_datePropertyInfo = new ConcurrentDictionary<Type, IEnumerable<PropertyInfo>>();

        public DbSet<Member> Members { get; set; }

        public DbSet<MemberLog> MemberLogs { get; set; }

        //先執行基本驗證再執行自訂驗證
        protected override DbEntityValidationResult ValidateEntity(DbEntityEntry entityEntry,
                                                                   IDictionary<object, object> items)
        {
            var result = base.ValidateEntity(entityEntry, items);
            if (result.IsValid)
            {
                this.ValidateMinDateTime(result);
            }

            return result;
        }

        ////兩者一起執行
        //protected override DbEntityValidationResult ValidateEntity(DbEntityEntry entityEntry,
        //                                                           IDictionary<object, object> items)
        //{
        //    var result = new DbEntityValidationResult(entityEntry, new List<DbValidationError>());
        //    this.ValidateMinDateTime(result);
        //    base.ValidateEntity(entityEntry, items);
        //    return result;
        //}

        ////先執行自訂驗證再執行基本驗證
        //protected override DbEntityValidationResult ValidateEntity(DbEntityEntry entityEntry,
        //                                                           IDictionary<object, object> items)
        //{
        //    var result = new DbEntityValidationResult(entityEntry, new List<DbValidationError>());
        //    this.ValidateMinDateTime(result);
        //    if (!result.IsValid)
        //    {
        //        return result;
        //    }
        //    //return result;

        //    return base.ValidateEntity(entityEntry, items);
        //}

        private void ValidateMinDateTime(DbEntityValidationResult result)
        {
            var entityEntry = result.Entry;
            var entityType = entityEntry.Entity.GetType();
            var datePropertyInfos = GetDatePropertyInfos(entityType);
            foreach (var datePropertyInfo in datePropertyInfos)
            {
                var name = datePropertyInfo.Name;
                var value = (DateTime) datePropertyInfo.GetValue(entityEntry.Entity, null);
                if (value == DateTime.MinValue)
                {
                    result.ValidationErrors.Add(new DbValidationError(name, $"Not support {value} data"));
                }
            }
        }

        private static IEnumerable<PropertyInfo> GetDatePropertyInfos(Type entityType)
        {
            List<PropertyInfo> results = null;

            if (s_datePropertyInfo.ContainsKey(entityType))
            {
                results = s_datePropertyInfo[entityType].ToList();
            }
            else
            {
                results = new List<PropertyInfo>();
                var propertyInfos = entityType.GetProperties();

                foreach (var propertyInfo in propertyInfos)
                {
                    if (IsDateTime(propertyInfo.PropertyType))
                    {
                        results.Add(propertyInfo);
                    }
                }

                s_datePropertyInfo.TryAdd(entityType, results.ToArray());
            }

            return results;
        }

        private static bool IsDateTime(Type sourceType)
        {
            var result = false;
            result = sourceType == typeof(DateTime) || sourceType == typeof(DateTime?);
            return result;
        }
    }
}