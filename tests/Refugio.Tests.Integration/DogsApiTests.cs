using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using Refugio.Application.Contracts;
using Refugio.Domain.Entities;

namespace Refugio.Tests.Integration;

public class DogsApiTests : IClassFixture<ShelterWebFactory>, IAsyncLifetime
{
    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly ShelterWebFactory _factory;
    private readonly HttpClient _anonClient;
    private HttpClient _client = null!;

    public DogsApiTests(ShelterWebFactory factory)
    {
        _factory = factory;
        _anonClient = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    public async Task InitializeAsync()
    {
        _client = await _factory.CreateAuthenticatedClientAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetDogs_ReturnsOk_WithoutAuth()
    {
        var response = await _anonClient.GetAsync("/api/dogs");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetDogs_ReturnsJsonArray()
    {
        var dogs = await _anonClient.GetFromJsonAsync<List<DogDto>>("/api/dogs", _jsonOpts);
        Assert.NotNull(dogs);
    }

    [Fact]
    public async Task GetDog_NonExistent_Returns404()
    {
        var response = await _anonClient.GetAsync("/api/dogs/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostDog_CreatesAndReturns201()
    {
        var response = await _client.PostAsJsonAsync("/api/dogs", new
        {
            Name = "Integration Test Dog",
            Breed = "Labrador",
            AgeMonths = 18,
            Gender = "Male",
            WeightKg = 25.5m,
            PhotoUrl = (string?)null,
            Traits = (string?)null,
            Notes = (string?)null,
            ArrivalDate = DateTime.UtcNow
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var id = doc.RootElement.GetProperty("id").GetInt32();
        var name = doc.RootElement.GetProperty("name").GetString();
        Assert.True(id > 0);
        Assert.Equal("Integration Test Dog", name);
    }

    [Fact]
    public async Task GetDog_AfterCreate_ReturnsIt()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/dogs", new
        {
            Name = "Retrievable",
            Breed = "Beagle",
            AgeMonths = 6,
            Gender = "Female",
            WeightKg = 8.0m,
            PhotoUrl = (string?)null,
            Traits = (string?)null,
            Notes = (string?)null,
            ArrivalDate = DateTime.UtcNow
        });
        var json = await createResponse.Content.ReadAsStringAsync();
        var id = JsonDocument.Parse(json).RootElement.GetProperty("id").GetInt32();

        var getResponse = await _anonClient.GetAsync($"/api/dogs/{id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteDog_NonExistent_Returns404()
    {
        var response = await _client.DeleteAsync("/api/dogs/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteDog_AfterCreate_Returns204()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/dogs", new
        {
            Name = "ToDelete",
            Breed = "Mixed",
            AgeMonths = 12,
            Gender = "Male",
            WeightKg = 10.0m,
            PhotoUrl = (string?)null,
            Traits = (string?)null,
            Notes = (string?)null,
            ArrivalDate = DateTime.UtcNow
        });
        var json = await createResponse.Content.ReadAsStringAsync();
        var id = JsonDocument.Parse(json).RootElement.GetProperty("id").GetInt32();

        var deleteResponse = await _client.DeleteAsync($"/api/dogs/{id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }
}
