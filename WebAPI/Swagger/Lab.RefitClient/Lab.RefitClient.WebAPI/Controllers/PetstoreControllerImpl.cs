using Microsoft.AspNetCore.Mvc;

namespace Lab.RefitClient.WebAPI.Controllers;

public class PetStoreControllerImpl : IPetStoreController
{
    private readonly IContextGetter<HeaderContext> _contextGetter;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PetStoreControllerImpl(IContextGetter<HeaderContext> contextGetter, 
        IHttpContextAccessor httpContextAccessor)
    {
        this._contextGetter = contextGetter;
        this._httpContextAccessor = httpContextAccessor;
    }

    public Task<ActionResult<Pet>> UpdatePetAsync(Pet body, CancellationToken cancellationToken = default(CancellationToken))
    {
        throw new NotImplementedException();
    }

    public Task<ActionResult<Pet>> AddPetAsync(Pet body, CancellationToken cancellationToken = default(CancellationToken))
    {
        throw new NotImplementedException();
    }

    public Task<ActionResult<ICollection<Pet>>> FindPetsByStatusAsync(Status status, CancellationToken cancellationToken = default(CancellationToken))
    {
        throw new NotImplementedException();
    }

    public Task<ActionResult<ICollection<Pet>>> FindPetsByTagsAsync(IEnumerable<string> tags, CancellationToken cancellationToken = default(CancellationToken))
    {
        throw new NotImplementedException();
    }

    public Task<ActionResult<Pet>> GetPetByIdAsync(long petId, CancellationToken cancellationToken = default(CancellationToken))
    {
        throw new NotImplementedException();
    }

    public Task<IActionResult> UpdatePetWithFormAsync(long petId, string name, string status,
        CancellationToken cancellationToken = default(CancellationToken))
    {
        throw new NotImplementedException();
    }

    public Task<IActionResult> DeletePetAsync(string api_key, long petId, CancellationToken cancellationToken = default(CancellationToken))
    {
        throw new NotImplementedException();
    }

    public Task<ActionResult<ApiResponse>> UploadFileAsync(long petId, string additionalMetadata, IFormFile body,
        CancellationToken cancellationToken = default(CancellationToken))
    {
        throw new NotImplementedException();
    }

    public Task<ActionResult<IDictionary<string, int>>> GetInventoryAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
        throw new NotImplementedException();
    }

    public Task<ActionResult<Order>> PlaceOrderAsync(Order body, CancellationToken cancellationToken = default(CancellationToken))
    {
        throw new NotImplementedException();
    }

    public Task<ActionResult<Order>> GetOrderByIdAsync(long orderId, CancellationToken cancellationToken = default(CancellationToken))
    {
        throw new NotImplementedException();
    }

    public Task<IActionResult> DeleteOrderAsync(long orderId, CancellationToken cancellationToken = default(CancellationToken))
    {
        throw new NotImplementedException();
    }

    public Task<ActionResult<User>> CreateUserAsync(User body, CancellationToken cancellationToken = default(CancellationToken))
    {
        throw new NotImplementedException();
    }

    public Task<ActionResult<User>> CreateUsersWithListInputAsync(IEnumerable<User> body, CancellationToken cancellationToken = default(CancellationToken))
    {
        throw new NotImplementedException();
    }

    public Task<ActionResult<string>> LoginUserAsync(string username, string password, CancellationToken cancellationToken = default(CancellationToken))
    {
        throw new NotImplementedException();
    }

    public Task<IActionResult> LogoutUserAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
        throw new NotImplementedException();
    }

    public async Task<ActionResult<User>> GetUserByNameAsync(string username, CancellationToken cancellationToken = default(CancellationToken))
    {
        // 透過一個 Filter 處理 Header 並轉呈 HeaderContext 物件
        var headerContext = this._contextGetter.Get();

        var response = this._httpContextAccessor.HttpContext.Response;
        response.Headers.Add(PetStoreHeaderNames.IdempotencyKey,headerContext.IdempotencyKey);
        response.Headers.Add(PetStoreHeaderNames.ApiKey,headerContext.ApiKey);
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

    public Task<IActionResult> UpdateUserAsync(string username, User body, CancellationToken cancellationToken = default(CancellationToken))
    {
        throw new NotImplementedException();
    }

    public Task<IActionResult> DeleteUserAsync(string username, CancellationToken cancellationToken = default(CancellationToken))
    {
        throw new NotImplementedException();
    }
}