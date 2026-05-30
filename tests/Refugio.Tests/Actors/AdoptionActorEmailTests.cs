using Akka.Actor;
using Microsoft.Extensions.DependencyInjection;
using Refugio.Application.Actors;
using Refugio.Application.Messages;
using Refugio.Application.Services;
using Refugio.Domain.Entities;
using Refugio.Tests.Helpers;

namespace Refugio.Tests.Actors;

public sealed class CapturingEmailSender : IShelterEmailSender
{
    public readonly List<(string To, string Subject, string Body)> Sent = [];

    public Task SendAsync(string to, string subject, string body)
    {
        Sent.Add((to, subject, body));
        return Task.CompletedTask;
    }
}

public class AdoptionActorEmailTests : ActorTestBase
{
    private readonly IActorRef _actor;
    private readonly CapturingEmailSender _emailSender = new();

    protected override void ConfigureServices(IServiceCollection services)
        => services.AddSingleton<IShelterEmailSender>(_emailSender);

    public AdoptionActorEmailTests()
    {
        _actor = Sys.ActorOf(Props.Create(() => new AdoptionActor(_sf)));
    }

    private async Task<(Dog dog, Adoption adoption)> SeedAdoptionWithEmail(string email)
    {
        var dog = await SeedAsync(db => { var d = new Dog { Name = "EmailDog", Breed = "Lab", Gender = "M" }; db.Dogs.Add(d); return d; });
        var adoption = await SeedAsync(db =>
        {
            var a = new Adoption { DogId = dog.Id, ApplicantName = "Tester", ApplicantEmail = email, Status = AdoptionStatus.Applied };
            db.Adoptions.Add(a);
            return a;
        });
        return (dog, adoption);
    }

    [Fact]
    public async Task UpdateAdoptionStatus_SendsEmail_WhenApplicantEmailSet()
    {
        var (_, adoption) = await SeedAdoptionWithEmail("applicant@test.com");

        await _actor.Ask<Adoption?>(
            new UpdateAdoptionStatus(adoption.Id, AdoptionStatus.Interview, null),
            TimeSpan.FromSeconds(5));

        Assert.Single(_emailSender.Sent);
        var (to, _, body) = _emailSender.Sent[0];
        Assert.Equal("applicant@test.com", to);
        Assert.Contains("Interview", body);
    }

    [Fact]
    public async Task UpdateAdoptionStatus_DoesNotSendEmail_WhenNoApplicantEmail()
    {
        var dog = await SeedAsync(db => { var d = new Dog { Name = "NoEmailDog", Breed = "Lab", Gender = "M" }; db.Dogs.Add(d); return d; });
        var adoption = await SeedAsync(db =>
        {
            var a = new Adoption { DogId = dog.Id, ApplicantName = "NoEmail", ApplicantEmail = null, Status = AdoptionStatus.Applied };
            db.Adoptions.Add(a);
            return a;
        });

        await _actor.Ask<Adoption?>(
            new UpdateAdoptionStatus(adoption.Id, AdoptionStatus.Interview, null),
            TimeSpan.FromSeconds(5));

        Assert.Empty(_emailSender.Sent);
    }
}
