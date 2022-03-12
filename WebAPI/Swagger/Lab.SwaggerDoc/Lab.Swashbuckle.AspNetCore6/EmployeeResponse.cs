namespace Lab.Swashbuckle.AspNetCore6;

public class EmployeeResponse
{
    /// <summary>
    ///     編號
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    ///     姓名
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    ///     年齡
    /// </summary>
    public int Age { get; set; }

    /// <summary>
    ///     註解
    /// </summary>
    public string Remark { get; set; }
}