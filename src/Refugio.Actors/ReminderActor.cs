using Akka.Actor;
using Akka.Event;
using Microsoft.Extensions.DependencyInjection;
using Refugio.Application.Services;

namespace Refugio.Actors;

/// <summary>
/// Daily vet-appointment digest on an actor timer (replaces the BackgroundService wrapper).
/// A failed run is logged and retried at the next tick instead of stopping the host.
/// </summary>
public sealed class ReminderActor : ReceiveActor, IWithTimers
{
    public sealed record SendDigest;

    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    private readonly ILoggingAdapter _log = Context.GetLogger();

    public ITimerScheduler Timers { get; set; } = null!;

    public ReminderActor(IServiceScopeFactory scopeFactory)
    {
        ReceiveAsync<SendDigest>(async _ =>
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                await scope.ServiceProvider.GetRequiredService<VetAppointmentNotifier>()
                    .NotifyUpcomingAppointmentsAsync();
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Vet appointment digest failed");
            }
        });
    }

    protected override void PreStart() =>
        Timers.StartPeriodicTimer("digest", new SendDigest(), TimeSpan.Zero, Interval);
}
