namespace Refugio.Domain.Common;

/// <summary>Generic paged result used by repositories and the API alike.</summary>
public record Page<T>(List<T> Items, int TotalCount, int PageNumber, int PageSize);
