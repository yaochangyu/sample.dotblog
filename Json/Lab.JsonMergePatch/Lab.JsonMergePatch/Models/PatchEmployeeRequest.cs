using Swashbuckle.AspNetCore.Filters;

namespace Lab.JsonMergePatch.Models;

public class PatchEmployeeRequest
{
    public string? Name { get; set; }

    public Address? Address { get; set; }

    public DateTime? Birthday { get; set; }

    public List<string> Extensions { get; set; } = new();

    public class PatchEmployeeRequestExample : IExamplesProvider<PatchEmployeeRequest>
    {
        public PatchEmployeeRequest GetExamples()
        {
            return new PatchEmployeeRequest
            {
                Name = "小章",
                Address = new Address
                {
                    Address1 = "台北市",
                    Address2 = "大安區",
                    Street = "忠孝東路"
                },
                Birthday = DateTime.Today,
                Extensions = new List<string>() { "ex1", "ex2" }
            };
        }
    }
}