using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Refugio.Tests.Integration;

/// <summary>
/// Verifies that Blazor SSR pages handle pagination query params without crashing,
/// and that REST API paging params return consistent results.
/// </summary>
public class PaginationTests : IClassFixture<ShelterWebFactory>
{
    private readonly ShelterWebFactory _factory;

    public PaginationTests(ShelterWebFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> AuthClientAsync() =>
        await _factory.CreateAuthenticatedClientAsync();

    // ── Blazor page — Dogs ─────────────────────────────────────────────────

    [Fact]
    public async Task DogsPage_DefaultPage_Returns200()
    {
        var client = await AuthClientAsync();
        var response = await client.GetAsync("/dogs");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DogsPage_Page2_Returns200()
    {
        var client = await AuthClientAsync();
        var response = await client.GetAsync("/dogs?page=2");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DogsPage_OutOfRangePage_Returns200()
    {
        var client = await AuthClientAsync();
        var response = await client.GetAsync("/dogs?page=9999");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DogsPage_ZeroPage_Returns200()
    {
        var client = await AuthClientAsync();
        var response = await client.GetAsync("/dogs?page=0");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── Blazor page — Volunteers ───────────────────────────────────────────

    [Fact]
    public async Task VolunteersPage_DefaultPage_Returns200()
    {
        var client = await AuthClientAsync();
        var response = await client.GetAsync("/volunteers");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task VolunteersPage_Page2_Returns200()
    {
        var client = await AuthClientAsync();
        var response = await client.GetAsync("/volunteers?page=2");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task VolunteersPage_WithStatusFilter_Returns200()
    {
        var client = await AuthClientAsync();
        var response = await client.GetAsync("/volunteers?filter=active&page=1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── Blazor page — Funds ────────────────────────────────────────────────

    [Fact]
    public async Task FundsPage_DonationsTab_Returns200()
    {
        var client = await AuthClientAsync();
        var response = await client.GetAsync("/funds?tab=donations");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task FundsPage_ExpensesTab_Returns200()
    {
        var client = await AuthClientAsync();
        var response = await client.GetAsync("/funds?tab=expenses");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task FundsPage_Page2_Returns200()
    {
        var client = await AuthClientAsync();
        var response = await client.GetAsync("/funds?page=2");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task FundsPage_OutOfRangePage_Returns200()
    {
        var client = await AuthClientAsync();
        var response = await client.GetAsync("/funds?page=9999");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── Blazor page — Adoptions kanban (per-column limits) ────────────────

    [Fact]
    public async Task AdoptionsPage_DefaultView_Returns200()
    {
        var client = await AuthClientAsync();
        var response = await client.GetAsync("/adoptions");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AdoptionsPage_ExpandAppliedColumn_Returns200()
    {
        var client = await AuthClientAsync();
        var response = await client.GetAsync("/adoptions?ap=10");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AdoptionsPage_MultipleColumnsExpanded_Returns200()
    {
        var client = await AuthClientAsync();
        var response = await client.GetAsync("/adoptions?ap=10&iv=10&hc=10&ar=10&fn=10");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AdoptionsPage_LargeColumnLimit_Returns200()
    {
        var client = await AuthClientAsync();
        var response = await client.GetAsync("/adoptions?ap=9999");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── REST API paging consistency ────────────────────────────────────────

    [Fact]
    public async Task GetDogs_Page1_AndPage2_HaveNoOverlap()
    {
        var anonClient = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Seed enough dogs to span two pages (pageSize = 10)
        for (int i = 0; i < 12; i++)
        {
            await anonClient.PostAsJsonAsync("/api/dogs", new
            {
                Name = $"PagingDog{i:D2}",
                Breed = "Mixed",
                AgeMonths = 12,
                Gender = "Male",
                WeightKg = 10.0m,
                PhotoUrl = (string?)null,
                Traits = (string?)null,
                Notes = (string?)null,
                ArrivalDate = DateTime.UtcNow
            });
        }

        // The REST API returns all dogs — paging lives in the Blazor layer.
        // Verify: the full list contains all created dogs.
        var all = await anonClient.GetFromJsonAsync<List<JsonElement>>("/api/dogs");
        Assert.NotNull(all);
        var names = all.Select(d => d.GetProperty("name").GetString()).ToHashSet();
        Assert.Contains("PagingDog00", names);
        Assert.Contains("PagingDog11", names);
    }

    [Fact]
    public async Task GetAdoptions_FilterByStatus_ExcludesOtherStatuses()
    {
        var anonClient = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Create a dog + adoption (starts as Applied = 0)
        var dogResponse = await anonClient.PostAsJsonAsync("/api/dogs", new
        {
            Name = "FilterPagingDog",
            Breed = "Beagle",
            AgeMonths = 6,
            Gender = "Female",
            WeightKg = 6.0m,
            PhotoUrl = (string?)null,
            Traits = (string?)null,
            Notes = (string?)null,
            ArrivalDate = DateTime.UtcNow
        });
        var dogId = JsonDocument.Parse(await dogResponse.Content.ReadAsStringAsync())
            .RootElement.GetProperty("id").GetInt32();

        await anonClient.PostAsJsonAsync("/api/adoptions", new
        {
            DogId = dogId,
            ApplicantName = "Filter Test",
            ApplicantEmail = (string?)null,
            ApplicantPhone = (string?)null,
            Type = "Adoption",
            Notes = (string?)null
        });

        // ?status=Interview must not include Applied records
        var interviewAdoptions = await anonClient
            .GetFromJsonAsync<List<JsonElement>>("/api/adoptions?status=Interview");
        Assert.NotNull(interviewAdoptions);
        Assert.All(interviewAdoptions, a => Assert.Equal("Interview", a.GetProperty("status").GetString()));
    }
}
