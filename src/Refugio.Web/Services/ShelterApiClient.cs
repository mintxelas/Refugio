using Refugio.Application.Actors;
using Refugio.Application.Messages;
using Refugio.Application.Services;
using Refugio.Domain.Entities;

namespace Refugio.Web.Services;

// Thin façade over the actor system. Every call routes on the message's marker
// interface (see ShelterActorService.Ask), so no actor ref is named here.
public class ShelterApiClient(ShelterActorService actors)
{
    // Dogs
    public Task<List<Dog>> GetDogs(string? search = null, DogStatus? status = null)
        => actors.Ask<List<Dog>>(new GetAllDogs(search, status));

    public Task<Page<Dog>> GetDogsPaged(string? search, DogStatus? status, int page, int pageSize = 20)
        => actors.Ask<Page<Dog>>(new GetDogsPaged(search, status, page, pageSize));

    public Task<Dog?> GetDog(int id)
        => actors.Ask<Dog?>(new GetDogById(id));

    public Task<Dog> CreateDog(CreateDog cmd)
        => actors.Ask<Dog>(cmd);

    public Task<Dog?> UpdateDog(UpdateDog cmd)
        => actors.Ask<Dog?>(cmd);

    public Task<bool> DeleteDog(int id)
        => actors.Ask<bool>(new DeleteDog(id));

    public Task<bool> UpdateDogPhoto(int id, string? photoUrl)
        => actors.Ask<bool>(new UpdateDogPhoto(id, photoUrl));

    // Medical
    public Task<List<MedicalRecord>> GetMedicalRecords(int dogId)
        => actors.Ask<List<MedicalRecord>>(new GetMedicalRecords(dogId));

    public Task<MedicalRecord?> GetMedicalRecord(int id)
        => actors.Ask<MedicalRecord?>(new GetMedicalRecordById(id));

    public Task<MedicalRecord> CreateMedicalRecord(CreateMedicalRecord cmd)
        => actors.Ask<MedicalRecord>(cmd);

    public Task<MedicalRecord?> UpdateMedicalRecord(UpdateMedicalRecord cmd)
        => actors.Ask<MedicalRecord?>(cmd);

    public Task<bool> DeleteMedicalRecord(int id)
        => actors.Ask<bool>(new DeleteMedicalRecord(id));

    // Medications
    public Task<List<Medication>> GetMedications(int dogId)
        => actors.Ask<List<Medication>>(new GetMedications(dogId));

    public Task<Medication?> GetMedication(int id)
        => actors.Ask<Medication?>(new GetMedicationById(id));

    public Task<Medication> CreateMedication(CreateMedication cmd)
        => actors.Ask<Medication>(cmd);

    public Task<Medication?> UpdateMedication(UpdateMedication cmd)
        => actors.Ask<Medication?>(cmd);

    public Task<bool> DeleteMedication(int id)
        => actors.Ask<bool>(new DeleteMedication(id));

    // Dashboard
    public Task<DashboardStats> GetDashboardStats()
        => actors.Ask<DashboardStats>(new GetDashboardStats());

    // Deleted / Admin
    public Task<List<Dog>> GetDeletedDogs()
        => actors.Ask<List<Dog>>(new GetDeletedDogs());

    public Task<bool> RestoreDog(int id)
        => actors.Ask<bool>(new RestoreDog(id));

    public Task<Dog?> GetDeletedDogById(int id)
        => actors.Ask<Dog?>(new GetDeletedDogById(id));

    public Task<List<Adoption>> GetDeletedAdoptions()
        => actors.Ask<List<Adoption>>(new GetDeletedAdoptions());

    public Task<bool> RestoreAdoption(int id)
        => actors.Ask<bool>(new RestoreAdoption(id));

    public Task<Adoption?> GetDeletedAdoptionById(int id)
        => actors.Ask<Adoption?>(new GetDeletedAdoptionById(id));

    public Task<AdoptionConversionStats> GetAdoptionConversionStats(int year)
        => actors.Ask<AdoptionConversionStats>(new GetAdoptionConversionStats(year));

    public Task<ShelterStayStats> GetShelterStayStats()
        => actors.Ask<ShelterStayStats>(new GetShelterStayStats());

