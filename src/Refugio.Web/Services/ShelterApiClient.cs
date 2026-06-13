using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Refugio.Application.Contracts;
using Refugio.Domain.Common;
using Refugio.Domain.Entities;

namespace Refugio.Web.Services;

/// <summary>
/// Typed HTTP client the Blazor SSR pages use to consume the REST API. Every call is a
/// real HTTP request to this same host (cookie-forwarded, so the user's session and role
/// apply). Base address comes from the current request; in integration tests the named
/// client is rewired to the TestServer handler.
/// </summary>
public class ShelterApiClient
{
    public const string ClientName = "ShelterApi";

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _http;

    public ShelterApiClient(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
    {
        _http = httpClientFactory.CreateClient(ClientName);
        var request = httpContextAccessor.HttpContext?.Request;
        _http.BaseAddress = request is not null
            ? new Uri($"{request.Scheme}://{request.Host}")
            : new Uri("http://localhost:5110");
    }

    // --- Dogs ---

    public Task<List<DogDto>> GetDogs(string? search = null, DogStatus? status = null)
        => GetRequired<List<DogDto>>($"/api/dogs{Query(("search", search), ("status", status?.ToString()))}");

    public Task<Page<DogDto>> GetDogsPaged(string? search, DogStatus? status, int page, int pageSize = 20)
        => GetRequired<Page<DogDto>>($"/api/dogs/paged{Query(("search", search), ("status", status?.ToString()), ("page", page.ToString()), ("pageSize", pageSize.ToString()))}");

    public Task<DogDto?> GetDog(int id) => GetOrNull<DogDto>($"/api/dogs/{id}");

    public Task<DogDto> CreateDog(CreateDogRequest request) => PostRequired<DogDto>("/api/dogs", request);

    public Task<DogDto?> UpdateDog(UpdateDogRequest request) => PutOrNull<DogDto>($"/api/dogs/{request.Id}", request);

    public Task<bool> DeleteDog(int id) => Delete($"/api/dogs/{id}");

    public Task<List<DogPhotoDto>> GetDogPhotos(int dogId)
        => GetRequired<List<DogPhotoDto>>($"/api/dogs/{dogId}/photos");

    // --- Medical records ---

    public Task<List<MedicalRecordDto>> GetMedicalRecords(int dogId)
        => GetRequired<List<MedicalRecordDto>>($"/api/dogs/{dogId}/medical");

    public Task<MedicalRecordDto?> GetMedicalRecord(int id) => GetOrNull<MedicalRecordDto>($"/api/medical/{id}");

    public Task<MedicalRecordDto?> CreateMedicalRecord(CreateMedicalRecordRequest request)
        => PostOrNull<MedicalRecordDto>($"/api/dogs/{request.DogId}/medical", request);

    public Task<MedicalRecordDto?> UpdateMedicalRecord(UpdateMedicalRecordRequest request)
        => PutOrNull<MedicalRecordDto>($"/api/medical/{request.Id}", request);

    public Task<bool> DeleteMedicalRecord(int id) => Delete($"/api/medical/{id}");

    // --- Medications ---

    public Task<List<MedicationDto>> GetMedications(int dogId)
        => GetRequired<List<MedicationDto>>($"/api/dogs/{dogId}/medications");

    public Task<MedicationDto?> GetMedication(int id) => GetOrNull<MedicationDto>($"/api/medications/{id}");

    public Task<MedicationDto?> CreateMedication(CreateMedicationRequest request)
        => PostOrNull<MedicationDto>($"/api/dogs/{request.DogId}/medications", request);

    public Task<MedicationDto?> UpdateMedication(UpdateMedicationRequest request)
        => PutOrNull<MedicationDto>($"/api/medications/{request.Id}", request);

    // --- Dashboard / reports ---

    public Task<DashboardStats> GetDashboardStats() => GetRequired<DashboardStats>("/api/dashboard");

    public Task<AdoptionConversionStats> GetAdoptionConversionStats(int year)
        => GetRequired<AdoptionConversionStats>($"/api/reports/adoption-conversion?year={year}");

    public Task<ShelterStayStats> GetShelterStayStats()
        => GetRequired<ShelterStayStats>("/api/reports/shelter-stay");

    // --- Deleted / Admin ---

    public Task<List<DogDto>> GetDeletedDogs() => GetRequired<List<DogDto>>("/api/dogs/deleted");
    public Task<DogDto?> GetDeletedDogById(int id) => GetOrNull<DogDto>($"/api/dogs/deleted/{id}");

    public Task<List<AdoptionDto>> GetDeletedAdoptions() => GetRequired<List<AdoptionDto>>("/api/adoptions/deleted");
    public Task<AdoptionDto?> GetDeletedAdoptionById(int id) => GetOrNull<AdoptionDto>($"/api/adoptions/deleted/{id}");

    public Task<List<VolunteerDto>> GetDeletedVolunteers() => GetRequired<List<VolunteerDto>>("/api/volunteers/deleted");
    public Task<VolunteerDto?> GetDeletedVolunteerById(int id) => GetOrNull<VolunteerDto>($"/api/volunteers/deleted/{id}");

    public Task<List<DonationDto>> GetDeletedDonations() => GetRequired<List<DonationDto>>("/api/donations/deleted");
    public Task<DonationDto?> GetDeletedDonationById(int id) => GetOrNull<DonationDto>($"/api/donations/deleted/{id}");

    public Task<List<ExpenseDto>> GetDeletedExpenses() => GetRequired<List<ExpenseDto>>("/api/expenses/deleted");
    public Task<ExpenseDto?> GetDeletedExpenseById(int id) => GetOrNull<ExpenseDto>($"/api/expenses/deleted/{id}");

    public Task<List<MedicalRecordDto>> GetDeletedMedicalRecords() => GetRequired<List<MedicalRecordDto>>("/api/medical/deleted");
    public Task<MedicalRecordDto?> GetDeletedMedicalRecordById(int id) => GetOrNull<MedicalRecordDto>($"/api/medical/deleted/{id}");

    public Task<List<MedicationDto>> GetDeletedMedications() => GetRequired<List<MedicationDto>>("/api/medications/deleted");
    public Task<MedicationDto?> GetDeletedMedicationById(int id) => GetOrNull<MedicationDto>($"/api/medications/deleted/{id}");

    public Task<List<GoalDto>> GetDeletedGoals() => GetRequired<List<GoalDto>>("/api/goals/deleted");
    public Task<GoalDto?> GetDeletedGoalById(int id) => GetOrNull<GoalDto>($"/api/goals/deleted/{id}");

    // --- Tasks ---

    public Task<List<ShelterTaskDto>> GetTasks(bool includeCompleted = false)
        => GetRequired<List<ShelterTaskDto>>($"/api/tasks?includeCompleted={includeCompleted}");

    public Task<ShelterTaskDto> CreateTask(CreateTaskRequest request) => PostRequired<ShelterTaskDto>("/api/tasks", request);

    // --- Adoptions ---

    public Task<List<AdoptionDto>> GetAdoptions(AdoptionStatus? status = null)
        => GetRequired<List<AdoptionDto>>($"/api/adoptions{Query(("status", status?.ToString()))}");

    public Task<Page<AdoptionDto>> GetAdoptionsPaged(AdoptionStatus? status, int page, int pageSize = 25)
        => GetRequired<Page<AdoptionDto>>($"/api/adoptions/paged{Query(("status", status?.ToString()), ("page", page.ToString()), ("pageSize", pageSize.ToString()))}");

    public Task<AdoptionDto?> GetAdoption(int id) => GetOrNull<AdoptionDto>($"/api/adoptions/{id}");

    public Task<AdoptionDto> CreateAdoption(CreateAdoptionRequest request) => PostRequired<AdoptionDto>("/api/adoptions", request);

    public Task<AdoptionDto?> UpdateAdoption(UpdateAdoptionRequest request)
        => PutOrNull<AdoptionDto>($"/api/adoptions/{request.Id}", request);

    public Task<AdoptionDto?> UpdateAdoptionStatus(int id, AdoptionStatus newStatus)
        => PutOrNull<AdoptionDto>($"/api/adoptions/{id}/status", new UpdateAdoptionStatusRequest(id, newStatus, null));

    public Task<List<AdoptionPhotoDto>> GetAdoptionPhotos(int adoptionId)
        => GetRequired<List<AdoptionPhotoDto>>($"/api/adoptions/{adoptionId}/photos");

    // --- Finance ---

    public Task<List<DonationDto>> GetDonations() => GetRequired<List<DonationDto>>("/api/donations");

    public Task<Page<DonationDto>> GetDonationsPaged(int page, int pageSize = 25)
        => GetRequired<Page<DonationDto>>($"/api/donations/paged?page={page}&pageSize={pageSize}");

    public Task<DonationDto?> GetDonation(int id) => GetOrNull<DonationDto>($"/api/donations/{id}");

    public Task<DonationDto> CreateDonation(CreateDonationRequest request) => PostRequired<DonationDto>("/api/donations", request);

    public Task<DonationDto?> UpdateDonation(UpdateDonationRequest request)
        => PutOrNull<DonationDto>($"/api/donations/{request.Id}", request);

    public Task<List<ExpenseDto>> GetExpenses() => GetRequired<List<ExpenseDto>>("/api/expenses");

    public Task<Page<ExpenseDto>> GetExpensesPaged(int page, int pageSize = 25)
        => GetRequired<Page<ExpenseDto>>($"/api/expenses/paged?page={page}&pageSize={pageSize}");

    public Task<ExpenseDto?> GetExpense(int id) => GetOrNull<ExpenseDto>($"/api/expenses/{id}");

    public Task<ExpenseDto> CreateExpense(CreateExpenseRequest request) => PostRequired<ExpenseDto>("/api/expenses", request);

    public Task<ExpenseDto?> UpdateExpense(UpdateExpenseRequest request)
        => PutOrNull<ExpenseDto>($"/api/expenses/{request.Id}", request);

    public Task<List<ExpensePhotoDto>> GetExpensePhotos(int expenseId)
        => GetRequired<List<ExpensePhotoDto>>($"/api/expenses/{expenseId}/photos");

    public Task<FinanceSummary> GetFinanceSummary(int year)
        => GetRequired<FinanceSummary>($"/api/finances/summary?year={year}");

    // --- Goals ---

    public Task<List<GoalDto>> GetGoals() => GetRequired<List<GoalDto>>("/api/goals");

    public Task<GoalDto?> GetGoal(int id) => GetOrNull<GoalDto>($"/api/goals/{id}");

    public Task<GoalDto> CreateGoal(CreateGoalRequest request) => PostRequired<GoalDto>("/api/goals", request);

    public Task<GoalDto?> UpdateGoal(UpdateGoalRequest request)
        => PutOrNull<GoalDto>($"/api/goals/{request.Id}", request);

    // --- Volunteers ---

    public Task<VolunteerDto?> GetVolunteer(int id) => GetOrNull<VolunteerDto>($"/api/volunteers/{id}");

    public Task<List<VolunteerDto>> GetVolunteers(VolunteerStatus? status = null)
        => GetRequired<List<VolunteerDto>>($"/api/volunteers{Query(("status", status?.ToString()))}");

    public Task<Page<VolunteerDto>> GetVolunteersPaged(VolunteerStatus? status, int page, int pageSize = 25)
        => GetRequired<Page<VolunteerDto>>($"/api/volunteers/paged{Query(("status", status?.ToString()), ("page", page.ToString()), ("pageSize", pageSize.ToString()))}");

    public Task<VolunteerCounts> GetVolunteerCounts() => GetRequired<VolunteerCounts>("/api/volunteers/counts");

    public Task<VolunteerDto> CreateVolunteer(CreateVolunteerRequest request)
        => PostRequired<VolunteerDto>("/api/volunteers", request);

    public Task<VolunteerDto?> UpdateVolunteer(UpdateVolunteerRequest request)
        => PutOrNull<VolunteerDto>($"/api/volunteers/{request.Id}", request);

    // --- Settings ---

    public Task<ShelterSettingsDto> GetSettings() => GetRequired<ShelterSettingsDto>("/api/settings");

    public Task<ShelterSettingsDto> UpdateSettings(string name, string? phrase)
        => PutRequired<ShelterSettingsDto>("/api/settings", new UpdateSettingsRequest(name, phrase));

    // --- Events ---

    public Task<List<ShelterEventDto>> GetEvents(DateTime? from = null, DateTime? to = null)
        => GetRequired<List<ShelterEventDto>>($"/api/events{Query(("from", from?.ToString("o")), ("to", to?.ToString("o")))}");

    public Task<ShelterEventDto?> GetEvent(int id) => GetOrNull<ShelterEventDto>($"/api/events/{id}");

    public Task<ShelterEventDto> CreateEvent(CreateEventRequest request) => PostRequired<ShelterEventDto>("/api/events", request);

    public Task<ShelterEventDto?> UpdateEvent(UpdateEventRequest request)
        => PutOrNull<ShelterEventDto>($"/api/events/{request.Id}", request);

    // --- HTTP plumbing ---

    private static string Query(params (string Name, string? Value)[] parameters)
    {
        var parts = parameters
            .Where(p => !string.IsNullOrEmpty(p.Value))
            .Select(p => $"{p.Name}={Uri.EscapeDataString(p.Value!)}")
            .ToList();
        return parts.Count == 0 ? "" : "?" + string.Join("&", parts);
    }

    private async Task<T> GetRequired<T>(string url)
    {
        var response = await _http.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>(JsonOpts))!;
    }

