using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Lab.LineBot.SDK.Internals;
using Lab.LineBot.SDK.Models;

namespace Lab.LineBot.SDK
{
    public class LineNotifyProvider : ILineNotifyProvider
    {
        private static readonly string OAuth2Endpoint = "https://notify-bot.line.me/";
        private static readonly string ApiEndpoint    = "https://notify-api.line.me/";

        private static readonly Lazy<SocketsHttpHandler> s_oauthSocketsHandlerLazy =
            new(() =>
                    new SocketsHttpHandler
                    {
                        PooledConnectionLifetime    = TimeSpan.FromMinutes(10),
                        PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
                        MaxConnectionsPerServer     = 10
                    });

        private static readonly Lazy<SocketsHttpHandler> s_apiSocketsHandlerLazy =
            new(() =>
                    new SocketsHttpHandler
                    {
                        PooledConnectionLifetime    = TimeSpan.FromMinutes(10),
                        PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
                        MaxConnectionsPerServer     = 10
                    });

        internal SocketsHttpHandler OAuth2SocketsHandler
        {
            get
            {
                if (this._oAuth2SocketsHandler == null)
                {
                    return s_oauthSocketsHandlerLazy.Value;
                }

                return this._oAuth2SocketsHandler;
            }
            set => this._oAuth2SocketsHandler = value;
        }

        internal SocketsHttpHandler ApiSocketsHandler
        {
            get
            {
                if (this._apiSocketsHandler == null)
                {
                    return s_apiSocketsHandlerLazy.Value;
                }

                return this._apiSocketsHandler;
            }
            set => this._apiSocketsHandler = value;
        }

        public bool IsThrowInternalError { get; set; } = false;

        internal HttpClient OAuth2Client
        {
            get
            {
                if (this._oAuth2Client == null)
                {
                    return new HttpClient(this.OAuth2SocketsHandler)
                    {
                        BaseAddress = new Uri(OAuth2Endpoint)
                    };
                }

                return this._oAuth2Client;
            }
            set => this._oAuth2Client = value;
        }

        internal HttpClient ApiClient
        {
            get
            {
                if (this._apiClient == null)
                {
                    return new HttpClient(this.ApiSocketsHandler)
                    {
                        BaseAddress = new Uri(OAuth2Endpoint)
                    };
                }

                return this._apiClient;
            }
            set => this._apiClient = value;
        }

        private HttpClient         _apiClient;
        private SocketsHttpHandler _apiSocketsHandler;
        private HttpClient         _oAuth2Client;
        private SocketsHttpHandler _oAuth2SocketsHandler;

        public async Task<GenericResponse> NotifyAsync(NotifyWithImageRequest request,
                                                       CancellationToken      cancelToken)
        {
            Validation.Validate(request);
            var       url             = $"api/notify?message={request.Message}";
            using var formDataContent = new MultipartFormDataContent();

            var imageName    = Path.GetFileName(request.FilePath);
            var mimeType     = MimeTypeMapping.GetMimeType(imageName);
            var imageContent = new ByteArrayContent(request.FileBytes);
            imageContent.Headers.ContentType = new MediaTypeHeaderValue(mimeType);

            formDataContent.Add(imageContent, "imageFile", imageName);

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Headers = {Authorization = new AuthenticationHeaderValue("Bearer", request.AccessToken)},
                Content = formDataContent
            };

