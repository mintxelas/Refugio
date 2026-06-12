namespace Refugio.Actors.Messages;

// Settings-area actor messages. Update reuses UpdateSettingsRequest.
public sealed record GetSettings;
public sealed record SetLogo(string LogoUrl);
