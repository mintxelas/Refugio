namespace Refugio.Actors.Messages;

// Event-area actor messages. Schedule/Update reuse the request records.
public sealed record GetEvents(DateTime? From, DateTime? To);
public sealed record GetEvent(int Id);
public sealed record DeleteEvent(int Id);
