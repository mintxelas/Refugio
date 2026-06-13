using Refugio.Application.Contracts;
using Refugio.Application.Mapping;
using Refugio.Domain.Common;
using Refugio.Domain.Entities;
using Refugio.Domain.Repositories;

namespace Refugio.Application.Services;

public interface IAdoptionService
{
    Task<List<AdoptionDto>> GetAdoptionsAsync(AdoptionStatus? status = null);
    Task<Page<AdoptionDto>> GetAdoptionsPagedAsync(AdoptionStatus? status, int page, int pageSize = 25);
    Task<AdoptionDto?> GetAdoptionAsync(int id);
    Task<AdoptionDto> SubmitAsync(CreateAdoptionRequest request);
    Task<AdoptionDto?> UpdateAsync(UpdateAdoptionRequest request);
    Task<AdoptionDto?> ChangeStatusAsync(UpdateAdoptionStatusRequest request);
    Task<AdoptionDto?> AdvanceAsync(int id);
    Task<bool> DeleteAsync(int id);
    Task<bool> RestoreAsync(int id);
    Task<bool> PurgeAsync(int id);
    Task<List<AdoptionDto>> GetDeletedAsync();
    Task<AdoptionDto?> GetDeletedByIdAsync(int id);

    // Photos
    Task<List<AdoptionPhotoDto>> GetAdoptionPhotosAsync(int adoptionId);
    Task<AdoptionPhotoDto?> AddAdoptionPhotoAsync(int adoptionId, string url);
    Task<string?> RemoveAdoptionPhotoAsync(int photoId);
}

/// <summary>
/// Use cases for the adoption pipeline. Status changes go through the aggregate's
/// ChangeStatus, which raises AdoptionStatusChanged → applicant email after save.
/// </summary>
public class AdoptionService(IAdoptionRepository adoptions, IUnitOfWork unitOfWork)
    : ShelterServiceBase(unitOfWork), IAdoptionService
{
    public async Task<List<AdoptionDto>> GetAdoptionsAsync(AdoptionStatus? status = null)
        => (await adoptions.GetAllAsync(status)).Select(a => a.ToDto()).ToList();

    public async Task<Page<AdoptionDto>> GetAdoptionsPagedAsync(AdoptionStatus? status, int page, int pageSize = 25)
        => (await adoptions.GetPagedAsync(status, page, pageSize)).ToDto(a => a.ToDto());

    public async Task<AdoptionDto?> GetAdoptionAsync(int id)
        => (await adoptions.GetWithDogAsync(id))?.ToDto();

    public async Task<AdoptionDto> SubmitAsync(CreateAdoptionRequest request)
    {
        var adoption = Adoption.Submit(
            request.DogId, request.ApplicantName, request.ApplicantEmail, request.ApplicantPhone,
            request.Type, request.Notes);
        adoptions.Add(adoption);
        await UnitOfWork.SaveChangesAsync();
        return adoption.ToDto();
    }

    public async Task<AdoptionDto?> UpdateAsync(UpdateAdoptionRequest request)
    {
        var adoption = await adoptions.GetAsync(request.Id);
        if (adoption is null) return null;
        adoption.UpdateDetails(
            request.ApplicantName, request.ApplicantEmail, request.ApplicantPhone,
            request.Type, request.Status, request.Notes);
        await UnitOfWork.SaveChangesAsync();
        return adoption.ToDto();
    }

    public async Task<AdoptionDto?> ChangeStatusAsync(UpdateAdoptionStatusRequest request)
    {
        var adoption = await adoptions.GetAsync(request.Id);
        if (adoption is null) return null;
        adoption.ChangeStatus(request.NewStatus, request.Notes);
        await UnitOfWork.SaveChangesAsync();
        return adoption.ToDto();
    }

    public async Task<AdoptionDto?> AdvanceAsync(int id)
    {
        var adoption = await adoptions.GetAsync(id);
        if (adoption is null) return null;
        adoption.ChangeStatus(adoption.NextStatus());
        await UnitOfWork.SaveChangesAsync();
        return adoption.ToDto();
    }

    public Task<bool> DeleteAsync(int id) => SoftDeleteAsync(() => adoptions.GetAsync(id), adoptions.Remove);
    public Task<bool> RestoreAsync(int id) => RestoreAsync(() => adoptions.GetDeletedByIdAsync(id));
    public Task<bool> PurgeAsync(int id) => PurgeAsync(() => adoptions.GetDeletedByIdAsync(id), adoptions.RemovePermanently);

    public async Task<List<AdoptionDto>> GetDeletedAsync()
        => (await adoptions.GetDeletedAsync()).Select(a => a.ToDto()).ToList();

    public async Task<AdoptionDto?> GetDeletedByIdAsync(int id)
        => (await adoptions.GetDeletedByIdAsync(id))?.ToDto();

    // --- Photos ---

    public async Task<List<AdoptionPhotoDto>> GetAdoptionPhotosAsync(int adoptionId)
        => (await adoptions.GetPhotosAsync(adoptionId)).Select(p => p.ToDto()).ToList();

    public async Task<AdoptionPhotoDto?> AddAdoptionPhotoAsync(int adoptionId, string url)
    {
        if (await adoptions.GetAsync(adoptionId) is null) return null;
        var photo = AdoptionPhoto.Create(adoptionId, url);
        adoptions.AddPhoto(photo);
        await UnitOfWork.SaveChangesAsync();
        return photo.ToDto();
    }

    public async Task<string?> RemoveAdoptionPhotoAsync(int photoId)
    {
        var photo = await adoptions.GetPhotoAsync(photoId);
        if (photo is null) return null;
        var url = photo.Url;
        adoptions.RemovePhotoPermanently(photo);
        await UnitOfWork.SaveChangesAsync();
        return url;
    }
}
