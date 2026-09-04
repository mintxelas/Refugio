using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Refugio.Tests.Integration;

/// <summary>
/// Proves CRIT-1 (anon volunteer escalation to Manager) and CRIT-2 (unauthenticated data access).
/// All tests assert the SECURE behavior; they fail before the fix and pass after.
/// </summary>
public class SecurityTests : IClassFixture<ShelterWebFactory>
{
    private readonly ShelterWebFactory _factory;

    public SecurityTests(ShelterWebFactory factory)
    {
        _factory = factory;
    }

    private HttpClient AnonClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    // ── CRIT-1: anonymous must not create a Manager account ───────────────

    [Fact]
    public async Task AnonPost_CreateManagerVolunteer_IsRejected()
    {
        var response = await AnonClient().PostAsJsonAsync("/api/volunteers", new
        {
            Name = "Attacker",
            Email = "attacker@evil.tld",
            Role = "Manager",
            CanLogin = true,
            Password = "pwn12345"
        });

        Assert.True(
            response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Redirect,
            $"Expected 401 or redirect, got {response.StatusCode}");
    }

    [Fact]
    public async Task AnonPost_CreateVolunteer_IsRejected()
    {
        var response = await AnonClient().PostAsJsonAsync("/api/volunteers", new
        {
            Name = "Attacker",
            Email = "attacker2@evil.tld",
            Role = "Volunteer",
            CanLogin = false
        });

        Assert.True(
            response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Redirect,
            $"Expected 401 or redirect, got {response.StatusCode}");
    }

    // ── CRIT-2: protected financial + PII endpoints require auth ─────────

    [Fact]
    public async Task AnonGet_Donations_Returns401OrRedirect()
    {
        var response = await AnonClient().GetAsync("/api/donations");
        Assert.True(
            response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Redirect,
            $"Expected 401 or redirect, got {response.StatusCode}");
    }

    [Fact]
    public async Task AnonGet_Expenses_Returns401OrRedirect()
    {
        var response = await AnonClient().GetAsync("/api/expenses");
        Assert.True(
            response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Redirect,
            $"Expected 401 or redirect, got {response.StatusCode}");
    }

    [Fact]
    public async Task AnonGet_Adoptions_Returns401OrRedirect()
    {
        var response = await AnonClient().GetAsync("/api/adoptions");
        Assert.True(
            response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Redirect,
            $"Expected 401 or redirect, got {response.StatusCode}");
    }

    [Fact]
    public async Task AnonGet_Volunteers_Returns401OrRedirect()
    {
        var response = await AnonClient().GetAsync("/api/volunteers");
        Assert.True(
            response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Redirect,
            $"Expected 401 or redirect, got {response.StatusCode}");
    }

    [Fact]
    public async Task AnonGet_UrgentMedications_Returns401OrRedirect()
    {
        var response = await AnonClient().GetAsync("/api/reports/urgent-medications");
        Assert.True(
            response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Redirect,
            $"Expected 401 or redirect, got {response.StatusCode}");
    }

    [Fact]
    public async Task AnonPost_CreateDonation_Returns401OrRedirect()
    {
        var response = await AnonClient().PostAsJsonAsync("/api/donations", new
        {
            DonorName = "Attacker",
            Amount = 1m,
            Category = "OneTime"
        });
        Assert.True(
            response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Redirect,
            $"Expected 401 or redirect, got {response.StatusCode}");
    }

    [Fact]
    public async Task AnonPost_CreateDog_Returns401OrRedirect()
    {
        var response = await AnonClient().PostAsJsonAsync("/api/dogs", new
        {
            Name = "AttackerDog",
            Breed = "X",
            AgeMonths = 1,
            Gender = "Male",
            WeightKg = 1m,
            ArrivalDate = DateTime.UtcNow
        });
        Assert.True(
            response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Redirect,
            $"Expected 401 or redirect, got {response.StatusCode}");
    }

    // ── Public endpoints still reachable without auth ─────────────────────

    [Fact]
    public async Task AnonGet_Dashboard_ReturnsOk()
    {
        var response = await AnonClient().GetAsync("/api/dashboard");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AnonGet_DogsList_ReturnsOk()
    {
        var response = await AnonClient().GetAsync("/api/dogs");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
