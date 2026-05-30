using Refugio.Application.Actors;
using Refugio.Application.Messages;
using Refugio.Application.Services;
using Refugio.Domain.Entities;

namespace Refugio.Web.Services;

public class ShelterApiClient(ShelterActorService actors)
{
    // Dogs
    public Task<List<Dog>> GetDogs(string? search = null, DogStatus? status = null)
        => actors.Ask<List<Dog>>(actors.Dogs, new GetAllDogs(search, status));

    public Task<DogPage> GetDogsPaged(string? search, DogStatus? status, int page, int pageSize = 20)
        => actors.Ask<DogPage>(actors.Dogs, new GetDogsPaged(search, status, page, pageSize));

    public Task<Dog?> GetDog(int id)
        => actors.Ask<Dog?>(actors.Dogs, new GetDogById(id));

    public Task<Dog> CreateDog(CreateDog cmd)
        => actors.Ask<Dog>(actors.Dogs, cmd);

    public Task<Dog?> UpdateDog(UpdateDog cmd)
        => actors.Ask<Dog?>(actors.Dogs, cmd);

    public Task<bool> DeleteDog(int id)
        => actors.Ask<bool>(actors.Dogs, new DeleteDog(id));

    public Task<bool> UpdateDogPhoto(int id, string? photoUrl)
        => actors.Ask<bool>(actors.Dogs, new UpdateDogPhoto(id, photoUrl));

    // Medical
    public Task<List<MedicalRecord>> GetMedicalRecords(int dogId)
        => actors.Ask<List<MedicalRecord>>(actors.Dogs, new GetMedicalRecords(dogId));

    public Task<MedicalRecord?> GetMedicalRecord(int id)
        => actors.Ask<MedicalRecord?>(actors.Dogs, new GetMedicalRecordById(id));

    public Task<MedicalRecord> CreateMedicalRecord(CreateMedicalRecord cmd)
        => actors.Ask<MedicalRecord>(actors.Dogs, cmd);

    public Task<MedicalRecord?> UpdateMedicalRecord(UpdateMedicalRecord cmd)
        => actors.Ask<MedicalRecord?>(actors.Dogs, cmd);

    public Task<bool> DeleteMedicalRecord(int id)
        => actors.Ask<bool>(actors.Dogs, new DeleteMedicalRecord(id));

    // Medications
    public Task<List<Medication>> GetMedications(int dogId)
        => actors.Ask<List<Medication>>(actors.Dogs, new GetMedications(dogId));

    public Task<Medication?> GetMedication(int id)
        => actors.Ask<Medication?>(actors.Dogs, new GetMedicationById(id));

    public Task<Medication> CreateMedication(CreateMedication cmd)
        => actors.Ask<Medication>(actors.Dogs, cmd);

    public Task<Medication?> UpdateMedication(UpdateMedication cmd)
        => actors.Ask<Medication?>(actors.Dogs, cmd);

    public Task<bool> DeleteMedication(int id)
        => actors.Ask<bool>(actors.Dogs, new DeleteMedication(id));

    // Dashboard
    public Task<DashboardStats> GetDashboardStats()
        => actors.Ask<DashboardStats>(actors.Dogs, new GetDashboardStats());

    // Deleted / Admin
    public Task<List<Dog>> GetDeletedDogs()
        => actors.Ask<List<Dog>>(actors.Dogs, new GetDeletedDogs());

    public Task<bool> RestoreDog(int id)
        => actors.Ask<bool>(actors.Dogs, new RestoreDog(id));

    public Task<List<Adoption>> GetDeletedAdoptions()
        => actors.Ask<List<Adoption>>(actors.Adoptions, new GetDeletedAdoptions());

    public Task<bool> RestoreAdoption(int id)
        => actors.Ask<bool>(actors.Adoptions, new RestoreAdoption(id));

    public Task<List<Volunteer>> GetDeletedVolunteers()
        => actors.Ask<List<Volunteer>>(actors.Volunteers, new GetDeletedVolunteers());

    public Task<bool> RestoreVolunteer(int id)
        => actors.Ask<bool>(actors.Volunteers, new RestoreVolunteer(id));

    public Task<List<Donation>> GetDeletedDonations()
        => actors.Ask<List<Donation>>(actors.Finance, new GetDeletedDonations());

    public Task<bool> RestoreDonation(int id)
        => actors.Ask<bool>(actors.Finance, new RestoreDonation(id));

    public Task<List<Expense>> GetDeletedExpenses()
        => actors.Ask<List<Expense>>(actors.Finance, new GetDeletedExpenses());

    public Task<bool> RestoreExpense(int id)
        => actors.Ask<bool>(actors.Finance, new RestoreExpense(id));

    public Task<List<MedicalRecord>> GetDeletedMedicalRecords()
        => actors.Ask<List<MedicalRecord>>(actors.Dogs, new GetDeletedMedicalRecords());

    public Task<bool> RestoreMedicalRecord(int id)
        => actors.Ask<bool>(actors.Dogs, new RestoreMedicalRecord(id));

    public Task<List<Medication>> GetDeletedMedications()
        => actors.Ask<List<Medication>>(actors.Dogs, new GetDeletedMedications());

    public Task<bool> RestoreMedication(int id)
        => actors.Ask<bool>(actors.Dogs, new RestoreMedication(id));

    // Tasks
    public Task<List<ShelterTask>> GetTasks(bool includeCompleted = false)
        => actors.Ask<List<ShelterTask>>(actors.Tasks, new GetAllTasks(includeCompleted));

    public Task<ShelterTask> CreateTask(CreateTask cmd)
        => actors.Ask<ShelterTask>(actors.Tasks, cmd);