    public Task<List<Volunteer>> GetDeletedVolunteers()
        => actors.Ask<List<Volunteer>>(new GetDeletedVolunteers());

    public Task<bool> RestoreVolunteer(int id)
        => actors.Ask<bool>(new RestoreVolunteer(id));

    public Task<Volunteer?> GetDeletedVolunteerById(int id)
        => actors.Ask<Volunteer?>(new GetDeletedVolunteerById(id));

    public Task<List<Donation>> GetDeletedDonations()
        => actors.Ask<List<Donation>>(new GetDeletedDonations());

    public Task<bool> RestoreDonation(int id)
        => actors.Ask<bool>(new RestoreDonation(id));

    public Task<Donation?> GetDeletedDonationById(int id)
        => actors.Ask<Donation?>(new GetDeletedDonationById(id));

    public Task<List<Expense>> GetDeletedExpenses()
        => actors.Ask<List<Expense>>(new GetDeletedExpenses());

    public Task<bool> RestoreExpense(int id)
        => actors.Ask<bool>(new RestoreExpense(id));

    public Task<Expense?> GetDeletedExpenseById(int id)
        => actors.Ask<Expense?>(new GetDeletedExpenseById(id));

    public Task<List<MedicalRecord>> GetDeletedMedicalRecords()
        => actors.Ask<List<MedicalRecord>>(new GetDeletedMedicalRecords());

    public Task<bool> RestoreMedicalRecord(int id)
        => actors.Ask<bool>(new RestoreMedicalRecord(id));

    public Task<MedicalRecord?> GetDeletedMedicalRecordById(int id)
        => actors.Ask<MedicalRecord?>(new GetDeletedMedicalRecordById(id));

    public Task<List<Medication>> GetDeletedMedications()
        => actors.Ask<List<Medication>>(new GetDeletedMedications());

    public Task<bool> RestoreMedication(int id)
        => actors.Ask<bool>(new RestoreMedication(id));

    public Task<Medication?> GetDeletedMedicationById(int id)
        => actors.Ask<Medication?>(new GetDeletedMedicationById(id));

    // Tasks
    public Task<List<ShelterTask>> GetTasks(bool includeCompleted = false)
        => actors.Ask<List<ShelterTask>>(new GetAllTasks(includeCompleted));

    public Task<ShelterTask> CreateTask(CreateTask cmd)
        => actors.Ask<ShelterTask>(cmd);

    public Task<bool> CompleteTask(int id)
        => actors.Ask<bool>(new CompleteTask(id));

    public Task<bool> DeleteTask(int id)
        => actors.Ask<bool>(new DeleteTask(id));

    // Adoptions
    public Task<List<Adoption>> GetAdoptions(AdoptionStatus? status = null)
        => actors.Ask<List<Adoption>>(new GetAllAdoptions(status));

    public Task<Page<Adoption>> GetAdoptionsPaged(AdoptionStatus? status, int page, int pageSize = 25)
        => actors.Ask<Page<Adoption>>(new GetAdoptionsPaged(status, page, pageSize));

    public Task<Adoption?> GetAdoption(int id)
        => actors.Ask<Adoption?>(new GetAdoptionById(id));

    public Task<Adoption> CreateAdoption(CreateAdoption cmd)
        => actors.Ask<Adoption>(cmd);

    public Task<Adoption?> UpdateAdoption(UpdateAdoption cmd)
        => actors.Ask<Adoption?>(cmd);

    public Task<Adoption?> UpdateAdoptionStatus(int id, AdoptionStatus newStatus)
        => actors.Ask<Adoption?>(new UpdateAdoptionStatus(id, newStatus, null));

    // Finance
    public Task<List<Donation>> GetDonations()
        => actors.Ask<List<Donation>>(new GetAllDonations());

    public Task<Page<Donation>> GetDonationsPaged(int page, int pageSize = 25)
        => actors.Ask<Page<Donation>>(new GetDonationsPaged(page, pageSize));

    public Task<Donation?> GetDonation(int id)
        => actors.Ask<Donation?>(new GetDonationById(id));

    public Task<Donation> CreateDonation(CreateDonation cmd)
        => actors.Ask<Donation>(cmd);

    public Task<Donation?> UpdateDonation(UpdateDonation cmd)
        => actors.Ask<Donation?>(cmd);

