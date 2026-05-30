using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Refugio.Domain.Entities;

namespace Refugio.Tests.Integration;

public class AdoptionApiTests : IClassFixture<ShelterWebFactory>
{
    private readonly ShelterWebFactory _factory;
    private readonly HttpClient _client;

    public AdoptionApiTests(ShelterWebFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    private async Task<int> CreateDogAsync(string name = "AdoptionTestDog")
    {
        var response = await _client.PostAsJsonAsync("/api/dogs", new
        {
            Name = name,
            Breed = "Terrier",
            AgeMonths = 24,
            Gender = "Female",
            WeightKg = 7.5m,
            PhotoUrl = (string?)null,
            Traits = (string?)null,
            Notes = (string?)null
        });
        var json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json).RootElement.GetProperty("id").GetInt32();
    }

    private async Task<int> CreateAdoptionAsync(int dogId, string applicantName = "Jane Doe")
    {
        var response = await _client.PostAsJsonAsync("/api/adoptions", new
        {
            DogId = dogId,
            ApplicantName = applicantName,
            ApplicantEmail = "jane@example.com",
            ApplicantPhone = (string?)null,
            Type = 0, // AdoptionType.Adoption
            Notes = (string?)null
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json).RootElement.GetProperty("id").GetInt32();
    }

    [Fact]
    public async Task GetAdoptions_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/adoptions");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAdoptions_ReturnsJsonArray()
    {
        var adoptions = await _client.GetFromJsonAsync<List<Adoption>>("/api/adoptions");
        Assert.NotNull(adoptions);
    }

    [Fact]
    public async Task GetAdoption_NonExistent_Returns404()
    {
        var response = await _client.GetAsync("/api/adoptions/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostAdoption_CreatesAndReturns201WithAppliedStatus()
    {
        var dogId = await CreateDogAsync("PostTestDog");
        var response = await _client.PostAsJsonAsync("/api/adoptions", new
        {
            DogId = dogId,
            ApplicantName = "Create Test",
            ApplicantEmail = (string?)null,
            ApplicantPhone = (string?)null,
            Type = 0,
            Notes = (string?)null
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(doc.RootElement.GetProperty("id").GetInt32() > 0);
        Assert.Equal(0, doc.RootElement.GetProperty("status").GetInt32()); // Applied
    }

    [Fact]
    public async Task GetAdoption_AfterCreate_ReturnsIt()
    {
        var dogId = await CreateDogAsync("GetAfterCreateDog");
        var id = await CreateAdoptionAsync(dogId);
        var response = await _client.GetAsync($"/api/adoptions/{id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AdvanceAdoption_Unauthenticated_RedirectsToLogin()
    {
        var dogId = await CreateDogAsync("AdvanceUnauthDog");
        var id = await CreateAdoptionAsync(dogId);
        var response = await _client.PostAsync($"/api/adoptions/{id}/advance", new StringContent(""));
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains("login", response.Headers.Location?.OriginalString ?? "");
    }

    [Fact]
    public async Task AdvanceAdoption_Authenticated_AdvancesAppliedToInterview()
    {
        var dogId = await CreateDogAsync("AdvanceDog");
        var id = await CreateAdoptionAsync(dogId);

        var authClient = await _factory.CreateAuthenticatedClientAsync();
        var advanceResponse = await authClient.PostAsync($"/api/adoptions/{id}/advance", new StringContent(""));
        Assert.Equal(HttpStatusCode.Found, advanceResponse.StatusCode);

        var json = await _client.GetStringAsync($"/api/adoptions/{id}");
        var status = JsonDocument.Parse(json).RootElement.GetProperty("status").GetInt32();
        Assert.Equal(1, status); // Interview
    }

    [Fact]
    public async Task AdvanceAdoption_FullPipeline_ReachesFinalized()
    {
        var dogId = await CreateDogAsync("PipelineDog");
        var id = await CreateAdoptionAsync(dogId);

        var authClient = await _factory.CreateAuthenticatedClientAsync();
        for (int i = 0; i < 4; i++) // Applied → Interview → HomeCheck → Approved → Finalized
            await authClient.PostAsync($"/api/adoptions/{id}/advance", new StringContent(""));

        var json = await _client.GetStringAsync($"/api/adoptions/{id}");
        var status = JsonDocument.Parse(json).RootElement.GetProperty("status").GetInt32();
        Assert.Equal(4, status); // Finalized
    }

    [Fact]
    public async Task AdvanceAdoption_AlreadyFinalized_StatusUnchanged()
    {
        var dogId = await CreateDogAsync("FinalizedDog");
        var id = await CreateAdoptionAsync(dogId);

        var authClient = await _factory.CreateAuthenticatedClientAsync();
        for (int i = 0; i < 5; i++) // one extra advance beyond Finalized
            await authClient.PostAsync($"/api/adoptions/{id}/advance", new StringContent(""));

        var json = await _client.GetStringAsync($"/api/adoptions/{id}");
        var status = JsonDocument.Parse(json).RootElement.GetProperty("status").GetInt32();
        Assert.Equal(4, status); // Still Finalized
    }

    [Fact]
    public async Task RejectAdoption_Authenticated_SetsRejected()
    {
        var dogId = await CreateDogAsync("RejectDog");
        var id = await CreateAdoptionAsync(dogId);

        var authClient = await _factory.CreateAuthenticatedClientAsync();
        var response = await authClient.PostAsync($"/api/adoptions/{id}/reject", new StringContent(""));
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);

        var json = await _client.GetStringAsync($"/api/adoptions/{id}");
        var status = JsonDocument.Parse(json).RootElement.GetProperty("status").GetInt32();
        Assert.Equal(5, status); // Rejected
    }

    [Fact]
    public async Task PutAdoptionStatus_UpdatesToArbitraryStatus()
    {
        var dogId = await CreateDogAsync("StatusUpdateDog");
        var id = await CreateAdoptionAsync(dogId);

        var response = await _client.PutAsJsonAsync($"/api/adoptions/{id}/status", new
        {
            Id = id,
            NewStatus = 3, // Approved
            Notes = "Direct approval"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Equal(3, JsonDocument.Parse(json).RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task GetAdoptions_FilterByStatus_ReturnsMatchingOnly()
    {
        var dogId = await CreateDogAsync("FilterDog");
        await CreateAdoptionAsync(dogId);

        var adoptions = await _client.GetFromJsonAsync<List<JsonElement>>("/api/adoptions?status=0"); // Applied
        Assert.NotNull(adoptions);
        Assert.All(adoptions, a => Assert.Equal(0, a.GetProperty("status").GetInt32()));
    }

    [Fact]
    public async Task DeleteAdoption_AfterCreate_Returns204()
    {
        var dogId = await CreateDogAsync("DeleteAdoptionDog");
        var id = await CreateAdoptionAsync(dogId);

        var response = await _client.DeleteAsync($"/api/adoptions/{id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAdoption_NonExistent_Returns404()
    {
        var response = await _client.DeleteAsync("/api/adoptions/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
