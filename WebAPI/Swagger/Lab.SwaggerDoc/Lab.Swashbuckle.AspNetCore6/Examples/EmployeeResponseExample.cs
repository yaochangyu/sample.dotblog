using Swashbuckle.AspNetCore.Filters;

namespace Lab.Swashbuckle.AspNetCore6.Examples;

public class EmployeeResponseExample : IExamplesProvider<EmployeeResponse>
{
    public EmployeeResponse GetExamples()
    {
        return new EmployeeResponse
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            Name = "小章",
            Age = 18,
            Remark = "說明"
        };
    }
}