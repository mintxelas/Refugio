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
    public async Task SetDefaultPhoto_ReturnsNoContent()
    {
        var dogId = await CreateDogAsync("DefaultPhotoDog");
        var photoId = await UploadPhotoAsync(dogId);

        var response = await _client.PostAsync($"/api/dogs/photos/{photoId}/default", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var photos = await _client.GetFromJsonAsync<List<DogPhotoDto>>($"/api/dogs/{dogId}/photos", _jsonOpts);
        Assert.True(photos!.Single(p => p.Id == photoId).IsDefault);
    }

    [Fact]
    public async Task DeletePhoto_ReturnsNoContent()
    {
        var dogId = await CreateDogAsync("DeletePhotoDog");
        var photoId = await UploadPhotoAsync(dogId);

        var response = await _client.PostAsync($"/api/dogs/photos/{photoId}/delete", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var photos = await _client.GetFromJsonAsync<List<DogPhotoDto>>($"/api/dogs/{dogId}/photos", _jsonOpts);
        Assert.DoesNotContain(photos!, p => p.Id == photoId);
    }

    private async Task<int> CreateDogAsync(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/dogs", new
        {
            Name = name,
            Breed = "Mixed",
            AgeMonths = 12,
            Gender = "Male",
            WeightKg = 10m,
            PhotoUrl = (string?)null,
            Traits = (string?)null,
            Notes = (string?)null,
            ArrivalDate = DateTime.UtcNow
        });
        var json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json).RootElement.GetProperty("id").GetInt32();
    }

    private static readonly byte[] _onePixelPng =
    {
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
        0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
        0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
        0x08, 0x02, 0x00, 0x00, 0x00, 0x90, 0x77, 0x53,
        0xDE, 0x00, 0x00, 0x00, 0x0C, 0x49, 0x44, 0x41,
        0x54, 0x08, 0xD7, 0x63, 0xF8, 0xCF, 0xC0, 0x00,
        0x00, 0x00, 0x02, 0x00, 0x01, 0xE2, 0x21, 0xBC,
        0x33, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E,
        0x44, 0xAE, 0x42, 0x60, 0x82
    };

    private async Task<int> UploadPhotoAsync(int dogId)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(_onePixelPng);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "Photos", "test-photo.png");

        var uploadResponse = await _client.PostAsync($"/api/dogs/{dogId}/photos/upload", content);
        Assert.Equal(HttpStatusCode.OK, uploadResponse.StatusCode);

        var photos = await _client.GetFromJsonAsync<List<DogPhotoDto>>($"/api/dogs/{dogId}/photos", _jsonOpts);
        return photos!.Single().Id;
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
