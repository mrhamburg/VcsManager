using System.Globalization;

namespace VcsManager.Internal;

internal static class ProviderConfiguration
{
    public static bool TryGetValue(
        IReadOnlyDictionary<string, string> configuration,
        string key,
        out string value)
    {
        if (configuration.TryGetValue(key, out var found) && !string.IsNullOrWhiteSpace(found))
        {
            value = found.Trim();
            return true;
        }

        value = string.Empty;
        return false;
    }

    public static TimeSpan ParseTimeSpan(string value, string key)
    {
        if (!TimeSpan.TryParseExact(value, "c", CultureInfo.InvariantCulture, out var timeout))
        {
            throw new ArgumentException(
                $"The '{key}' configuration setting must be a time span such as '00:01:40'.",
                "configuration");
        }

        return timeout;
    }
}
