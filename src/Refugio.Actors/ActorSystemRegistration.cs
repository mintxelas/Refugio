using Akka.Actor;
using Akka.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Refugio.Actors;

public static class ActorSystemRegistration
{
    public const string SystemName = "refugio";

    /// <summary>One actor per aggregate area; each delegates to the scoped application services.</summary>
    public static IServiceCollection AddShelterActors(this IServiceCollection services) =>
        services.AddAkka(SystemName, builder => builder.WithActors((system, registry, resolver) =>
        {
            registry.Register<DogActor>(system.ActorOf(resolver.Props<DogActor>(), "dog"));
            registry.Register<AdoptionActor>(system.ActorOf(resolver.Props<AdoptionActor>(), "adoption"));
            registry.Register<VolunteerActor>(system.ActorOf(resolver.Props<VolunteerActor>(), "volunteer"));
            registry.Register<EventActor>(system.ActorOf(resolver.Props<EventActor>(), "event"));
            registry.Register<TaskActor>(system.ActorOf(resolver.Props<TaskActor>(), "task"));
            registry.Register<FinanceActor>(system.ActorOf(resolver.Props<FinanceActor>(), "finance"));
            registry.Register<SettingsActor>(system.ActorOf(resolver.Props<SettingsActor>(), "settings"));
            system.ActorOf(resolver.Props<ReminderActor>(), "reminder");
        }));
}
