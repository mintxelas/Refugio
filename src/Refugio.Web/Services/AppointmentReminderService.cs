using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Refugio.Application.Services;
using Refugio.Infrastructure.Data;

namespace Refugio.Web.Services;

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
        var db = scope.ServiceProvider.GetRequiredService<ShelterDbContext>();
        var emailSender = scope.ServiceProvider.GetService<IShelterEmailSender>();
        if (emailSender is null) return;

        var today = DateTime.UtcNow.Date;
        var horizon = today.AddDays(3);

        var upcoming = await db.MedicalRecords
            .Include(r => r.Dog)
            .Where(r => r.NextVisitDate.HasValue
                && r.NextVisitDate.Value.Date >= today
                && r.NextVisitDate.Value.Date <= horizon)
            .ToListAsync();

        if (upcoming.Count == 0) return;

        var managers = await db.Volunteers
            .Where(v => v.Role == "Manager" && v.CanLogin && v.Email != null)
            .Select(v => v.Email!)
            .ToListAsync();

        var lines = upcoming.Select(r =>
            $"- {r.Dog.Name}: {r.NextVisitDate!.Value:MMM d} (Vet: {r.VetName}, {r.Diagnosis})");
        var body = "Upcoming veterinary appointments in the next 3 days:\n\n" + string.Join("\n", lines);
        var subject = $"Upcoming appointments ({upcoming.Count})";

        foreach (var email in managers)
            await emailSender.SendAsync(email, subject, body);
    }
}
