namespace Refugio.Application.Services;

public interface IShelterEmailSender
{
    Task SendAsync(string to, string subject, string body);
}
