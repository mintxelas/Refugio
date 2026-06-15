using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Refugio.Tests.Integration;

public class FinanceCsvTests : IClassFixture<ShelterWebFactory>
{
    private readonly ShelterWebFactory _factory;

    public FinanceCsvTests(ShelterWebFactory factory)
    {
        _factory = factory;
    }

    private HttpClient AnonClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    // ── Donations ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ExportDonations_Unauthenticated_RedirectsToLogin()
    {
        var response = await AnonClient().GetAsync("/api/export/donations");
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains("login", response.Headers.Location?.OriginalString ?? "");
    }

    [Fact]
    public async Task ExportDonations_ReturnsTextCsv()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var response = await client.GetAsync("/api/export/donations");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task ExportDonations_HasCorrectHeaderRow()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var csv = await client.GetStringAsync("/api/export/donations");
        var firstLine = csv.Split("\r\n", 2)[0];
        Assert.Equal("Date,Donor Name,Category,Amount,Notes,Tax ID", firstLine);
    }

    [Fact]
    public async Task ExportDonations_ContainsCreatedDonation()
    {
        var authClient = await _factory.CreateAuthenticatedClientAsync();
        await authClient.PostAsJsonAsync("/api/donations", new
        {
            DonorName = "CSV Export Donor",
            Amount = 99.50m,
            Category = "OneTime",
            Notes = (string?)null
        });

        var csv = await authClient.GetStringAsync("/api/export/donations");
        Assert.Contains("CSV Export Donor", csv);
        Assert.Contains("99.50", csv);
    }

    // ── Expenses ───────────────────────────────────────────────────────────

    [Fact]
    public async Task ExportExpenses_Unauthenticated_RedirectsToLogin()
    {
        var response = await AnonClient().GetAsync("/api/export/expenses");
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains("login", response.Headers.Location?.OriginalString ?? "");
    }

    [Fact]
    public async Task ExportExpenses_ReturnsTextCsv()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var response = await client.GetAsync("/api/export/expenses");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task ExportExpenses_HasCorrectHeaderRow()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var csv = await client.GetStringAsync("/api/export/expenses");
        var firstLine = csv.Split("\r\n", 2)[0];
        Assert.Equal("Date,Description,Category,Amount,Notes", firstLine);
    }

    [Fact]
    public async Task ExportExpenses_ContainsCreatedExpense()
    {
        var authClient = await _factory.CreateAuthenticatedClientAsync();
        await authClient.PostAsJsonAsync("/api/expenses", new
        {
            Description = "Vet Bills CSV Test",
            Amount = 250.00m,
            Category = "Medical",
            Notes = (string?)null,
            taxLines = new[] { new { ivaPercent = 21m, @base = 250.00m, importe = 52.50m } }
        });

        var csv = await authClient.GetStringAsync("/api/export/expenses");
        Assert.Contains("Vet Bills CSV Test", csv);
        Assert.Contains("250.00", csv);
    }

    // ── Adoptions ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ExportAdoptions_Unauthenticated_RedirectsToLogin()
    {
        var response = await AnonClient().GetAsync("/api/export/adoptions");
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains("login", response.Headers.Location?.OriginalString ?? "");
    }

    [Fact]
    public async Task ExportAdoptions_ReturnsTextCsv()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var response = await client.GetAsync("/api/export/adoptions");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task ExportAdoptions_HasCorrectHeaderRow()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var csv = await client.GetStringAsync("/api/export/adoptions");
        var firstLine = csv.Split("\r\n", 2)[0];
        Assert.Equal("ID,Applicant Name,Email,Phone,Type,Status,Dog Name,Created,Updated,Notes", firstLine);
    }

    [Fact]
    public async Task ExportAdoptions_ContainsCreatedAdoption()
    {
        var authClient = await _factory.CreateAuthenticatedClientAsync();
        var dogResponse = await authClient.PostAsJsonAsync("/api/dogs", new
        {
            Name = "CSV Adoption Dog",
            Breed = "Poodle",
            AgeMonths = 12,
            Gender = "Male",
            WeightKg = 5.0m,
            PhotoUrl = (string?)null,
            Traits = (string?)null,
            Notes = (string?)null,
            ArrivalDate = DateTime.UtcNow
        });
        var dogJson = await dogResponse.Content.ReadAsStringAsync();
        var dogId = JsonDocument.Parse(dogJson).RootElement.GetProperty("id").GetInt32();

        await authClient.PostAsJsonAsync("/api/adoptions", new
        {
            DogId = dogId,
            ApplicantName = "CSV Applicant",
            ApplicantEmail = "csvapplicant@test.com",
            ApplicantPhone = (string?)null,
            Type = "Adoption",
            Notes = (string?)null
        });

        var csv = await authClient.GetStringAsync("/api/export/adoptions");
        Assert.Contains("CSV Applicant", csv);
        Assert.Contains("csvapplicant@test.com", csv);
    }

    // ── Finance summary ────────────────────────────────────────────────────

    [Fact]
    public async Task GetFinanceSummary_ReturnsOk()
    {
        var authClient = await _factory.CreateAuthenticatedClientAsync();
        var response = await authClient.GetAsync("/api/finances/summary");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetFinanceSummary_ReflectsCreatedDonation()
    {
        var authClient = await _factory.CreateAuthenticatedClientAsync();
        await authClient.PostAsJsonAsync("/api/donations", new
        {
            DonorName = "Summary Test Donor",
            Amount = 500.00m,
            Category = "OneTime",
            Notes = (string?)null
        });

        var response = await authClient.GetAsync($"/api/finances/summary?year={DateTime.UtcNow.Year}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var totalIncome = doc.RootElement.GetProperty("totalIncome").GetDecimal();
        Assert.True(totalIncome >= 500.00m);
    }
}
