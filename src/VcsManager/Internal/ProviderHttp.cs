using System.Net;
using System.Text;

namespace VcsManager.Internal;

internal static class ProviderHttp
{
    public static TimeSpan? GetRetryAfter(HttpResponseMessage response)
    {
        var retryAfter = response.Headers.RetryAfter;
        if (retryAfter?.Delta is { } delta)
        {
            return delta > TimeSpan.Zero ? delta : TimeSpan.Zero;
        }

        if (retryAfter?.Date is { } date)
        {
            var delay = date - DateTimeOffset.UtcNow;
            return delay > TimeSpan.Zero ? delay : TimeSpan.Zero;
        }

        return null;
    }

    public static string AddQuery(
        string path,
        IReadOnlyDictionary<string, string> parameters)
    {
        var builder = new StringBuilder(path);
        var separator = path.Contains('?') ? '&' : '?';

        foreach (var parameter in parameters)
        {
            builder.Append(separator);
            builder.Append(Uri.EscapeDataString(parameter.Key));
            builder.Append('=');
            builder.Append(Uri.EscapeDataString(parameter.Value));
            separator = '&';
        }

        return builder.ToString();
    }

    public static string? BuildCommitMessage(string? title, string? message)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return string.IsNullOrWhiteSpace(message) ? null : message.Trim();
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            return title.Trim();
        }

        return $"{title.Trim()}\n\n{message.Trim()}";
    }
}