            var client   = this.ApiClient;
            var response = await client.SendAsync(httpRequest, cancelToken);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                if (this.IsThrowInternalError)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new LineNotifyProviderException(error);
                }
            }

            return await response.Content.ReadAsAsync<GenericResponse>(cancelToken);
        }

        public async Task<GenericResponse> NotifyAsync(NotifyWithStickerRequest request,
                                                       CancellationToken        cancelToken)
        {
            Validation.Validate(request);

            var url = "api/notify";
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Headers = {Authorization = new AuthenticationHeaderValue("Bearer", request.AccessToken)},
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    {"message", request.Message},
                    {"stickerPackageId", request.StickerPackageId},
                    {"stickerId", request.StickerId},
                }),
            };

            var client   = this.ApiClient;
            var response = await client.SendAsync(httpRequest, cancelToken);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                if (this.IsThrowInternalError)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new LineNotifyProviderException(error);
                }
            }

            return await response.Content.ReadAsAsync<GenericResponse>(cancelToken);
        }

        public async Task<TokenInfoResponse> GetAccessTokenInfoAsync(string            accessToken,
                                                                     CancellationToken cancelToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                throw new ArgumentNullException(nameof(accessToken));
            }

            var url = "api/status";
            var httpRequest = new HttpRequestMessage(HttpMethod.Get, url)
            {
                Headers = {Authorization = new AuthenticationHeaderValue("Bearer", accessToken)},
                Content = new FormUrlEncodedContent(new Dictionary<string, string>()),
            };

            var client   = this.ApiClient;
            var response = await client.SendAsync(httpRequest, cancelToken);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                if (this.IsThrowInternalError)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new LineNotifyProviderException(error);
                }
            }

            var tokenInfo = await response.Content.ReadAsAsync<TokenInfoResponse>(cancelToken);
            tokenInfo.Limit          = GetValue<int>(response, "X-RateLimit-Limit");
            tokenInfo.ImageLimit     = GetValue<int>(response, "X-RateLimit-ImageLimit");
            tokenInfo.Remaining      = GetValue<int>(response, "X-RateLimit-Remaining");
            tokenInfo.ImageRemaining = GetValue<int>(response, "X-RateLimit-ImageRemaining");
            tokenInfo.Reset          = GetValue<int>(response, "X-RateLimit-Reset");
            tokenInfo.ResetLocalTime = ToLocalTime(tokenInfo.Reset);
            return tokenInfo;
        }

        public string CreateAuthorizeCodeUrl(AuthorizeCodeUrlRequest request)
        {
            Validation.Validate(request);

            var url = "oauth/authorize";
            return $"{OAuth2Endpoint}"                    +
                   url                                    +
                   "?response_type=code"                  +
                   "&scope=notify"                        +
                   "&response_mode=form_post"             +
                   $"&client_id={request.ClientId}"       +
                   $"&redirect_uri={request.CallbackUrl}" +
                   $"&state={request.State}"
                ;
        }

        public async Task<TokenResponse> GetAccessTokenAsync(TokenRequest      request,
                                                             CancellationToken cancelToken)
        {
            Validation.Validate(request);

            var url = "oauth/token";

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                {"grant_type", "authorization_code"},
                {"code", request.Code},
                {"redirect_uri", request.CallbackUrl},
                {"client_id", request.ClientId},
                {"client_secret", request.ClientSecret},
            });

            var    client   = this.OAuth2Client;
            var    response = await client.PostAsync(url, content, cancelToken);
            string result   = null;

            if (response.StatusCode != HttpStatusCode.OK)
            {
                if (this.IsThrowInternalError)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new LineNotifyProviderException(error);
                }
            }

            return await response.Content.ReadAsAsync<TokenResponse>(cancelToken);
        }

        public async Task<GenericResponse> RevokeAsync(string accessToken, CancellationToken cancelToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                throw new ArgumentNullException(nameof(accessToken));
            }

            var url = "api/revoke";
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Headers = {Authorization = new AuthenticationHeaderValue("Bearer", accessToken)},
                Content = new FormUrlEncodedContent(new Dictionary<string, string>()),
            };

            var client   = this.ApiClient;
            var response = await client.SendAsync(httpRequest, cancelToken);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                if (this.IsThrowInternalError)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new LineNotifyProviderException(error);
                }
            }

            return await response.Content.ReadAsAsync<GenericResponse>(cancelToken);
        }

        private static T GetValue<T>(HttpResponseMessage response, string key)
        {
            var result = default(T);
            response.Headers.TryGetValues(key, out var values);
            if (values == null)
            {
                return result;
            }

            var content = values.FirstOrDefault();

            return (T) Convert.ChangeType(content, typeof(T));
        }

        private static DateTime ToLocalTime(long source)
        {
            var timeOffset = DateTimeOffset.FromUnixTimeSeconds(source);
            return timeOffset.DateTime.ToUniversalTime();
        }
    }
}