using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Refugio.Application.Contracts;

namespace Refugio.Tests.Integration;

/// <summary>Covers /api/tasks through the full pipeline: endpoint → TaskActor → ITaskService → DB.</summary>
public class TasksApiTests : IClassFixture<ShelterWebFactory>
{
    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly ShelterWebFactory _factory;

    public TasksApiTests(ShelterWebFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_List_Complete_Delete_Task_Roundtrip()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var create = await client.PostAsJsonAsync("/api/tasks", new
        {
            Title = "ActorRoundtripTask",
            DueDateTime = DateTime.UtcNow.AddDays(1),
            Notes = "via actor",
            Location = "Yard",
            AssignedVolunteerId = (int?)null
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var task = await create.Content.ReadFromJsonAsync<ShelterTaskDto>(_jsonOpts);
        Assert.NotNull(task);
        Assert.Equal("ActorRoundtripTask", task.Title);

        var list = await client.GetFromJsonAsync<List<ShelterTaskDto>>("/api/tasks", _jsonOpts);
        Assert.NotNull(list);
        Assert.Contains(list, t => t.Id == task.Id);

        var complete = await client.PutAsync($"/api/tasks/{task.Id}/complete", null);
        Assert.Equal(HttpStatusCode.NoContent, complete.StatusCode);

        var delete = await client.DeleteAsync($"/api/tasks/{task.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    [Fact]
    public async Task Complete_UnknownTask_ReturnsNotFound()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var response = await client.PutAsync("/api/tasks/99999/complete", null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
