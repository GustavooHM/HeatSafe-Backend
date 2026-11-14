using System.Globalization;

namespace HeatSafe.Api.Services;

public static class DateTimeNormalizer
{
    public static bool TryToUtc(string? value, out DateTime utc)
    {
        utc = default;
        if (string.IsNullOrWhiteSpace(value)) return false;
        var hasExplicitOffset = value.EndsWith('Z') || value.EndsWith('z') ||
            System.Text.RegularExpressions.Regex.IsMatch(value, @"[+-]\d{2}:?\d{2}$");

        if (hasExplicitOffset)
        {
            if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var offset)) return false;
            utc = offset.UtcDateTime;
            return true;
        }

        if (!DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var parsed)) return false;
        var local = DateTime.SpecifyKind(parsed, DateTimeKind.Unspecified);
        utc = TimeZoneInfo.ConvertTimeToUtc(local, GetSaoPauloTimeZone());
        return true;
    }

    public static TimeZoneInfo GetSaoPauloTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time"); }
    }
}
