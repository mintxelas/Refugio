namespace Refugio.Application.Messages;

/// <summary>
/// Generic paged result. Replaces the former per-entity page records
/// (DogPage, DonationPage, …). Property names are unchanged so existing
/// consumers (.Items / .TotalCount) keep working.
/// </summary>
public record Page<T>(List<T> Items, int TotalCount, int PageNumber, int PageSize);
