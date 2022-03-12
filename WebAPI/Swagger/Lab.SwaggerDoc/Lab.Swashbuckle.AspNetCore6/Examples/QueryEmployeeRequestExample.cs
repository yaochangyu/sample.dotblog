using Swashbuckle.AspNetCore.Filters;

namespace Lab.Swashbuckle.AspNetCore6.Examples;

public class QueryEmployeeRequestExample : IExamplesProvider<QueryEmployeeRequest>
{
    public QueryEmployeeRequest GetExamples()
    {
        return new QueryEmployeeRequest
        {
            Name = "小章",
            Age = 18,
            // State = (State)1
        };
    }
}