using Akka.Actor;
using Akka.DependencyInjection;
using Refugio.Application.Actors;
using Refugio.Application.Messages;

namespace Refugio.Application.Services;

public class ShelterActorService
{
    private readonly ActorSystem _system;
    public IActorRef Supervisor { get; }
    private IActorRef Dogs { get; }
    private IActorRef Adoptions { get; }
    private IActorRef Finance { get; }
    private IActorRef Volunteers { get; }
    private IActorRef Tasks { get; }
    private IActorRef Settings { get; }

    public ShelterActorService(ActorSystem system, IServiceProvider sp)
    {
        _system = system;
        var resolver = DependencyResolver.For(system);
        Supervisor = system.ActorOf(resolver.Props<ShelterSupervisorActor>(), "shelter");

        // Block until supervisor has started — it responds to Identify only after its ctor runs,
        // which is when all child actors are registered in the hierarchy.
        Supervisor.Ask<ActorIdentity>(new Identify("probe"), TimeSpan.FromSeconds(10)).GetAwaiter().GetResult();

        Dogs = system.ActorSelection("/user/shelter/dogs").ResolveOne(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
        Adoptions = system.ActorSelection("/user/shelter/adoptions").ResolveOne(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
        Finance = system.ActorSelection("/user/shelter/finance").ResolveOne(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
        Volunteers = system.ActorSelection("/user/shelter/volunteers").ResolveOne(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
        Tasks = system.ActorSelection("/user/shelter/tasks").ResolveOne(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
        Settings = system.ActorSelection("/user/shelter/settings").ResolveOne(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Routes a message to its owning actor purely on its marker interface, so call
    /// sites never name an actor ref (removes a whole class of wrong-actor bugs).
    /// </summary>
    public Task<T> Ask<T>(IShelterMessage message, TimeSpan? timeout = null)
        => Route(message).Ask<T>(message, timeout ?? TimeSpan.FromSeconds(10));

    private IActorRef Route(IShelterMessage message) => message switch
    {
        IDogMessage       => Dogs,
        IFinanceMessage   => Finance,
        IAdoptionMessage  => Adoptions,
        IVolunteerMessage => Volunteers,
        ITaskMessage      => Tasks,
        ISettingsMessage  => Settings,
        _ => throw new ArgumentException($"No actor registered for message {message.GetType().Name}")
    };
}
