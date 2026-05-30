namespace Refugio.Domain.Entities;

/// <summary>
/// Implemented by every entity that participates in soft delete. Lets the actor
/// layer write generic delete/restore/get-deleted handlers (see ShelterActorBase)
/// and EF global query filters key off a single shape.
/// </summary>
public interface ISoftDeletable
{
    int Id { get; set; }
    DateTime? DeletedAt { get; set; }
}
