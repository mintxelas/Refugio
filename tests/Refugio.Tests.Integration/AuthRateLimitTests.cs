using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Refugio.Tests.Integration;

/// <summary>
/// Proves /api/auth/login is throttled per client IP. Overrides the permit limit down to a
/// small number (the base ShelterWebFactory raises it to 1000 so the rest of the suite,
/// which logs in many times per test class, doesn't trip it).
/// </summary>
public class AuthRateLimitTests
{
    [Fact]
    public async Task Login_ExceedsRateLimit_Returns429()
    {
        await using var factory = new ShelterWebFactory().WithWebHostBuilder(b =>
        {
            b.UseSetting("Auth:RateLimitPermitLimit", "3");
            b.UseSetting("Auth:RateLimitWindowSeconds", "60");
        });
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await ShelterWebFactory.AttachCsrfTokenAsync(client);

        for (var i = 0; i < 3; i++)
        {
            var response = await client.PostAsJsonAsync("/api/auth/login", new { email = "nobody@test.com", password = "wrong" });
            Assert.NotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
        }

        var throttled = await client.PostAsJsonAsync("/api/auth/login", new { email = "nobody@test.com", password = "wrong" });
        Assert.Equal(HttpStatusCode.TooManyRequests, throttled.StatusCode);
    }
}
