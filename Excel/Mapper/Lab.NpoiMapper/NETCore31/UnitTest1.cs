using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Npoi.Mapper;

namespace NETCore31
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void FluentMethod����()
        {
            var mapper = new Mapper("Input.xlsx");
            mapper.Map<Employee>("LocationID", o => o.LocationId)
                  .Map<Employee>(1, o => o.DepartmentId)
                  .Ignore<Employee>(o => o.ErrorMessage)
                  .Format<Employee>("yyyy/MM/dd", o => o.Birthdaty)
                ;
            var numberOfSheets = mapper.Workbook.NumberOfSheets;
            for (var i = 0; i < numberOfSheets; i++)
            {
                var rowInfos = mapper.Take<Employee>().ToList();
                foreach (var rowInfo in rowInfos)
                {
                    Console.WriteLine(rowInfo.ErrorColumnIndex);
                    Console.WriteLine(rowInfo.ErrorMessage);
                }
            }
        }

        [TestMethod]
        public void �ץX�u�@��()
        {
            var mapper = new Mapper();
            var employees = new List<Employee>
            {
                new Employee
                {
                    Id             = 1,
                    LocationId     = "A",
                    DepartmentId   = "S000",
                    DepartmentName = "�s�i��",
                    EmployeeId     = "S001",
                    Name           = "�E�p��",
                    DomainName     = "TEST",
                    Birthdaty      = new DateTime(1988, 9, 11)
                },
                new Employee
                {
                    Id             = 2,
                    LocationId     = "A",
                    DepartmentId   = "A000",
                    DepartmentName = "������",
                    EmployeeId     = "A001",
                    Name           = "�p����",
                    DomainName     = "TEST",
                    Birthdaty      = new DateTime(1976, 8, 22)
                },
            };
            mapper.Put(employees, "sheet1",overwrite:true);
            mapper.Save("Output.xlsx");
        }

        [TestMethod]
        public void Ū���Ҧ��u�@��()
        {
            var mapper         = new Mapper("Input.xlsx");
            var numberOfSheets = mapper.Workbook.NumberOfSheets;
            for (var i = 0; i < numberOfSheets; i++)
            {
                var rowInfos = mapper.Take<Employee>(i).ToList();
                foreach (var rowInfo in rowInfos)
                {
                    if (string.IsNullOrWhiteSpace(rowInfo.ErrorMessage) == false)
                    {
                        Console.WriteLine(rowInfo.ErrorColumnIndex);
                        Console.WriteLine(rowInfo.ErrorMessage);
                    }
                }
            }

            foreach (var sheetInfo in mapper.Objects)
            {
                var sheetName = sheetInfo.Key;
                foreach (var rowInfo in sheetInfo.Value)
                {
                    var rowIndex = rowInfo.Key;
                    var rowData  = rowInfo.Value as Employee;
                    if (rowData == null)
                    {
                        continue;
                    }

                    Console.WriteLine(JsonConvert.SerializeObject(new
                    {
                        SheetName = sheetName,
                        Index     = rowIndex,
                        Data      = rowData
                    }));
                }
            }
        }

        [TestMethod]
        public void Ū���S�w�u�@��()
        {
            var mapper   = new Mapper("Input.xlsx");
            var rowInfos = mapper.Take<Employee>("sheet1");
            foreach (var rowInfo in rowInfos)
            {
                if (string.IsNullOrWhiteSpace(rowInfo.ErrorMessage) == false)
                {
                    Console.WriteLine(rowInfo.ErrorColumnIndex);
                    Console.WriteLine(rowInfo.ErrorMessage);
                }

                Console.WriteLine(JsonConvert.SerializeObject(new
                {
                    Index = rowInfo.RowNumber,
                    Data  = rowInfo.Value
                }));
            }
        }

        [TestMethod]
        public void Ū���ɮ���dynamic()
        {
            var mapper   = new Mapper("Input.xlsx");
            var rowInfos = mapper.Take<dynamic>().ToList();
            foreach (var rowInfo in rowInfos)
            {
                var rowData = rowInfo.Value;
                Console.WriteLine(JsonConvert.SerializeObject(rowData));
            }
        }

        [TestMethod]
        public void Ū���d���ɫ�t�s�s��()
        {
            var inputStream = File.Open("Template.xlsx", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            // var mapper = new Mapper("Template.xlsx");
            var mapper = new Mapper(inputStream);
            var employees = new List<Employee>
            {
                new Employee
                {
                    Id = 1,
                    LocationId = "A",
                    DepartmentId = "S000",
                    DepartmentName = "�s�i��",
                    EmployeeId = "S001",
                    Name = "�E�p��",
                    DomainName = "TEST",
                    Birthdaty = new DateTime(1988, 9, 11),
                    ErrorMessage = null
                },
                new Employee
                {
                    Id = 2,
                    LocationId = "A",
                    DepartmentId = "A000",
                    DepartmentName = "������",
                    EmployeeId = "A001",
                    Name = "�p����",
                    DomainName = "TEST",
                    Birthdaty = new DateTime(1976, 8, 22),
                    ErrorMessage = "�ڿ��F"
                },
            };

            mapper.Put(employees, overwrite: true);
            // mapper.Save("Output.xlsx");
            var outputStream = File.Open("Output.xlsx", FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
            mapper.Save(outputStream);
        }
    }
}