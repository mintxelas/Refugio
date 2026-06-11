namespace Refugio.Web.Services;

/// <summary>
/// Copies the incoming request's Cookie header onto outgoing API calls so the
/// signed-in user's auth cookie (and culture cookie) flow through the HTTP boundary.
/// The named client's primary handler must have UseCookies disabled, otherwise the
/// handler's cookie container would discard this manually-set header.
/// </summary>
public class ForwardCookieHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var cookie = httpContextAccessor.HttpContext?.Request.Headers.Cookie.ToString();
        if (!string.IsNullOrEmpty(cookie))
            request.Headers.TryAddWithoutValidation("Cookie", cookie);
        return base.SendAsync(request, cancellationToken);
    }
}
