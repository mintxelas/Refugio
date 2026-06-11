namespace Refugio.Domain.Common;

/// <summary>Marker for domain events raised by aggregates and dispatched after persistence.</summary>
public interface IDomainEvent { }

/// <summary>Marker for aggregate roots — the only entities reachable through repositories.</summary>
public interface IAggregateRoot { }

/// <summary>
/// Base class for all domain entities. Carries identity, soft-delete state and the
/// domain-event collection. Events are raised by behavior methods and dequeued by the
/// unit of work after a successful save.
/// </summary>
public abstract class Entity : ISoftDeletable
{
    public int Id { get; set; }
    public DateTime? DeletedAt { get; set; }

    private readonly List<IDomainEvent> _domainEvents = [];

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <summary>Returns and clears the pending domain events (exposed as a method so EF ignores it).</summary>
    public IReadOnlyList<IDomainEvent> DequeueDomainEvents()
    {
        if (_domainEvents.Count == 0) return [];
        var events = _domainEvents.ToList();
        _domainEvents.Clear();
        return events;
    }

    /// <summary>Brings a soft-deleted entity back to life.</summary>
    public void Restore() => DeletedAt = null;
}
