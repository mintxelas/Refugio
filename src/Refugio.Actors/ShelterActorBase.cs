using Akka.Actor;
using Microsoft.Extensions.DependencyInjection;

namespace Refugio.Actors;

/// <summary>
/// Base for the per-area shelter actors. Each message runs the area's application service
/// inside a fresh DI scope (one scope = one unit of work, mirroring an HTTP request).
/// Command: awaited in the mailbox — writes to an area are serialized.
/// Query: dispatched to the thread pool and piped back — reads stay concurrent.
/// </summary>
public abstract class ShelterActorBase<TService>(IServiceScopeFactory scopeFactory) : ReceiveActor
    where TService : notnull
{
    protected void Command<TMessage>(Func<TService, TMessage, Task<object?>> handle) =>
        ReceiveAsync<TMessage>(async message =>
        {
            try
            {
                Sender.Tell(await RunInScopeAsync(message, handle) ?? NullReply.Instance);
            }
            catch (Exception ex)
            {
                Sender.Tell(new Status.Failure(ex));
            }
        });

    protected void Query<TMessage>(Func<TService, TMessage, Task<object?>> handle) =>
        Receive<TMessage>(message => RunInScopeAsync(message, handle).PipeTo(
            Sender,
            success: reply => reply ?? NullReply.Instance,
            failure: ex => new Status.Failure(ex)));

    private async Task<object?> RunInScopeAsync<TMessage>(TMessage message, Func<TService, TMessage, Task<object?>> handle)
    {
        using var scope = scopeFactory.CreateScope();
        return await handle(scope.ServiceProvider.GetRequiredService<TService>(), message);
    }
}
