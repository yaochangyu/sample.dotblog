using System;
using System.Collections.Generic;
using System.Data;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.DynamicAccessor.UnitTest
{
    [TestClass]
    public class PropertyAccessorUnitTest
    {
        [TestMethod]
        public void 動態存取屬性()
        {
            var expected     = DataLevel.Medium;
            var instance     = new Data();
            var propertyInfo = instance.GetType().GetProperty("Enum2");
            var accessor     = new PropertyAccessor(propertyInfo);
            accessor.SetValue(instance, expected);
            var actual = (DataLevel) accessor.GetValue(instance);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void 動態存取屬性1()
        {
            var expected      = DataLevel.Medium;
            var instance      = new Data();
            var propertyInfo1 = instance.GetType().GetProperty("Enum1");
            var propertyInfo2 = instance.GetType().GetProperty("Enum2");
            var accessor1     = DynamicMemberManager.Property.GetOrCreate(propertyInfo1);
            var accessor2     = DynamicMemberManager.Property.GetOrCreate(propertyInfo2);
            accessor1.SetValue(instance, expected);
            accessor2.SetValue(instance, expected);
            var actual1 = (DataLevel) accessor1.GetValue(instance);
            var actual2 = (DataLevel) accessor2.GetValue(instance);
            Assert.AreEqual(expected, actual1);
            Assert.AreEqual(expected, actual2);
        }
        [TestMethod]
        public void DataTable轉集合()
        {
            var expected = new List<Data> {Data.CreateDefaultData()};
            var source   = CreateDataTable();
            var actual   = source.ToList<Data>();
            actual.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public void Model轉換()
        {
            var expected = Data.CreateDefaultData();
            var actual   = new Data();
            foreach (var propertyInfo in expected.GetType().GetProperties())
            {
                var accessor = new PropertyAccessor(propertyInfo);
                var value    = accessor.GetValue(expected);
                accessor.SetValue(actual, value);
            }

            actual.Should().BeEquivalentTo(expected);
        }

        private static DataTable CreateDataTable()
        {
            var result = new DataTable("table1");
            result.Columns.Add(new DataColumn("Bool1"));
            result.Columns.Add(new DataColumn("Bool2"));
            result.Columns.Add(new DataColumn("Byte1"));
            result.Columns.Add(new DataColumn("Byte2"));
            result.Columns.Add(new DataColumn("Char1"));
            result.Columns.Add(new DataColumn("Char2"));
            result.Columns.Add(new DataColumn("Date1"));
            result.Columns.Add(new DataColumn("Date2"));
            result.Columns.Add(new DataColumn("Decimal1"));
            result.Columns.Add(new DataColumn("Decimal2"));
            result.Columns.Add(new DataColumn("Double1"));
            result.Columns.Add(new DataColumn("Double2"));
            result.Columns.Add(new DataColumn("Enum1"));
            result.Columns.Add(new DataColumn("Enum2"));
            result.Columns.Add(new DataColumn("Float1"));
            result.Columns.Add(new DataColumn("Float2"));
            result.Columns.Add(new DataColumn("Guid1"));
            result.Columns.Add(new DataColumn("Guid2"));
            result.Columns.Add(new DataColumn("Int1"));
            result.Columns.Add(new DataColumn("Int2"));
            result.Columns.Add(new DataColumn("Long1"));
            result.Columns.Add(new DataColumn("Long2"));
            result.Columns.Add(new DataColumn("SByte1"));
            result.Columns.Add(new DataColumn("Sbyte2"));
            result.Columns.Add(new DataColumn("Short1"));
            result.Columns.Add(new DataColumn("Short2"));
            result.Columns.Add(new DataColumn("String1"));
            result.Columns.Add(new DataColumn("UInt1"));
            result.Columns.Add(new DataColumn("UInt2"));
            result.Columns.Add(new DataColumn("ULong1"));
            result.Columns.Add(new DataColumn("ULong2"));
            result.Columns.Add(new DataColumn("UShort1"));
            result.Columns.Add(new DataColumn("UShort2"));
            var row = result.NewRow();

            var id = "19ADC6C6-570C-40E5-84CD-C8425ECB81D2";

            row["Bool1"]    = true;
            row["Bool2"]    = (bool?) true;
            row["Byte1"]    = (byte) 1;
            row["Byte2"]    = (byte?) 1;
            row["Char1"]    = 'A';
            row["Char2"]    = (char?) 'A';
            row["Date1"]    = new DateTime(1900, 1, 1);
            row["Date2"]    = (DateTime?) new DateTime(1900, 1, 1);
            row["Decimal1"] = (decimal) 1;
            row["Decimal2"] = (decimal?) 1;
            row["Double1"]  = (double) 1;
            row["Double2"]  = (double?) 1;
            row["Enum1"]    = (DataLevel) 7;
            row["Enum2"]    = (DataLevel?) 7;
            row["Float1"]   = (float) 1;
            row["Float2"]   = (float?) 1;
            row["Guid1"]    = new Guid(id);
            row["Guid2"]    = (Guid?) new Guid(id);
            row["Int1"]     = 1;
            row["Int2"]     = 1;
            row["Long1"]    = (long) 1;
            row["Long2"]    = (long?) 1;
            row["SByte1"]   = (sbyte) 1;
            row["SByte2"]   = (sbyte?) 1;
            row["Short1"]   = (short) 1;
            row["Short2"]   = (short?) 1;
            row["String1"]  = "String1";
            row["UInt1"]    = (uint) 1;
            row["UInt2"]    = (uint?) 1;
            row["ULong1"]   = (ulong) 1;
            row["ULong2"]   = (ulong?) 1;
            row["UShort1"]  = (ushort) 1;
            row["UShort2"]  = (ushort?) 1;

            result.Rows.Add(row);
            return result;
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
        internal enum DataLevel
        {
            None   = 0,
            Low    = 1,
            Medium = 2,
            High   = 4
        }
    }
}