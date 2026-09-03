public class ApiKeyHandler : DelegatingHandler
{
    private readonly string _key;
    public ApiKeyHandler(IConfiguration config) => _key = config["ServiceUrls:ApiKey"] ?? "";

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        request.Headers.Add("X-Api-Key", _key);
        return base.SendAsync(request, ct);
    }
}