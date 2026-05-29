using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Refugio.Tests.Integration;

public class AuthEndpointTests : IClassFixture<ShelterWebFactory>
{
    private readonly HttpClient _client;

    public AuthEndpointTests(ShelterWebFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    [Fact]
    public async Task UnauthenticatedRequest_ToProtectedPage_RedirectsToLogin()
    {
        var response = await _client.GetAsync("/");
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains("login", response.Headers.Location?.OriginalString ?? "");
    }

    [Fact]
    public async Task Login_WithValidCredentials_RedirectsToHome()
    {
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["email"] = "elena@havensanctuary.org",
            ["password"] = "shelter123"
        });
        var response = await _client.PostAsync("/auth/login", form);
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Login_WithWrongPassword_RedirectsToLoginWithError()
    {
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["email"] = "elena@havensanctuary.org",
            ["password"] = "wrongpassword"
        });
        var response = await _client.PostAsync("/auth/login", form);
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains("error=1", response.Headers.Location?.OriginalString ?? "");
    }

    [Fact]
    public async Task Login_WithUnknownEmail_RedirectsToLoginWithError()
    {
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["email"] = "nobody@nowhere.com",
            ["password"] = "shelter123"
        });
        var response = await _client.PostAsync("/auth/login", form);
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains("error=1", response.Headers.Location?.OriginalString ?? "");
    }

    [Fact]
    public async Task Logout_RedirectsToLogin()
    {
        var response = await _client.GetAsync("/auth/logout");
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains("/login", response.Headers.Location?.OriginalString ?? "");
    }
}
