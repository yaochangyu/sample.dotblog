using System.Threading;
using System.Threading.Tasks;
using Lab.LineBot.SDK.Models;

namespace Lab.LineBot.SDK
{
    public interface ILineNotifyProvider
    {
        string CreateAuthorizeCodeUrl(AuthorizeCodeUrlRequest request);

        Task<TokenResponse> GetAccessTokenAsync(TokenRequest request, CancellationToken cancelToken);

        Task<TokenInfoResponse> GetAccessTokenInfoAsync(string accessToken, CancellationToken cancelToken);

        Task<GenericResponse> NotifyAsync(NotifyWithStickerRequest request, CancellationToken cancelToken);

        Task<GenericResponse> NotifyAsync(NotifyWithImageRequest request, CancellationToken cancelToken);

        Task<GenericResponse> RevokeAsync(string accessToken, CancellationToken cancelToken);
    }
}