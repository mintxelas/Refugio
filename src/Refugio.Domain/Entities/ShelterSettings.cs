using Refugio.Domain.Common;

namespace Refugio.Domain.Entities;

/// <summary>Single-row aggregate holding the shelter's branding (name, phrase, logo).</summary>
public class ShelterSettings : Entity, IAggregateRoot
{
    public string Name { get; private set; } = "Haven Sanctuary";
    public string? Phrase { get; private set; }
    public string? LogoUrl { get; private set; }

    private ShelterSettings() { }

    public static ShelterSettings CreateDefault(string name = "Haven Sanctuary", string? phrase = "City Main Branch") => new()
    {
        Name = name,
        Phrase = phrase
    };

    public void Update(string name, string? phrase)
    {
        Name = name;
        Phrase = phrase;
    }

    public void SetLogo(string logoUrl) => LogoUrl = logoUrl;
}
