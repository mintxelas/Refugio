namespace Refugio.Application.Messages;

/// <summary>
/// Marker interfaces that bind each request message to the actor that owns it.
/// <see cref="Refugio.Application.Services.ShelterActorService.Ask{T}(IShelterMessage, System.TimeSpan?)"/>
/// dispatches purely on these markers, so call sites never name an actor ref.
/// Response/page/stat records are NOT markers — only request messages route.
/// </summary>
public interface IShelterMessage { }

public interface IDogMessage : IShelterMessage { }
public interface IFinanceMessage : IShelterMessage { }
public interface IAdoptionMessage : IShelterMessage { }
public interface IVolunteerMessage : IShelterMessage { }
public interface ITaskMessage : IShelterMessage { }
