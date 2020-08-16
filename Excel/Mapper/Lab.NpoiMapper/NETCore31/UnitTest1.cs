using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npoi.Mapper;

namespace NETCore31
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void 讀取檔案所有的工作表()
        {
            var mapper         = new Mapper("Input.xlsx");
            var numberOfSheets = mapper.Workbook.NumberOfSheets;
            for (var i = 0; i < numberOfSheets; i++)
            {
                var employees = mapper.Take<Employee>().ToList();
                foreach (var employee in employees)
                {
                    Console.WriteLine(employee.ErrorColumnIndex);
                    Console.WriteLine(employee.ErrorMessage);
                }
            }

            foreach (KeyValuePair<string, Dictionary<int, object>> item1 in mapper.Objects)
            {
                foreach (KeyValuePair<int, object> item2 in item1.Value)
                {
                    
                }
            }
        }

        [TestMethod]
        public void 讀取檔案轉dynamic()
        {
            var mapper    = new Mapper("Input.xlsx");
            var employees = mapper.Take<dynamic>();
        }

        [TestMethod]
        public void 讀取檔案轉強型別()
        {
            var mapper    = new Mapper("Input.xlsx");
            var employees = mapper.Take<Employee>().ToList();
            foreach (var employee in employees)
            {
                Console.WriteLine(employee.ErrorColumnIndex);
                Console.WriteLine(employee.ErrorMessage);
            }
        }
    }
}