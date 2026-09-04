using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Refugio.Tests.Integration;

/// <summary>
/// The Pi deployment sits behind a reverse proxy that terminates TLS; Kestrel only ever sees
/// plain HTTP. Proves the app trusts X-Forwarded-Proto so redirects (the cookie-auth login
/// redirect in particular) are built with scheme "https", not "http" — an "http" redirect on an
/// https:// page is blocked by browsers as mixed content.
/// </summary>
public class ForwardedHeadersTests : IClassFixture<ShelterWebFactory>
{
    private readonly ShelterWebFactory _factory;

    public ForwardedHeadersTests(ShelterWebFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task LoginRedirect_HonorsForwardedProto_BuildsHttpsLocation()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Add("X-Forwarded-Proto", "https");
        request.Headers.Add("X-Forwarded-For", "203.0.113.1");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("https://", response.Headers.Location!.ToString());
    }
}
