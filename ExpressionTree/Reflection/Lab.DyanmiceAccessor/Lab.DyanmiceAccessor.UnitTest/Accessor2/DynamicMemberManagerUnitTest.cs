using System;
using System.Reflection;
using Lab.DynamicAccessor.Accessor2;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.DynamicAccessor.UnitTest.Accessor2
{
    [TestClass]
    public class DynamicMemberManagerUnitTest
    {
        [TestMethod]
        public void 動態存取私有欄位()
        {
            var expected = DataLevel.Medium;
            var instance = new Data();
            var flags    = BindingFlags.NonPublic | BindingFlags.Instance;

            var fieldInfo = instance.GetType().GetField("Field", flags);
            var accessor  = DynamicMemberManager.Field.Get(fieldInfo);
            accessor.SetValue(instance, expected);
            var actual = (DataLevel) accessor.GetValue(instance);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void 動態存取屬性()
        {
            var expected      = DataLevel.Medium;
            var instance      = new Data();
            var propertyInfo1 = instance.GetType().GetProperty("Enum1");
            var propertyInfo2 = instance.GetType().GetProperty("Enum2");
            var accessor1     = DynamicMemberManager.Property.Get(propertyInfo1);
            var accessor2     = DynamicMemberManager.Property.Get(propertyInfo2);
            accessor1.SetValue(instance, expected);
            accessor2.SetValue(instance, expected);
            var actual1 = (DataLevel) accessor1.GetValue(instance);
            var actual2 = (DataLevel) accessor2.GetValue(instance);
            Assert.AreEqual(expected, actual1);
            Assert.AreEqual(expected, actual2);
        }

        [TestMethod]
        public void 動態建立私有建構子()
        {
            var type            = typeof(MyClass2);
            var flags           = BindingFlags.NonPublic | BindingFlags.Instance;
            var constructorInfo = type.GetConstructor(flags, null, new Type[0], null);
            var accessor        = DynamicMemberManager.Construct.Get(constructorInfo);
            accessor.Execute(null);
        }

        [TestMethod]
        public void 執行私有Sum方法()
        {
            var instance   = new MyClass();
            var flags      = BindingFlags.NonPublic | BindingFlags.Instance;
            var methodInfo = instance.GetType().GetMethod("Sum", flags);
            var accessor   = DynamicMemberManager.Method.Get(methodInfo);
            var result     = accessor.Execute(instance, 1, 1);
            Assert.AreEqual(2, result);
        }

        private class Data
        {
            private static readonly string guid = "19ADC6C6-570C-40E5-84CD-C8425ECB81D2";
            private static readonly string date = "1900/1/1";

            public bool Bool1 { get; set; }

            public bool? Bool2 { get; set; }

            public byte Byte1 { get; set; }

            public byte? Byte2 { get; set; }

            public char Char1 { get; set; }

            public char? Char2 { get; set; }

            public DateTime Date1 { get; set; }

            public DateTime? Date2 { get; set; }

            public decimal Decimal1 { get; set; }

            public decimal? Decimal2 { get; set; }

            public double Double1 { get; set; }

            public double? Double2 { get; set; }

            public DataLevel Enum1 { get; set; }

            public DataLevel? Enum2 { get; set; }

            public float Float1 { get; set; }

            public float? Float2 { get; set; }

            public Guid Guid1 { get; set; }

            public Guid? Guid2 { get; set; }

            public int Int1 { get; set; }

            public int? Int2 { get; set; }

            public long Long1 { get; set; }

            public long? Long2 { get; set; }

            public sbyte Sbyte1 { get; set; }

            public sbyte? Sbyte2 { get; set; }

            public short Short1 { get; set; }

            public short? Short2 { get; set; }

            public string String1 { get; set; }

            public uint UInt1 { get; set; }

            public uint? UInt2 { get; set; }

            public ulong ULong1 { get; set; }

            public ulong? ULong2 { get; set; }

            public ushort UShort1 { get; set; }

            public ushort? UShort2 { get; set; }

            private DataLevel? Field;

            public static Data CreateDefaultData()
            {
                var result = new Data
                {
                    Bool1    = true,
                    Bool2    = true,
                    Byte1    = 1,
                    Byte2    = 1,
                    Char1    = 'A',
                    Char2    = 'A',
                    Date1    = DateTime.Parse(date),
                    Date2    = DateTime.Parse(date),
                    Decimal1 = 1,
                    Decimal2 = 1,
                    Double1  = 1,
                    Double2  = 1,
                    Enum1    = (DataLevel) 7,
                    Enum2    = (DataLevel) 7,
                    Float1   = 1,
                    Float2   = 1,
                    Guid1    = Guid.Parse(guid),
                    Guid2    = Guid.Parse(guid),
                    Int1     = 1,
                    Int2     = 1,
                    Long1    = 1,
                    Long2    = 1,
                    Sbyte1   = 1,
                    Sbyte2   = 1,
                    Short1   = 1,
                    Short2   = 1,
                    String1  = "String1",
                    UInt1    = 1,
                    UInt2    = 1,
                    ULong1   = 1,
                    ULong2   = 1,
                    UShort1  = 1,
                    UShort2  = 1
                };
                return result;
            }
        }

        [Flags]
        private enum DataLevel
        {
            None   = 0,
            Low    = 1,
            Medium = 2,
            High   = 4
        }

        private class MyClass2
        {
            public string Data { get; }

            private MyClass2()
            {
                this.Data = "private construct";
            }
        }

        private class MyClass
        {
            private int Sum(int p1, int p2)
            {
                return p1 + p2;
            }
        }
    }
}