    public Task<bool> CompleteTask(int id)
        => actors.Ask<bool>(actors.Tasks, new CompleteTask(id));

    public Task<bool> DeleteTask(int id)
        => actors.Ask<bool>(actors.Tasks, new DeleteTask(id));

    // Adoptions
    public Task<List<Adoption>> GetAdoptions(AdoptionStatus? status = null)
        => actors.Ask<List<Adoption>>(actors.Adoptions, new GetAllAdoptions(status));

    public Task<AdoptionPage> GetAdoptionsPaged(AdoptionStatus? status, int page, int pageSize = 25)
        => actors.Ask<AdoptionPage>(actors.Adoptions, new GetAdoptionsPaged(status, page, pageSize));

    public Task<Adoption?> GetAdoption(int id)
        => actors.Ask<Adoption?>(actors.Adoptions, new GetAdoptionById(id));

    public Task<Adoption> CreateAdoption(CreateAdoption cmd)
        => actors.Ask<Adoption>(actors.Adoptions, cmd);

    public Task<Adoption?> UpdateAdoption(UpdateAdoption cmd)
        => actors.Ask<Adoption?>(actors.Adoptions, cmd);

    public Task<Adoption?> UpdateAdoptionStatus(int id, AdoptionStatus newStatus)
        => actors.Ask<Adoption?>(actors.Adoptions, new UpdateAdoptionStatus(id, newStatus, null));

    // Finance
    public Task<List<Donation>> GetDonations()
        => actors.Ask<List<Donation>>(actors.Finance, new GetAllDonations());

    public Task<DonationPage> GetDonationsPaged(int page, int pageSize = 25)
        => actors.Ask<DonationPage>(actors.Finance, new GetDonationsPaged(page, pageSize));

    public Task<Donation?> GetDonation(int id)
        => actors.Ask<Donation?>(actors.Finance, new GetDonationById(id));

    public Task<Donation> CreateDonation(CreateDonation cmd)
        => actors.Ask<Donation>(actors.Finance, cmd);

    public Task<Donation?> UpdateDonation(UpdateDonation cmd)
        => actors.Ask<Donation?>(actors.Finance, cmd);

    public Task<bool> DeleteDonation(int id)
        => actors.Ask<bool>(actors.Finance, new DeleteDonation(id));

    public Task<List<Expense>> GetExpenses()
        => actors.Ask<List<Expense>>(actors.Finance, new GetAllExpenses());

    public Task<ExpensePage> GetExpensesPaged(int page, int pageSize = 25)
        => actors.Ask<ExpensePage>(actors.Finance, new GetExpensesPaged(page, pageSize));

    public Task<Expense?> GetExpense(int id)
        => actors.Ask<Expense?>(actors.Finance, new GetExpenseById(id));

    public Task<Expense> CreateExpense(CreateExpense cmd)
        => actors.Ask<Expense>(actors.Finance, cmd);

    public Task<Expense?> UpdateExpense(UpdateExpense cmd)
        => actors.Ask<Expense?>(actors.Finance, cmd);

    public Task<bool> DeleteExpense(int id)
        => actors.Ask<bool>(actors.Finance, new DeleteExpense(id));

    public Task<FinanceSummary> GetFinanceSummary(int year)
        => actors.Ask<FinanceSummary>(actors.Finance, new GetFinanceSummary(year));

    // Volunteers
    public Task<Volunteer?> GetVolunteer(int id)
        => actors.Ask<Volunteer?>(actors.Volunteers, new GetVolunteerById(id));

    public Task<List<Volunteer>> GetVolunteers(VolunteerStatus? status = null)
        => actors.Ask<List<Volunteer>>(actors.Volunteers, new GetAllVolunteers(status));

    public Task<VolunteerPage> GetVolunteersPaged(VolunteerStatus? status, int page, int pageSize = 25)
        => actors.Ask<VolunteerPage>(actors.Volunteers, new GetVolunteersPaged(status, page, pageSize));

    public Task<Volunteer> CreateVolunteer(CreateVolunteer cmd)
        => actors.Ask<Volunteer>(actors.Volunteers, cmd);

    public Task<Volunteer?> UpdateVolunteer(UpdateVolunteer cmd)
        => actors.Ask<Volunteer?>(actors.Volunteers, cmd);

    public Task<Volunteer?> UpdateVolunteerStatus(int id, VolunteerStatus status)
        => actors.Ask<Volunteer?>(actors.Volunteers, new UpdateVolunteerStatus(id, status));

    public Task<Volunteer?> LoginVolunteer(string email, string password)
        => actors.Ask<Volunteer?>(actors.Volunteers, new LoginVolunteer(email, password));

    public Task<bool> ChangeVolunteerPassword(int id, string currentPassword, string newPassword)
        => actors.Ask<bool>(actors.Volunteers, new ChangeVolunteerPassword(id, currentPassword, newPassword));

    // Events
    public Task<List<ShelterEvent>> GetEvents(DateTime? from = null, DateTime? to = null)
        => actors.Ask<List<ShelterEvent>>(actors.Volunteers, new GetAllEvents(from, to));

    public Task<ShelterEvent?> GetEvent(int id)
        => actors.Ask<ShelterEvent?>(actors.Volunteers, new GetEventById(id));

    public Task<ShelterEvent> CreateEvent(CreateEvent cmd)
        => actors.Ask<ShelterEvent>(actors.Volunteers, cmd);

    public Task<ShelterEvent?> UpdateEvent(UpdateEvent cmd)
        => actors.Ask<ShelterEvent?>(actors.Volunteers, cmd);

    public Task<bool> DeleteEvent(int id)
        => actors.Ask<bool>(actors.Volunteers, new DeleteEvent(id));
}
