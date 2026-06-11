using Refugio.Application.Services;

namespace Refugio.Web.Services;

/// <summary>Runs the vet-appointment digest once a day via the application-layer notifier.</summary>
public class AppointmentReminderService(IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await NotifyUpcomingAppointmentsAsync();
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    internal async Task NotifyUpcomingAppointmentsAsync()
    {
        using var scope = scopeFactory.CreateScope();
        var notifier = scope.ServiceProvider.GetRequiredService<VetAppointmentNotifier>();
        await notifier.NotifyUpcomingAppointmentsAsync();
    }
}