    private async Task<T?> GetOrNull<T>(string url)
    {
        var response = await _http.GetAsync(url);
        if (response.StatusCode == HttpStatusCode.NotFound) return default;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(JsonOpts);
    }

    private async Task<T> PostRequired<T>(string url, object body)
    {
        var response = await _http.PostAsJsonAsync(url, body, JsonOpts);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>(JsonOpts))!;
    }

    private async Task<T?> PostOrNull<T>(string url, object body)
    {
        var response = await _http.PostAsJsonAsync(url, body, JsonOpts);
        if (response.StatusCode == HttpStatusCode.NotFound) return default;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(JsonOpts);
    }

    private async Task<T> PutRequired<T>(string url, object body)
    {
        var response = await _http.PutAsJsonAsync(url, body, JsonOpts);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>(JsonOpts))!;
    }

    private async Task<T?> PutOrNull<T>(string url, object body)
    {
        var response = await _http.PutAsJsonAsync(url, body, JsonOpts);
        if (response.StatusCode == HttpStatusCode.NotFound) return default;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(JsonOpts);
    }

    private async Task<bool> Delete(string url)
    {
        var response = await _http.DeleteAsync(url);
        if (response.StatusCode == HttpStatusCode.NotFound) return false;
        response.EnsureSuccessStatusCode();
        return true;
    }
}
