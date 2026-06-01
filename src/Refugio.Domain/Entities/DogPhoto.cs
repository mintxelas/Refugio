namespace Refugio.Domain.Entities;

public class DogPhoto : ISoftDeletable
{
    public int Id { get; set; }
    public int DogId { get; set; }
    public Dog? Dog { get; set; }
    public string Url { get; set; } = "";
    public bool IsDefault { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public DateTime? DeletedAt { get; set; }
}
