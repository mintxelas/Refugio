using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Refugio.Application.Contracts;

namespace Refugio.Tests.Integration;

public class ExpenseTaxLineTests : IClassFixture<ShelterWebFactory>
{
    private readonly ShelterWebFactory _factory;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public ExpenseTaxLineTests(ShelterWebFactory factory)
    {
        _factory = factory;
    }

    private object ExpenseBody(string desc, object[] taxLines) => new
    {
        description = desc,
        amount = 121m,
        category = "Medical",
        notes = (string?)null,
        taxLines,
    };

    private static object TaxLine(decimal iva, decimal @base, decimal importe) =>
        new { ivaPercent = iva, @base, importe };

    [Fact]
    public async Task CreateExpense_WithValidTaxLines_Returns201_AndGetReturnsLines()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var body = ExpenseBody("Vet supplies TaxLine test", [TaxLine(21m, 100m, 21m)]);

        var response = await client.PostAsJsonAsync("/api/expenses", body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<ExpenseDto>(JsonOpts);
        Assert.NotNull(created);
        Assert.True(created.Id > 0);

        var fetched = await client.GetFromJsonAsync<ExpenseDto>($"/api/expenses/{created.Id}", JsonOpts);
        Assert.NotNull(fetched?.TaxLines);
        Assert.Single(fetched.TaxLines);
        Assert.Equal(21m, fetched.TaxLines[0].IvaPercent);
        Assert.Equal(100m, fetched.TaxLines[0].Base);
        Assert.Equal(21m, fetched.TaxLines[0].Importe);
    }

    [Fact]
    public async Task UpdateExpense_ReplacesTaxLines()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        // Create with 2 tax lines
        var createBody = ExpenseBody("Replace TaxLines test", [TaxLine(21m, 100m, 21m), TaxLine(10m, 200m, 20m)]);
        var createResp = await client.PostAsJsonAsync("/api/expenses", createBody);
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);
        var created = await createResp.Content.ReadFromJsonAsync<ExpenseDto>(JsonOpts);
        Assert.NotNull(created);

        // Update with 1 tax line
        var updateBody = new
        {
            description = "Replace TaxLines test",
            amount = 121m,
            category = "Medical",
            notes = (string?)null,
            taxLines = new[] { TaxLine(21m, 100m, 21m) },
        };
        var updateResp = await client.PutAsJsonAsync($"/api/expenses/{created.Id}", updateBody);
        Assert.Equal(HttpStatusCode.OK, updateResp.StatusCode);

        var fetched = await client.GetFromJsonAsync<ExpenseDto>($"/api/expenses/{created.Id}", JsonOpts);
        Assert.NotNull(fetched?.TaxLines);
        Assert.Single(fetched.TaxLines);
    }

    [Fact]
    public async Task CreateExpense_WithNoTaxLines_Returns400()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var body = new
        {
            description = "No tax lines test",
            amount = 50m,
            category = "Food",
            notes = (string?)null,
            taxLines = Array.Empty<object>(),
        };

        var response = await client.PostAsJsonAsync("/api/expenses", body);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
