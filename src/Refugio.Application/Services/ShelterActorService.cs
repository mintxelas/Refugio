using Akka.Actor;
using Akka.DependencyInjection;
using Refugio.Application.Actors;

namespace Refugio.Application.Services;

public class ShelterActorService
{
    private readonly ActorSystem _system;
    public IActorRef Supervisor { get; }
    public IActorRef Dogs { get; }
    public IActorRef Adoptions { get; }
    public IActorRef Finance { get; }
    public IActorRef Volunteers { get; }
    public IActorRef Tasks { get; }

    public ShelterActorService(ActorSystem system, IServiceProvider sp)
    {
        _system = system;
        var resolver = DependencyResolver.For(system);
        Supervisor = system.ActorOf(resolver.Props<ShelterSupervisorActor>(), "shelter");

        // Give supervisor time to create child actors before resolving them
        Task.Delay(500).Wait();

        // Resolve child actors by path
        Dogs = system.ActorSelection("/user/shelter/dogs").ResolveOne(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
        Adoptions = system.ActorSelection("/user/shelter/adoptions").ResolveOne(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
        Finance = system.ActorSelection("/user/shelter/finance").ResolveOne(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
        Volunteers = system.ActorSelection("/user/shelter/volunteers").ResolveOne(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
        Tasks = system.ActorSelection("/user/shelter/tasks").ResolveOne(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
    }

    public Task<T> Ask<T>(IActorRef actor, object message, TimeSpan? timeout = null)
        => actor.Ask<T>(message, timeout ?? TimeSpan.FromSeconds(10));
}
