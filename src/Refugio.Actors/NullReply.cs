namespace Refugio.Actors;

/// <summary>Reply sentinel for null results — an actor cannot Tell(null).</summary>
public sealed class NullReply
{
    public static readonly NullReply Instance = new();
    private NullReply() { }
}
