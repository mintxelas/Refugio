using Refugio.Domain.Common;

namespace Refugio.Domain.Entities;

public class AdoptionPhoto : Entity
{
    public int AdoptionId { get; private set; }
    public Adoption? Adoption { get; private set; }
    public string Url { get; private set; } = "";
    public DateTime UploadedAt { get; private set; } = DateTime.UtcNow;

    private AdoptionPhoto() { }

    public static AdoptionPhoto Create(int adoptionId, string url) => new()
    {
        AdoptionId = adoptionId,
        Url = url
    };
}
