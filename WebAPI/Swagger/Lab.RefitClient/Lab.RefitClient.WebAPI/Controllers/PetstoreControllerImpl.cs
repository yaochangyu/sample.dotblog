namespace Lab.RefitClient.WebAPI.Controllers;

public class PetStoreControllerImpl : IPetStoreController
{
    private IHttpContextAccessor _httpContextAccessor;

    private HttpContext _httpContent;

    public PetStoreControllerImpl(IHttpContextAccessor httpContextAccessor)
    {
        this._httpContextAccessor = httpContextAccessor;
    }

    // public PetStoreControllerImpl(HttpContextAccessor httpContextAccessor)
    // {
    //     this._httpContextAccessor = httpContextAccessor;
    // }

    public Task<Pet> UpdatePetAsync(Pet body, CancellationToken cancellationToken = default(CancellationToken))
    {
        throw new NotImplementedException();
    }

    public Task<Pet> AddPetAsync(Pet body, CancellationToken cancellationToken = default(CancellationToken))
    {
        throw new NotImplementedException();
    }

    public Task<ICollection<Pet>> FindPetsByStatusAsync(Status status, CancellationToken cancellationToken = default(CancellationToken))
    {
        throw new NotImplementedException();
    }

    public Task<ICollection<Pet>> FindPetsByTagsAsync(IEnumerable<string> tags, CancellationToken cancellationToken = default(CancellationToken))
    {
        throw new NotImplementedException();
    }

    public Task<Pet> GetPetByIdAsync(long petId, CancellationToken cancellationToken = default(CancellationToken))
    {
        throw new NotImplementedException();
    }

    public Task UpdatePetWithFormAsync(long petId, string name, string status,
        CancellationToken cancellationToken = default(CancellationToken))
    {
        throw new NotImplementedException();
    }

    public Task DeletePetAsync(string api_key, long petId, CancellationToken cancellationToken = default(CancellationToken))
    {
        throw new NotImplementedException();
    }

    public Task<ApiResponse> UploadFileAsync(long petId, string additionalMetadata, IFormFile body,
        CancellationToken cancellationToken = default(CancellationToken))
    {
        throw new NotImplementedException();
    }

    public Task<IDictionary<string, int>> GetInventoryAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
        throw new NotImplementedException();
    }

    public Task<Order> PlaceOrderAsync(Order body, CancellationToken cancellationToken = default(CancellationToken))
    {
        throw new NotImplementedException();
    }

    public Task<Order> GetOrderByIdAsync(long orderId, CancellationToken cancellationToken = default(CancellationToken))
    {
        throw new NotImplementedException();
    }

    public Task DeleteOrderAsync(long orderId, CancellationToken cancellationToken = default(CancellationToken))
    {
        throw new NotImplementedException();
    }

    public Task<User> CreateUserAsync(User body, CancellationToken cancellationToken = default(CancellationToken))
    {
        throw new NotImplementedException();
    }

    public Task<User> CreateUsersWithListInputAsync(IEnumerable<User> body, CancellationToken cancellationToken = default(CancellationToken))
    {
        throw new NotImplementedException();
    }

    public Task<string> LoginUserAsync(string username, string password, CancellationToken cancellationToken = default(CancellationToken))
    {
        throw new NotImplementedException();
    }

    public Task LogoutUserAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
        throw new NotImplementedException();
    }

    public Task<User> GetUserByNameAsync(string username, string x_api_key,
        CancellationToken cancellationToken = default(CancellationToken))
    {
        throw new NotImplementedException();
    }

    public async Task<User> GetUserByNameAsync(string username, CancellationToken cancellationToken = default(CancellationToken))
    {
        var headers = this._httpContextAccessor.HttpContext.Request.Headers;
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

    public Task UpdateUserAsync(string username, User body, CancellationToken cancellationToken = default(CancellationToken))
    {
        throw new NotImplementedException();
    }

    public Task DeleteUserAsync(string username, CancellationToken cancellationToken = default(CancellationToken))
    {
        throw new NotImplementedException();
    }
}