    public Task<bool> DeleteDonation(int id)
        => actors.Ask<bool>(new DeleteDonation(id));

    public Task<List<Expense>> GetExpenses()
        => actors.Ask<List<Expense>>(new GetAllExpenses());

    public Task<Page<Expense>> GetExpensesPaged(int page, int pageSize = 25)
        => actors.Ask<Page<Expense>>(new GetExpensesPaged(page, pageSize));

    public Task<Expense?> GetExpense(int id)
        => actors.Ask<Expense?>(new GetExpenseById(id));

    public Task<Expense> CreateExpense(CreateExpense cmd)
        => actors.Ask<Expense>(cmd);

    public Task<Expense?> UpdateExpense(UpdateExpense cmd)
        => actors.Ask<Expense?>(cmd);

    public Task<bool> DeleteExpense(int id)
        => actors.Ask<bool>(new DeleteExpense(id));

    public Task<FinanceSummary> GetFinanceSummary(int year)
        => actors.Ask<FinanceSummary>(new GetFinanceSummary(year));

    // Goals
    public Task<List<Goal>> GetGoals()
        => actors.Ask<List<Goal>>(new GetAllGoals());

    public Task<Goal?> GetGoal(int id)
        => actors.Ask<Goal?>(new GetGoalById(id));

    public Task<Goal> CreateGoal(CreateGoal cmd)
        => actors.Ask<Goal>(cmd);

    public Task<Goal?> UpdateGoal(UpdateGoal cmd)
        => actors.Ask<Goal?>(cmd);

    public Task<bool> DeleteGoal(int id)
        => actors.Ask<bool>(new DeleteGoal(id));

    public Task<List<Goal>> GetDeletedGoals()
        => actors.Ask<List<Goal>>(new GetDeletedGoals());

    public Task<bool> RestoreGoal(int id)
        => actors.Ask<bool>(new RestoreGoal(id));

    public Task<Goal?> GetDeletedGoalById(int id)
        => actors.Ask<Goal?>(new GetDeletedGoalById(id));

    // Volunteers
    public Task<Volunteer?> GetVolunteer(int id)
        => actors.Ask<Volunteer?>(new GetVolunteerById(id));

    public Task<List<Volunteer>> GetVolunteers(VolunteerStatus? status = null)
        => actors.Ask<List<Volunteer>>(new GetAllVolunteers(status));

    public Task<Page<Volunteer>> GetVolunteersPaged(VolunteerStatus? status, int page, int pageSize = 25)
        => actors.Ask<Page<Volunteer>>(new GetVolunteersPaged(status, page, pageSize));

    public Task<VolunteerCounts> GetVolunteerCounts()
        => actors.Ask<VolunteerCounts>(new GetVolunteerCounts());

    public Task<Volunteer> CreateVolunteer(CreateVolunteer cmd)
        => actors.Ask<Volunteer>(cmd);

    public Task<Volunteer?> UpdateVolunteer(UpdateVolunteer cmd)
        => actors.Ask<Volunteer?>(cmd);

    public Task<Volunteer?> UpdateVolunteerStatus(int id, VolunteerStatus status)
        => actors.Ask<Volunteer?>(new UpdateVolunteerStatus(id, status));

    public Task<Volunteer?> LoginVolunteer(string email, string password)
        => actors.Ask<Volunteer?>(new LoginVolunteer(email, password));

    public Task<bool> ChangeVolunteerPassword(int id, string currentPassword, string newPassword)
        => actors.Ask<bool>(new ChangeVolunteerPassword(id, currentPassword, newPassword));

    // Events
    public Task<List<ShelterEvent>> GetEvents(DateTime? from = null, DateTime? to = null)
        => actors.Ask<List<ShelterEvent>>(new GetAllEvents(from, to));

    public Task<ShelterEvent?> GetEvent(int id)
        => actors.Ask<ShelterEvent?>(new GetEventById(id));

    public Task<ShelterEvent> CreateEvent(CreateEvent cmd)
        => actors.Ask<ShelterEvent>(cmd);

    public Task<ShelterEvent?> UpdateEvent(UpdateEvent cmd)
        => actors.Ask<ShelterEvent?>(cmd);

    public Task<bool> DeleteEvent(int id)
        => actors.Ask<bool>(new DeleteEvent(id));
}
