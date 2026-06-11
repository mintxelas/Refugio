namespace Refugio.Application.Abstractions;

public interface IShelterEmailSender
{
    Task SendAsync(string to, string subject, string body);
}
