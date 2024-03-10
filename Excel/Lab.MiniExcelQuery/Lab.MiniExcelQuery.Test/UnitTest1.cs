using System.ComponentModel;
using MiniExcelLibs;
using MiniExcelLibs.Attributes;
using MiniExcelLibs.OpenXml;

namespace Lab.MiniExcelQuery.Test;

public class UnitTest1
{
    class Member
    {
        [ExcelColumnName(excelColumnName: "編號", aliases: ["MemberId"])]
        public string Id { get; set; }

        [ExcelColumnName(excelColumnName: "姓名", aliases: ["FullName"])]
        public string Name { get; set; }

        [ExcelColumnName(excelColumnName: "生日")]
        public DateTime Birthday { get; set; }

        // [ExcelColumnName(excelColumnName: "年齡", aliases: ["Age"])]
        [DisplayName("年齡")]
        public int Age { get; set; }

        [DisplayName("電話")]

        public string Phone { get; set; }

        [DisplayName("失敗原因")]
        public string Reason { get; set; }
    }

    class Data
    {
        [ExcelColumnIndex("A")]
        [ExcelColumnName("商品名稱")]
        public string SaleName { get; set; }

        [ExcelColumnIndex("B")]
        [ExcelColumnName("規格")]
        public string Option { get; set; }

        [ExcelColumnIndex("C")]
        [ExcelColumnName("編號")]
        public string Id { get; set; }

        [ExcelColumnIndex("D")]
        [ExcelColumnName("Code")]
        public string Code { get; set; }

        [ExcelColumnIndex("E")]
        [ExcelColumnName("庫存")]
        public string StockQty { get; set; }
    }

    [Fact]
    public async Task 批次匯入大檔案()
    {
        //import large excel file
        var inputPath = "10萬.xlsx";
        var chunkSize = 1024;

        await using var inputStream = File.OpenRead(inputPath);
        var results = new List<Data>();

        // chunk read the data
        // var inputRows = await MiniExcel.QueryAsync<Data>(inputPath);
        var inputRows = await inputStream.QueryAsync<Data>();
        foreach (var chunks in inputRows.Chunk(chunkSize))
        {
            results.AddRange(chunks);
        }

        // var lookup = new List<IDictionary<string, object>>();
        // foreach (IDictionary<string, object> row in await MiniExcel.QueryAsync(inputPath))
        // {
        //     lookup.Add(row);
        // }
    }

    [Fact]
    public async Task 批次匯入後批次填充範本()
    {
        //import excel file
        var inputPath = "Import.xlsx";
        var outputPath = "MemberResult.xlsx";
        var templatePath = "Template/Member.xlsx";
        var chunkSize = 2;

        await using var inputStream = File.OpenRead(inputPath);
        var inputRows = await inputStream.QueryAsync<Member>();
        var results = new List<Member>();
        foreach (var chunks in inputRows.Chunk(chunkSize))
        {
            foreach (var row in chunks)
            {
                var age = (DateTime.Now - row.Birthday).TotalDays / 365.25;
                if (age > 30)
                {
                    row.Reason = "年齡超過30";
                }

                row.Age = (int)age;
            }

            results.AddRange(chunks);
            var value = new
            {
                Members = results
            };

            //Append to the same file
            await MiniExcel.SaveAsByTemplateAsync(outputPath, templatePath, value);
        }
    }

    [Fact]
    public async Task 匿名型別填充員工表()
    {
        var templatePath = "Template/Employee.xlsx";

        // var templatePath = "Template/ImportWithError.xltx";
        var outputPath = "EmployeeResult.xlsx";
        var value = new
        {
            employees = new[]
            {
                new { name = "Jack", department = "HR" },
                new { name = "Lisa", department = "HR" },
                new { name = "John", department = "HR" },
                new { name = "Mike", department = "IT" },
                new { name = "Neo", department = "IT" },
                new { name = "Loan", department = "IT" }
            }
        };
        await MiniExcel.SaveAsByTemplateAsync(outputPath, templatePath, value);
    }

    [Fact]
    public async Task 強型別填充會員表()
    {
        var templatePath = "Template/Member.xlsx";

        // var templatePath = "Template/ImportWithError.xltx";
        var outputPath = "MemberResult.xlsx";

        var value = new
        {
            Members = new List<Member>()
            {
                new()
                {
                    Id = "1",
                    Name = "Alice",
                    Age = 25,
                    Phone = "1234567890",
                    Reason = null,
                },
                new()
                {
                    Id = "2",
                    Name = "Bob",
                    Age = 35,
                    Phone = "1234567890",
                    Reason = "年齡超過30",
                }
            }
        };
        await MiniExcel.SaveAsByTemplateAsync(outputPath, templatePath, value);
    }

    [Fact]
    public async Task 強型別另存會員表()
    {
        var outputPath = "MemberResult.xlsx";

        await using var outputStream = File.Open(outputPath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        var value = new List<Member>()
        {
            new()
            {
                Id = "1",
                Name = "Alice",
                Birthday = DateTime.Now,
                Age = 25,
                Phone = "1234567890",
                Reason = null,
            },
            new()
            {
                Id = "2",
                Name = "Bob",
                Birthday = DateTime.Now,
                Age = 35,
                Phone = "1234567890",
                Reason = "年齡超過30",
            }
        };

        // await MiniExcel.SaveAsAsync(outputPath, value, overwriteFile: true);
        await outputStream.SaveAsAsync(value);
    }
    
    [Fact]
    public async Task 產生大資料填充範本()
    {
        //import excel file
        var outputPath = "MemberResult.xlsx";
        var templatePath = "Template/Member.xlsx";
        var chunkSize = 128;
        
        //generate 10000000 member row
        var inputRows = Enumerable.Range(1, 600000).Select(x => new Member
        {
            Id = x.ToString(),
            Name = "Name" + x,
            Birthday = DateTime.Now,
            Phone = "1234567890"
        });
        var results = new List<Member>();
        foreach (var chunks in inputRows.Chunk(chunkSize))
        {
            foreach (var row in chunks)
            {
                var age = (DateTime.Now - row.Birthday).TotalDays / 365.25;
                if (age > 30)
                {
                    row.Reason = "年齡超過30";
                }

                row.Age = (int)age;
            }

            results.AddRange(chunks);
        }

        var value = new
        {
            Members = results
        };

        //Append to the same file
        MiniExcel.SaveAsByTemplate(outputPath, templatePath, value);
    }

}