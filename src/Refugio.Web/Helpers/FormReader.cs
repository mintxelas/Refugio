using System.Globalization;

namespace Refugio.Web.Helpers;

public static class FormReader
{
    public static string GetString(IFormCollection form, string key)
        => form[key].ToString().Trim();

    public static string? GetStringOrNull(IFormCollection form, string key)
    {
        var v = form[key].ToString().Trim();
        return v.Length > 0 ? v : null;
    }

    public static int GetInt(IFormCollection form, string key, int defaultValue = 0)
        => int.TryParse(form[key].ToString(), out var v) ? v : defaultValue;

    public static int? GetNullableInt(IFormCollection form, string key)
        => int.TryParse(form[key].ToString(), out var v) && v > 0 ? v : null;

    public static decimal GetDecimal(IFormCollection form, string key, decimal defaultValue = 0)
        => decimal.TryParse(form[key].ToString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var v) ? v : defaultValue;

    public static DateTime? GetDateTime(IFormCollection form, string key)
        => DateTime.TryParse(form[key].ToString(), out var v) ? v : null;

    public static bool GetBool(IFormCollection form, string key)
        => form[key].ToString() is "true" or "on" or "1";

    public static T GetEnum<T>(IFormCollection form, string key, T defaultValue = default!) where T : struct, Enum
        => Enum.TryParse<T>(form[key].ToString(), out var v) ? v : defaultValue;
}
