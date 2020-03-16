using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Client
{
    public class AuthorityCode
    {
        public int Code { get; set; }

        public string Name { get; set; }

        public static ICollection<AuthorityCode> ConvertTo<TSource>() where TSource : Enum
        {
            //ICollection<AuthorityCode> results = null;
            //return results;

            return Enum.GetNames(typeof(TSource))
                       .Cast<TSource>()
                       .Select(p => new AuthorityCode
                       {
                           Code = Convert.ToInt32(p),
                           Name = p.ToString()
                       })
                       .ToList()
                ;
        }
    }

    public static class EnumerationExtensions
    {
        public static TSource Add<TSource>(this TSource source, TSource value) where TSource : struct, IConvertible
        {
            if (!typeof(TSource).IsEnum)
            {
                throw new ArgumentException("T must be an enumerated type");
            }

            return (TSource) (object) (Convert.ToInt32(source) | Convert.ToInt32(value));
        }

        public static TSource Add1<TSource>(this TSource source, TSource value) where TSource : Enum
        {
            return (TSource) (object) (Convert.ToInt32(source) | Convert.ToInt32(value));
        }

        public static bool Has<TSource>(this TSource source, TSource value) where TSource : struct, IConvertible
        {
            if (!typeof(TSource).IsEnum)
            {
                throw new ArgumentException("T must be an enumerated type");
            }

            return (Convert.ToInt32(source) & Convert.ToInt32(value)) == Convert.ToInt32(value);
        }

        public static bool Has1<TSource>(this TSource source, TSource value) where TSource : Enum
        {
            return (Convert.ToInt32(source) & Convert.ToInt32(value)) == Convert.ToInt32(value);
        }

        public static TSource Remove<TSource>(this TSource source, TSource value) where TSource : struct, IConvertible
        {
            if (!typeof(TSource).IsEnum)
            {
                throw new ArgumentException("T must be an enumerated type");
            }

            return (TSource) (object) (Convert.ToInt32(source) & ~Convert.ToInt32(value));
        }

        public static TSource Remove1<TSource>(this TSource source, TSource value) where TSource : Enum
        {
            return (TSource) (object) (Convert.ToInt32(source) & ~Convert.ToInt32(value));
        }
    }

    [Flags]
    internal enum EnumPermissionType
    {
        None    = 0,
        Read    = 1,
        Add     = 2,
        Edit    = 4,
        Delete  = 8,
        Print   = 16,
        RunFlow = 32,
        Export  = 64,
        All     = Read | Add | Edit | Delete | Print | RunFlow
    }

    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void ConvertTo()
        {
            var authorityCodes = AuthorityCode.ConvertTo<EnumPermissionType>();
        }

        [TestMethod]
        public void HasAdd()
        {
            var fromDb    = 7;
            var operation = (EnumPermissionType) fromDb;
            var has       = operation.Has(EnumPermissionType.Add);
            Assert.AreEqual(true, has);
        }

        [TestMethod]
        public void HasRead()
        {
            var fromDb = "Add,Read";
            Enum.TryParse<EnumPermissionType>(fromDb, out var operation);
            var has = operation.Has(EnumPermissionType.Read);
            Assert.AreEqual(true, has);
        }

        [TestMethod]
        public void Remove()
        {
            var operation = EnumPermissionType.None
                                              .Add(EnumPermissionType.Add)
                                              .Add(EnumPermissionType.Read);
            var after = operation.Remove(EnumPermissionType.Read);
            var has   = after.Has(EnumPermissionType.Read);
            Assert.AreEqual(false, has);
        }
    }
}