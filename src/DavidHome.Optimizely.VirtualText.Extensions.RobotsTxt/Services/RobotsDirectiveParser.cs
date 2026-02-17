using System.Globalization;
using System.Text.RegularExpressions;

namespace DavidHome.Optimizely.VirtualText.Extensions.RobotsTxt.Services;

internal static partial class RobotsDirectiveParser
{
    private const string RobotsName = "robots";

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

    private static readonly HashSet<string> ParameterizedDirectivePrefixes =
    [
        "max-snippet",
        "max-video-preview",
        "max-image-preview",
        "unavailable_after"
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

            if (!TryNormalizePart(part, allowUserAgent: true, out var normalizedPart, out error))
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

    public static string? GetMetaTagContent(string? tagName, string? normalizedDirectives)
    {
        if (string.IsNullOrWhiteSpace(tagName) || string.IsNullOrWhiteSpace(normalizedDirectives))
        {
            return null;
        }

        var normalizedTagName = tagName.Trim().ToLowerInvariant();
        if (!string.Equals(normalizedTagName, RobotsName, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var parts = DirectiveSplitRegex().Split(normalizedDirectives.Trim());
        var output = new List<string>();

        foreach (var rawPart in parts)
        {
            var part = rawPart.Trim();
            if (part.Length == 0)
            {
                continue;
            }

            if (!TryExtractUserAgentDirective(part, out _, out _))
            {
                output.Add(part);
            }
        }

        return output.Count == 0 ? null : string.Join(", ", output);
    }

    [GeneratedRegex(@",\s*(?=(?:all|noindex|nofollow|none|nosnippet|indexifembedded|notranslate|noimageindex|max-snippet:|max-image-preview:|max-video-preview:|unavailable_after:|[a-z0-9_*\-]+\s*:))", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DirectiveSplitRegex();

    private static bool TryNormalizePart(string part, bool allowUserAgent, out string normalizedPart, out string? error)
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

        if (allowUserAgent && TryExtractUserAgentDirective(part, out var userAgent, out var userAgentDirective))
        {
            if (!TryNormalizePart(userAgentDirective, allowUserAgent: false, out var normalizedUserAgentDirective, out error))
            {
                normalizedPart = string.Empty;
                return false;
            }

            normalizedPart = $"{userAgent}: {normalizedUserAgentDirective}";
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

    private static bool TryExtractUserAgentDirective(string part, out string userAgent, out string directive)
    {
        userAgent = string.Empty;
        directive = string.Empty;

        var separatorIndex = part.IndexOf(':');
        if (separatorIndex <= 0)
        {
            return false;
        }

        var possiblePrefix = part[..separatorIndex].Trim().ToLowerInvariant();
        if (possiblePrefix.Length == 0 || ParameterizedDirectivePrefixes.Contains(possiblePrefix) || !UserAgentRegex().IsMatch(possiblePrefix))
        {
            return false;
        }

        var possibleDirective = part[(separatorIndex + 1)..].Trim();
        if (possibleDirective.Length == 0)
        {
            return false;
        }

        userAgent = possiblePrefix;
        directive = possibleDirective;
        return true;
    }

    [GeneratedRegex("^[a-z0-9][a-z0-9_\\-*]*$", RegexOptions.CultureInvariant)]
    private static partial Regex UserAgentRegex();
}
