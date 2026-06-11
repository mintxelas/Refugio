using Refugio.Application.Abstractions;
using Refugio.Domain.Events;

namespace Refugio.Application.Events;

/// <summary>Notifies the applicant by email whenever their application changes status.</summary>
public class AdoptionStatusChangedHandler(IShelterEmailSender emailSender)
    : IDomainEventHandler<AdoptionStatusChanged>
{
    public Task HandleAsync(AdoptionStatusChanged domainEvent, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(domainEvent.ApplicantEmail)) return Task.CompletedTask;
        return emailSender.SendAsync(
            domainEvent.ApplicantEmail,
            $"Application update for {domainEvent.ApplicantName}",
            $"Your adoption application status has been updated to: {domainEvent.NewStatus}.");
    }
}
