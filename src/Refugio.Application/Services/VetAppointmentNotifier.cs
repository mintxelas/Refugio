using Refugio.Application.Abstractions;
using Refugio.Application.Queries;

namespace Refugio.Application.Services;

/// <summary>
/// Emails every manager a digest of vet visits scheduled in the next three days.
/// Hosted by the web app's daily background service.
/// </summary>
public class VetAppointmentNotifier(
    IMedicalQueries medicalQueries,
    IVolunteerQueries volunteerQueries,
    IShelterEmailSender emailSender)
{
    public async Task NotifyUpcomingAppointmentsAsync()
    {
        var upcoming = await medicalQueries.GetUpcomingVisitsAsync(daysAhead: 3);
        if (upcoming.Count == 0) return;

        var managers = await volunteerQueries.GetManagerEmailsAsync();
        var lines = upcoming.Select(v =>
            $"- {v.DogName}: {v.NextVisitDate:MMM d} (Vet: {v.VetName}, {v.Diagnosis})");
        var body = "Upcoming veterinary appointments in the next 3 days:\n\n" + string.Join("\n", lines);
        var subject = $"Upcoming appointments ({upcoming.Count})";

        foreach (var email in managers)
            await emailSender.SendAsync(email, subject, body);
    }
}
