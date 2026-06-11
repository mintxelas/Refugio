namespace Refugio.Domain.Common;

/// <summary>
/// Implemented by every entity that participates in soft delete. EF global query
/// filters and the SoftDeleteInterceptor key off this single shape.
/// </summary>
public interface ISoftDeletable
{
    int Id { get; set; }
    DateTime? DeletedAt { get; set; }
}
