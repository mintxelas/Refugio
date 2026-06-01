using Akka.Actor;
using Akka.DependencyInjection;

namespace Refugio.Application.Actors;

public class ShelterSupervisorActor : ReceiveActor
{
    public ShelterSupervisorActor()
    {
        var resolver = DependencyResolver.For(Context.System);
        Context.ActorOf(resolver.Props<DogActor>(), "dogs");
        Context.ActorOf(resolver.Props<AdoptionActor>(), "adoptions");
        Context.ActorOf(resolver.Props<FinanceActor>(), "finance");
        Context.ActorOf(resolver.Props<VolunteerActor>(), "volunteers");
        Context.ActorOf(resolver.Props<TaskActor>(), "tasks");
        Context.ActorOf(resolver.Props<SettingsActor>(), "settings");
    }

    protected override SupervisorStrategy SupervisorStrategy()
        => new OneForOneStrategy(ex => Directive.Restart);
}
