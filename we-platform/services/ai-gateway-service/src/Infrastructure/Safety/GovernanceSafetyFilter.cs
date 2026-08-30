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

        if (EmailPattern().IsMatch(content))
        {
            return new SafetyFilterResult(false, "Prompt contains PII (email address).");
        }

        if (PhonePattern().IsMatch(content))
        {
            return new SafetyFilterResult(false, "Prompt contains PII (phone number).");
        }

        if (SsnPattern().IsMatch(content))
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

    [GeneratedRegex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b", RegexOptions.CultureInvariant)]
    private static partial Regex EmailPattern();

    [GeneratedRegex(@"\b(?:\+?\d{1,3}[-.\s]?)?(?:\(\d{3}\)|\d{3})[-.\s]?\d{3}[-.\s]?\d{4}\b", RegexOptions.CultureInvariant)]
    private static partial Regex PhonePattern();

    [GeneratedRegex(@"\b\d{3}-\d{2}-\d{4}\b", RegexOptions.CultureInvariant)]
    private static partial Regex SsnPattern();
}
