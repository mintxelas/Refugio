namespace Refugio.Application.Messages;

public record GetSettings() : ISettingsMessage;
public record UpdateSettings(string Name, string? Phrase) : ISettingsMessage;
public record UpdateSettingsLogo(string LogoUrl) : ISettingsMessage;
