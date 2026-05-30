using Microsoft.Extensions.Logging;
using Refugio.Application.Services;

namespace Refugio.Web.Services;

public class NoOpEmailSender(ILogger<NoOpEmailSender> logger) : IShelterEmailSender
{
    public Task SendAsync(string to, string subject, string body)
    {
        logger.LogInformation("Email (no-op): to={To} subject={Subject}", to, subject);
        return Task.CompletedTask;
    }
}
