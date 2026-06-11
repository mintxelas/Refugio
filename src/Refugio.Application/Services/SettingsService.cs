using Refugio.Application.Contracts;
using Refugio.Application.Mapping;
using Refugio.Domain.Common;
using Refugio.Domain.Entities;
using Refugio.Domain.Repositories;

namespace Refugio.Application.Services;

public interface ISettingsService
{
    Task<ShelterSettingsDto> GetAsync();
    Task<ShelterSettingsDto> UpdateAsync(UpdateSettingsRequest request);
    Task<bool> SetLogoAsync(string logoUrl);
}

public class SettingsService(ISettingsRepository settings, IUnitOfWork unitOfWork) : ISettingsService
{
    public async Task<ShelterSettingsDto> GetAsync()
        => (await GetOrCreateAsync()).ToDto();

    public async Task<ShelterSettingsDto> UpdateAsync(UpdateSettingsRequest request)
    {
        var current = await GetOrCreateAsync();
        current.Update(request.Name, request.Phrase);
        await unitOfWork.SaveChangesAsync();
        return current.ToDto();
    }

    public async Task<bool> SetLogoAsync(string logoUrl)
    {
        var current = await GetOrCreateAsync();
        current.SetLogo(logoUrl);
        await unitOfWork.SaveChangesAsync();
        return true;
    }

    private async Task<ShelterSettings> GetOrCreateAsync()
    {
        var current = await settings.GetAsync();
        if (current is null)
        {
            current = ShelterSettings.CreateDefault();
            settings.Add(current);
            await unitOfWork.SaveChangesAsync();
        }
        return current;
    }
}
