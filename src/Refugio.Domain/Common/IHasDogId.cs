namespace Refugio.Domain.Common;

/// <summary>Implemented by child entities of the Dog aggregate (medical records, medications).</summary>
public interface IHasDogId
{
    int DogId { get; }
}
