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
        public string A { get; set; }

        [ExcelColumnIndex("B")]
        public string B { get; set; }

        [ExcelColumnIndex("C")]
        public string C { get; set; }

        [ExcelColumnIndex("D")]
        public string D { get; set; }

        [ExcelColumnIndex("E")]
        public string E { get; set; }

        [ExcelColumnIndex("F")]
        public string F { get; set; }

        [ExcelColumnIndex("G")]
        public string G { get; set; }
    }

    [Fact]
    public async Task 讀取大檔案()
    {
        //import excel file
        var inputPath = "修改策略範例_20230518093153_10萬.xlsx";
        var chunkSize = 1024;

        await using var inputStream = File.OpenRead(inputPath);

        // await using var templateStream = File.OpenWrite(templatePath);
        // await using var outputStream = File.Create(outputPath);

        var inputRows = await inputStream.QueryAsync<Data>();
        var results = new List<Data>();
        foreach (var chunks in inputRows.Chunk(chunkSize))
        {
            results.AddRange(chunks);
        }
    }

    [Fact]
    public async Task 批次匯入後另存()
    {
        //import excel file
        var inputPath = "Import.xlsx";
        var outputPath = "Data.xlsx";
        var templatePath = "Template/Member.xlsx";
        var chunkSize = 2;

        await using var inputStream = File.OpenRead(inputPath);

        // await using var templateStream = File.OpenWrite(templatePath);
        // await using var outputStream = File.Create(outputPath);

        var inputRows = await inputStream.QueryAsync<Member>();
        var results = new List<Member>();
        foreach (var chunks in inputRows.Chunk(chunkSize))
        {
            foreach (var row in chunks)
            {
                if (row.Age > 30)
                {
                    row.Reason = "年齡超過30";
                }
            }

            results.AddRange(chunks);
        }

        var value = new
        {
            Members = results
        };
        
        
        //結果會被覆蓋
        await MiniExcel.SaveAsByTemplateAsync(outputPath, templatePath, value);
    }

    [Fact]
    public async Task 填充員工表()
    {
        var templatePath = "Template/Employee.xlsx";

        // var templatePath = "Template/ImportWithError.xltx";
        var outputPath = "data.xlsx";

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
    public async Task 填充會員表()
    {
        var templatePath = "Template/Member.xlsx";

        // var templatePath = "Template/ImportWithError.xltx";
        var outputPath = "data.xlsx";

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
}