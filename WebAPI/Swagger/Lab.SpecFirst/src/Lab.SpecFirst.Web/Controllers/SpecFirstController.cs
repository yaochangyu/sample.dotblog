namespace Lab.SpecFirst.Web.Controllers;

class SpecFirstController : ISpecFirstContractController
{
    public async Task<ICollection<Pet>> ListPetsAsync(int? limit)
    {
        return new List<Pet>()
        {
            new()
            {
                Id = 1,
                Name = "yao",
                Tag = "dog",
                AdditionalProperties = new Dictionary<string, object>()
                {
                }
            },
        };
    }

    public async Task CreatePetsAsync()
    {
        
    }

    public async Task<Pet> ShowPetByIdAsync(string petId)
    {
        return new()
        {
            Id = 1,
            Name = "yao",
            Tag = "dog",
            AdditionalProperties = new Dictionary<string, object>()
            {
            }
        };
    }
}