namespace Refugio.Application.Contracts;

public record ShelterSettingsDto(int Id, string Name, string? Phrase, string? LogoUrl);

public record UpdateSettingsRequest(string Name, string? Phrase);
