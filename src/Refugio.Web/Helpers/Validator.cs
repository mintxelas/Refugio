namespace Refugio.Web.Helpers;

public static class Validator
{
    public static void RequireNotEmpty(List<string> errors, string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value)) errors.Add(message);
    }

    public static void RequirePositive(List<string> errors, int value, string message)
    {
        if (value <= 0) errors.Add(message);
    }

    public static void RequirePositive(List<string> errors, decimal value, string message)
    {
        if (value <= 0) errors.Add(message);
    }

    public static void RequireNonNegative(List<string> errors, decimal value, string message)
    {
        if (value < 0) errors.Add(message);
    }

    public static void RequireValidEmail(List<string> errors, string? email, string message)
    {
        if (!string.IsNullOrWhiteSpace(email) && !email.Contains('@')) errors.Add(message);
    }

    public static void RequireDate(List<string> errors, DateTime? value, string message)
    {
        if (!value.HasValue) errors.Add(message);
    }

    public static void RequireAfter(List<string> errors, DateTime? end, DateTime? start, string message)
    {
        if (end.HasValue && start.HasValue && end < start) errors.Add(message);
    }
}
