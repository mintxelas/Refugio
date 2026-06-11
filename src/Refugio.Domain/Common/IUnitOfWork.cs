namespace Refugio.Domain.Common;

/// <summary>
/// Commits a business transaction. The infrastructure implementation saves all tracked
/// changes and then dispatches the domain events raised by the saved aggregates.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
