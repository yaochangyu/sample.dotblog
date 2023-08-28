using Lab.ModelValidation.API.Controllers;

namespace Lab.ModelValidation.API.Models;

public class GetMemberResult
{
    public Guid Id { get; set; }

    public string Name { get; set; }

    public int Age { get; set; }
}