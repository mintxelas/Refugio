using Refugio.Application.Contracts;
using Refugio.Domain.Entities;

namespace Refugio.Application.Mapping;

/// <summary>Hand-written entity → DTO mapping. DTO property names match the wire contract.</summary>
public static class DtoMapping
{
    public static DogDto ToDto(this Dog dog) => new(
        dog.Id, dog.Name, dog.Breed, dog.AgeMonths, dog.Gender, dog.Status,
        dog.PhotoUrl, dog.Traits, dog.Notes, dog.ArrivalDate, dog.WeightKg, dog.DeletedAt,
        dog.MedicalRecords.Select(r => r.ToDto(includeDog: false)).ToList(),
        dog.Medications.Select(m => m.ToDto(includeDog: false)).ToList(),
        dog.Photos.Select(p => p.ToDto()).ToList());

    public static MedicalRecordDto ToDto(this MedicalRecord record, bool includeDog = true) => new(
        record.Id, record.DogId, record.VisitDate, record.VetName, record.Diagnosis,
        record.Treatment, record.Notes, record.NextVisitDate, record.DeletedAt,
        includeDog && record.Dog is not null ? record.Dog.ToDto() : null);

    public static MedicationDto ToDto(this Medication medication, bool includeDog = true) => new(
        medication.Id, medication.DogId, medication.Name, medication.Dosage, medication.Frequency,
        medication.StartDate, medication.EndDate, medication.IsActive, medication.DeletedAt,
        includeDog && medication.Dog is not null ? medication.Dog.ToDto() : null);

    public static DogPhotoDto ToDto(this DogPhoto photo)
        => new(photo.Id, photo.DogId, photo.Url, photo.IsDefault, photo.UploadedAt);

    public static AdoptionDto ToDto(this Adoption adoption) => new(
        adoption.Id, adoption.DogId, adoption.ApplicantName, adoption.ApplicantEmail,
        adoption.ApplicantPhone, adoption.Type, adoption.Status, adoption.Notes,
        adoption.CreatedAt, adoption.UpdatedAt, adoption.DeletedAt,
        adoption.Dog is not null ? adoption.Dog.ToDto() : null,
        adoption.Photos.Select(p => p.ToDto()).ToList());

    public static AdoptionPhotoDto ToDto(this AdoptionPhoto photo)
        => new(photo.Id, photo.AdoptionId, photo.Url, photo.UploadedAt);

    public static VolunteerDto ToDto(this Volunteer volunteer) => new(
        volunteer.Id, volunteer.Name, volunteer.Email, volunteer.Phone, volunteer.Role,
        volunteer.Status, volunteer.JoinDate, volunteer.Notes, volunteer.CanLogin,
        volunteer.PreferredLanguage, volunteer.PhotoUrl, volunteer.DeletedAt);

    public static DonationDto ToDto(this Donation donation) => new(
        donation.Id, donation.DonorName, donation.Amount, donation.Date,
        donation.Category, donation.Notes, donation.DeletedAt);

    public static ExpenseDto ToDto(this Expense expense) => new(
        expense.Id, expense.Description, expense.Amount, expense.Date,
        expense.Category, expense.Notes, expense.DeletedAt,
        expense.Photos.Select(p => p.ToDto()).ToList());

    public static ExpensePhotoDto ToDto(this ExpensePhoto photo)
        => new(photo.Id, photo.ExpenseId, photo.Url, photo.UploadedAt);

    public static GoalDto ToDto(this Goal goal) => new(
        goal.Id, goal.Title, goal.Description, goal.TargetAmount, goal.CurrentAmount,
        goal.Deadline, goal.CreatedAt, goal.DeletedAt);

    public static ShelterTaskDto ToDto(this ShelterTask task) => new(
        task.Id, task.Title, task.Notes, task.DueDateTime, task.IsCompleted,
        task.Location, task.AssignedVolunteerId, task.AssignedVolunteer?.ToDto());

    public static ShelterEventDto ToDto(this ShelterEvent ev) => new(
        ev.Id, ev.Title, ev.StartDateTime, ev.EndDateTime,
        ev.Location, ev.Description, ev.EventType, ev.AssignedVolunteers);

    public static ShelterSettingsDto ToDto(this ShelterSettings settings)
        => new(settings.Id, settings.Name, settings.Phrase, settings.LogoUrl);

    public static Domain.Common.Page<TDto> ToDto<TEntity, TDto>(
        this Domain.Common.Page<TEntity> page, Func<TEntity, TDto> map)
        => new(page.Items.Select(map).ToList(), page.TotalCount, page.PageNumber, page.PageSize);
}
