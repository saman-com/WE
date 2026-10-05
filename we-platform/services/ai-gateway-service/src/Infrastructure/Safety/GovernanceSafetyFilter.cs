using System.Text.RegularExpressions;
using AiGatewayService.Application;

namespace AiGatewayService.Infrastructure.Safety;

public sealed partial class GovernanceSafetyFilter : ISafetyFilter
{
    private static readonly string[] ProhibitedPhrases =
    [
        "assign an official grade",
        "assign official grade",
        "approve this evidence",
        "approve evidence without",
        "disciplinary suspension",
        "disciplinary action",
        "override the teacher",
        "override teacher decision",
        "alter educational evidence",
        "modify educational evidence"
    ];

    private static readonly string[] HarmfulPhrases =
    [
        "harm yourself",
        "kill yourself",
        "build a bomb",
        "make a weapon"
    ];

    public SafetyFilterResult Evaluate(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return new SafetyFilterResult(false, "Content is empty.");
        }

        // Strip GUIDs and ISO timestamps so digit groups inside them are not treated as phone/SSN.
        var sanitized = IsoTimestampPattern().Replace(GuidPattern().Replace(content, string.Empty), string.Empty);

        if (EmailPattern().IsMatch(sanitized))
        {
            return new SafetyFilterResult(false, "Prompt contains PII (email address).");
        }

        if (PhonePattern().IsMatch(sanitized))
        {
            return new SafetyFilterResult(false, "Prompt contains PII (phone number).");
        }

        if (SsnPattern().IsMatch(sanitized))
        {
            return new SafetyFilterResult(false, "Prompt contains PII (government identifier).");
        }

        var normalized = content.ToLowerInvariant();
        foreach (var phrase in ProhibitedPhrases)
        {
            if (normalized.Contains(phrase, StringComparison.Ordinal))
            {
                return new SafetyFilterResult(false, "Prompt requests a prohibited educational action.");
            }
        }

        foreach (var phrase in HarmfulPhrases)
        {
            if (normalized.Contains(phrase, StringComparison.Ordinal))
            {
                return new SafetyFilterResult(false, "Prompt contains harmful content patterns.");
            }
        }

        return new SafetyFilterResult(true, null);
    }

    [GeneratedRegex(
        @"\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\b",
        RegexOptions.CultureInvariant)]
    private static partial Regex GuidPattern();

    // ISO-8601 and common Unix-ms suffixes used in generated titles.
    [GeneratedRegex(
        @"\b\d{4}-\d{2}-\d{2}(?:[T ]\d{2}:\d{2}(?::\d{2}(?:\.\d+)?)?(?:Z|[+-]\d{2}:?\d{2})?)?\b|\b\d{13}\b",
        RegexOptions.CultureInvariant)]
    private static partial Regex IsoTimestampPattern();

    [GeneratedRegex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b", RegexOptions.CultureInvariant)]
    private static partial Regex EmailPattern();

    // Detect formatted and compact local/international numbers (e.g. 0211234567, +64211234567).
    [GeneratedRegex(
        @"\b(?:\+?\d{1,3}[-.\s]*)?(?:\(?\d{2,4}\)?[-.\s]*)?\d{3,4}[-.\s]*\d{3,4}\b|\b(?:0\d{8,10}|\+\d{8,15})\b",
        RegexOptions.CultureInvariant)]
    private static partial Regex PhonePattern();

    [GeneratedRegex(@"\b\d{3}-\d{2}-\d{4}\b", RegexOptions.CultureInvariant)]
    private static partial Regex SsnPattern();
}
