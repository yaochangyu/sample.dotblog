namespace Lab.RefitClient.WebAPI.Controllers;

public class PetStoreControllerImpl : IPetStoreController
{
    public Task<Pet> UpdatePetAsync(Pet body)
    {
        throw new NotImplementedException();
    }

    public Task<Pet> AddPetAsync(Pet body)
    {
        throw new NotImplementedException();
    }

    public Task<ICollection<Pet>> FindPetsByStatusAsync(Status status)
    {
        throw new NotImplementedException();
    }

    public Task<ICollection<Pet>> FindPetsByTagsAsync(IEnumerable<string> tags)
    {
        throw new NotImplementedException();
    }

    public Task<Pet> GetPetByIdAsync(long petId)
    {
        throw new NotImplementedException();
    }

    public Task UpdatePetWithFormAsync(long petId, string name, string status)
    {
        throw new NotImplementedException();
    }

    public Task DeletePetAsync(string api_key, long petId)
    {
        throw new NotImplementedException();
    }

    public Task<ApiResponse> UploadFileAsync(long petId, string additionalMetadata, IFormFile body)
    {
        throw new NotImplementedException();
    }

    public Task<IDictionary<string, int>> GetInventoryAsync()
    {
        throw new NotImplementedException();
    }

    public Task<Order> PlaceOrderAsync(Order body)
    {
        throw new NotImplementedException();
    }

    public Task<Order> GetOrderByIdAsync(long orderId)
    {
        throw new NotImplementedException();
    }

    public Task DeleteOrderAsync(long orderId)
    {
        throw new NotImplementedException();
    }

    public Task<User> CreateUserAsync(User body)
    {
        throw new NotImplementedException();
    }

    public Task<User> CreateUsersWithListInputAsync(IEnumerable<User> body)
    {
        throw new NotImplementedException();
    }

    public Task<string> LoginUserAsync(string username, string password)
    {
        throw new NotImplementedException();
    }

    public Task LogoutUserAsync()
    {
        throw new NotImplementedException();
    }

    public async Task<User> GetUserByNameAsync(string username)
    {
        return new User
        {
            Id = 0,
            Username = username,
            FirstName = null,
            LastName = null,
            Email = "yao@aa.bb",
            Password = null,
            Phone = null,
            UserStatus = 0,
            AdditionalProperties = null
        };
    }

    public Task UpdateUserAsync(string username, User body)
    {
        throw new NotImplementedException();
    }

    public Task DeleteUserAsync(string username)
    {
        throw new NotImplementedException();
    }
}