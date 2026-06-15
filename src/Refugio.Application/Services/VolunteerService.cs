using Refugio.Application.Contracts;
using Refugio.Application.Mapping;
using Refugio.Domain.Common;
using Refugio.Domain.Entities;
using Refugio.Domain.Helpers;
using Refugio.Domain.Repositories;

namespace Refugio.Application.Services;

public interface IVolunteerService
{
    Task<List<VolunteerDto>> GetVolunteersAsync(VolunteerStatus? status = null);
    Task<Page<VolunteerDto>> GetVolunteersPagedAsync(VolunteerStatus? status, int page, int pageSize = 25);
    Task<VolunteerDto?> GetVolunteerAsync(int id);
    Task<VolunteerDto> RegisterAsync(CreateVolunteerRequest request);
    Task<VolunteerDto?> UpdateAsync(UpdateVolunteerRequest request);
    Task<VolunteerDto?> ChangeStatusAsync(int id, VolunteerStatus status);
    Task<VolunteerDto?> LoginAsync(string email, string password);
    Task<bool> ChangePasswordAsync(int id, string currentPassword, string newPassword);
    Task<bool> SetPhotoAsync(int id, string? photoUrl);
    Task<bool> DeleteAsync(int id);
    Task<bool> RestoreAsync(int id);
    Task<bool> PurgeAsync(int id);
    Task<List<VolunteerDto>> GetDeletedAsync();
    Task<VolunteerDto?> GetDeletedByIdAsync(int id);
}

public class VolunteerService(IVolunteerRepository volunteers, IUnitOfWork unitOfWork)
    : ShelterServiceBase(unitOfWork), IVolunteerService
{
    public async Task<List<VolunteerDto>> GetVolunteersAsync(VolunteerStatus? status = null)
        => (await volunteers.GetAllAsync(status)).Select(v => v.ToDto()).ToList();

    public async Task<Page<VolunteerDto>> GetVolunteersPagedAsync(VolunteerStatus? status, int page, int pageSize = 25)
        => (await volunteers.GetPagedAsync(status, page, pageSize)).ToDto(v => v.ToDto());

    public async Task<VolunteerDto?> GetVolunteerAsync(int id)
        => (await volunteers.GetAsync(id))?.ToDto();

    public async Task<VolunteerDto> RegisterAsync(CreateVolunteerRequest request)
    {
        var volunteer = Volunteer.Register(
            request.Name, request.Email, request.Phone, request.Role, request.Notes,
            request.CanLogin, request.Password, request.PreferredLanguage);
        volunteers.Add(volunteer);
        await UnitOfWork.SaveChangesAsync();
        return volunteer.ToDto();
    }

    public async Task<VolunteerDto?> UpdateAsync(UpdateVolunteerRequest request)
    {
        var volunteer = await volunteers.GetAsync(request.Id);
        if (volunteer is null) return null;
        volunteer.Update(
            request.Name, request.Email, request.Phone, request.Role, request.Notes,
            request.Status, request.CanLogin, request.NewPassword, request.PreferredLanguage);
        await UnitOfWork.SaveChangesAsync();
        return volunteer.ToDto();
    }

    public async Task<VolunteerDto?> ChangeStatusAsync(int id, VolunteerStatus status)
    {
        var volunteer = await volunteers.GetAsync(id);
        if (volunteer is null) return null;
        volunteer.ChangeStatus(status);
        await UnitOfWork.SaveChangesAsync();
        return volunteer.ToDto();
    }

    public async Task<VolunteerDto?> LoginAsync(string email, string password)
    {
        var volunteer = await volunteers.FindLoginCandidateAsync(email);
        if (volunteer is null)
        {
            // constant-time dummy to prevent email enumeration via timing
            PasswordHelper.Verify(password, PasswordHelper.DummyHash);
            return null;
        }
        return volunteer.VerifyPassword(password) ? volunteer.ToDto() : null;
    }

    public async Task<bool> ChangePasswordAsync(int id, string currentPassword, string newPassword)
    {
        var volunteer = await volunteers.GetAsync(id);
        if (volunteer is null || !volunteer.ChangePassword(currentPassword, newPassword)) return false;
        await UnitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SetPhotoAsync(int id, string? photoUrl)
    {
        var volunteer = await volunteers.GetAsync(id);
        if (volunteer is null) return false;
        volunteer.SetPhotoUrl(photoUrl);
        await UnitOfWork.SaveChangesAsync();
        return true;
    }

    public Task<bool> DeleteAsync(int id) => SoftDeleteAsync(() => volunteers.GetAsync(id), volunteers.Remove);
    public Task<bool> RestoreAsync(int id) => RestoreAsync(() => volunteers.GetDeletedByIdAsync(id));
    public Task<bool> PurgeAsync(int id) => PurgeAsync(() => volunteers.GetDeletedByIdAsync(id), volunteers.RemovePermanently);

    public async Task<List<VolunteerDto>> GetDeletedAsync()
        => (await volunteers.GetDeletedAsync()).Select(v => v.ToDto()).ToList();

    public async Task<VolunteerDto?> GetDeletedByIdAsync(int id)
        => (await volunteers.GetDeletedByIdAsync(id))?.ToDto();
}
