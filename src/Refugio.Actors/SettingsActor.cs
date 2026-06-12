using Microsoft.Extensions.DependencyInjection;
using Refugio.Actors.Messages;
using Refugio.Application.Contracts;
using Refugio.Application.Services;

namespace Refugio.Actors;

/// <summary>
/// Routes shelter-settings operations to ISettingsService. GetSettings is a Command on
/// purpose: the service creates the default row when missing, so serializing reads through
/// the mailbox removes the duplicate-default race two parallel first reads could hit.
/// </summary>
public sealed class SettingsActor : ShelterActorBase<ISettingsService>
{
    public SettingsActor(IServiceScopeFactory scopeFactory) : base(scopeFactory)
    {
        Command<GetSettings>(async (s, _) => await s.GetAsync());
        Command<UpdateSettingsRequest>(async (s, m) => await s.UpdateAsync(m));
        Command<SetLogo>(async (s, m) => await s.SetLogoAsync(m.LogoUrl));
    }
}
