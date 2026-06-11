using Refugio.Domain.Common;

namespace Refugio.Domain.Entities;

/// <summary>Child entity of the Dog aggregate: one gallery image, at most one default per dog.</summary>
public class DogPhoto : Entity
{
    public int DogId { get; private set; }
    public Dog? Dog { get; private set; }
    public string Url { get; private set; } = "";
    public bool IsDefault { get; private set; }
    public DateTime UploadedAt { get; private set; } = DateTime.UtcNow;

    private DogPhoto() { }

    public static DogPhoto Create(int dogId, string url, bool isDefault = false) => new()
    {
        DogId = dogId,
        Url = url,
        IsDefault = isDefault
    };

    public void SetDefault(bool isDefault) => IsDefault = isDefault;
}
