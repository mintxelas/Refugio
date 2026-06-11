using Microsoft.Extensions.Logging;
using Refugio.Application.Abstractions;

namespace Refugio.Infrastructure.Email;

/// <summary>Default email adapter: logs instead of sending. Swap for a real SMTP/API sender in production.</summary>
public class NoOpEmailSender(ILogger<NoOpEmailSender> logger) : IShelterEmailSender
{
    public Task SendAsync(string to, string subject, string body)
    {
        logger.LogInformation("Email (no-op): to={To} subject={Subject}", to, subject);
        return Task.CompletedTask;
    }
}
