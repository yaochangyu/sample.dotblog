namespace Lab.RefitClient;

public class DefaultHeaderHandler : DelegatingHandler
{
    private IContextGetter<HeaderContext> _contextGetter;

    public DefaultHeaderHandler(IContextGetter<HeaderContext> contextGetter)
    {
        this._contextGetter = contextGetter;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var headerContext = this._contextGetter.Get();
        request.Headers.Add(PetStoreHeaderNames.IdempotencyKey, headerContext.IdempotencyKey);
        request.Headers.Add(PetStoreHeaderNames.ApiKey, headerContext.ApiKey);

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}