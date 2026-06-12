using Akka.Actor;

namespace Refugio.Actors;

public static class ActorAsk
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    /// <summary>Ask an area actor, unwrapping the NullReply sentinel back to null.</summary>
    public static async Task<TReply?> AskFor<TReply>(this IActorRef actor, object message, CancellationToken ct = default)
    {
        var reply = await actor.Ask<object>(message, Timeout, ct);
        return reply switch
        {
            NullReply => default,
            TReply typed => typed,
            _ => throw new InvalidOperationException(
                $"Unexpected reply '{reply.GetType().Name}' for message '{message.GetType().Name}'."),
        };
    }

    /// <summary>Ask an area actor for a reply the operation guarantees to be non-null.</summary>
    public static async Task<TReply> AskRequired<TReply>(this IActorRef actor, object message, CancellationToken ct = default)
        where TReply : notnull
        => await actor.AskFor<TReply>(message, ct)
           ?? throw new InvalidOperationException($"Null reply for message '{message.GetType().Name}'.");
}
