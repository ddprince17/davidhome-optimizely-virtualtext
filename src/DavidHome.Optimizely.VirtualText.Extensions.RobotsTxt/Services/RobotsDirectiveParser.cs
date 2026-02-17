using System.Globalization;
using System.Text.RegularExpressions;

namespace DavidHome.Optimizely.VirtualText.Extensions.RobotsTxt.Services;

internal static partial class RobotsDirectiveParser
{
    private static readonly HashSet<string> PlainDirectives =
    [
        "all",
        "noindex",
        "nofollow",
        "none",
        "nosnippet",
        "indexifembedded",
        "notranslate",
        "noimageindex"
    ];

    public static bool TryNormalize(string? input, out string? normalized, out string? error)
    {
        normalized = null;
        error = null;

        if (string.IsNullOrWhiteSpace(input))
        {
            return true;
        }

        var parts = DirectiveSplitRegex().Split(input.Trim());
        var normalizedParts = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawPart in parts)
        {
            var part = rawPart.Trim();
            if (part.Length == 0)
            {
                continue;
            }

            if (!TryNormalizePart(part, out var normalizedPart, out error))
            {
                normalized = null;
                return false;
            }

            if (seen.Add(normalizedPart))
            {
                normalizedParts.Add(normalizedPart);
            }
        }

        if (normalizedParts.Count == 0)
        {
            return true;
        }

        normalized = string.Join(", ", normalizedParts);
        return true;
    }

    [GeneratedRegex(",\\s*(?=(?:all|noindex|nofollow|none|nosnippet|indexifembedded|notranslate|noimageindex|max-snippet:|max-image-preview:|max-video-preview:|unavailable_after:))", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DirectiveSplitRegex();

    private static bool TryNormalizePart(string part, out string normalizedPart, out string? error)
    {
        var lower = part.ToLowerInvariant();
        if (PlainDirectives.Contains(lower))
        {
            normalizedPart = lower;
            error = null;
            return true;
        }

        if (lower.StartsWith("max-snippet:", StringComparison.Ordinal))
        {
            return TryNormalizeIntegerDirective(part, "max-snippet", out normalizedPart, out error);
        }

        if (lower.StartsWith("max-video-preview:", StringComparison.Ordinal))
        {
            return TryNormalizeIntegerDirective(part, "max-video-preview", out normalizedPart, out error);
        }

        if (lower.StartsWith("max-image-preview:", StringComparison.Ordinal))
        {
            var value = part["max-image-preview:".Length..].Trim().ToLowerInvariant();
            if (value is "none" or "standard" or "large")
            {
                normalizedPart = $"max-image-preview:{value}";
                error = null;
                return true;
            }

            normalizedPart = string.Empty;
            error = "Invalid max-image-preview value. Allowed values: none, standard, large.";
            return false;
        }

        if (lower.StartsWith("unavailable_after:", StringComparison.Ordinal))
        {
            var value = part["unavailable_after:".Length..].Trim();
            if (value.Length == 0 || !DateTimeOffset.TryParse(value, out _))
            {
                normalizedPart = string.Empty;
                error = "Invalid unavailable_after value. Use a valid date/time.";
                return false;
            }

            normalizedPart = $"unavailable_after:{value}";
            error = null;
            return true;
        }

        normalizedPart = string.Empty;
        error = $"Unknown robots directive: '{part}'.";
        return false;
    }

    private static bool TryNormalizeIntegerDirective(string part, string directiveName, out string normalizedPart, out string? error)
    {
        var value = part[(directiveName.Length + 1)..].Trim();
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            normalizedPart = string.Empty;
            error = $"Invalid {directiveName} value. Use an integer.";
            return false;
        }

        normalizedPart = $"{directiveName}:{parsed.ToString(CultureInfo.InvariantCulture)}";
        error = null;
        return true;
    }
}
