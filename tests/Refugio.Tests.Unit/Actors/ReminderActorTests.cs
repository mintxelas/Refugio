using Akka.Actor;
using Akka.TestKit.Xunit2;
using Microsoft.Extensions.DependencyInjection;
using Refugio.Actors;
using Refugio.Application.Abstractions;
using Refugio.Application.Contracts;
using Refugio.Application.Queries;
using Refugio.Application.Services;

namespace Refugio.Tests.Unit.Actors;

/// <summary>The reminder timer fires immediately on start and sends the digest to managers.</summary>
public class ReminderActorTests : TestKit
{
    private sealed class StubMedicalQueries : IMedicalQueries
    {
        public Task<List<UpcomingVisit>> GetUpcomingVisitsAsync(int daysAhead) =>
            Task.FromResult(new List<UpcomingVisit>
            {
                new("Rex", DateTime.UtcNow.AddDays(1), "Dr. Vet", "Checkup")
            });
    }

    private sealed class StubVolunteerQueries : IVolunteerQueries
    {
        public Task<List<string>> GetManagerEmailsAsync() =>
            Task.FromResult(new List<string> { "manager@shelter.org" });

        public Task<VolunteerCounts> GetCountsAsync() =>
            Task.FromResult(new VolunteerCounts(0, 0, 0));
    }

    private sealed class CapturingEmailSender : IShelterEmailSender
    {
        public readonly TaskCompletionSource<(string To, string Subject)> Sent =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task SendAsync(string to, string subject, string body)
        {
            Sent.TrySetResult((to, subject));
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Digest_runs_on_startup_and_emails_managers()
    {
        var emailSender = new CapturingEmailSender();
        var scopeFactory = new ServiceCollection()
            .AddScoped<VetAppointmentNotifier>()
            .AddSingleton<IMedicalQueries>(new StubMedicalQueries())
            .AddSingleton<IVolunteerQueries>(new StubVolunteerQueries())
            .AddSingleton<IShelterEmailSender>(emailSender)
            .BuildServiceProvider()
            .GetRequiredService<IServiceScopeFactory>();

        Sys.ActorOf(Props.Create(() => new ReminderActor(scopeFactory)));

        var (to, subject) = await emailSender.Sent.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("manager@shelter.org", to);
        Assert.Contains("Upcoming appointments", subject);
    }
}
