using System.ComponentModel.DataAnnotations;

namespace Lab.Swashbuckle.AspNetCore6;

public class QueryEmployeeRequest
{
    /// <summary>
    /// 姓名
    /// </summary>
    /// <example>小章</example>
    [Required]
    public string Name { get; set; }

    /// <summary>
    /// 年齡
    /// </summary>
    /// <example>18</example>
    public int Age { get; set; }
}