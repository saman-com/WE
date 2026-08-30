using System.Text;
using EiService.Application;

namespace EiService.Infrastructure.Ai;

public sealed class SummaryContextBuilder : ISummaryContextBuilder
{
    public IReadOnlyDictionary<string, string> BuildLessonSummaryVariables(
        ClassEiInsightsResponse insights,
        Guid unitId)
    {
        var summaryParts = new List<string>();

        foreach (var item in insights.MasteryDistribution)
        {
            summaryParts.Add(
                $"Micro-skill {item.MicroSkillId}: {item.Explanation} " +
                $"(evidence: {string.Join(", ", item.LinkedEvidenceIds)})");
        }

        foreach (var gap in insights.ActiveLearningGaps)
        {
            summaryParts.Add(
                $"Gap {gap.GapId} for student {gap.StudentUserId}: {gap.Explanation} " +
                $"(evidence: {gap.EvidenceId})");
        }

        foreach (var trend in insights.RecentDiagnosticTrends)
        {
            summaryParts.Add(
                $"Diagnostic {trend.Status} for micro-skill {trend.MicroSkillId}: {trend.Explanation} " +
                $"(evidence: {trend.EvidenceId})");
        }

        return new Dictionary<string, string>
        {
            ["unitId"] = unitId.ToString(),
            ["classInsightsSummary"] = string.Join("\n", summaryParts)
        };
    }

    public IReadOnlyDictionary<string, string> BuildProgressReportVariables(
        StudentEiSnapshot snapshot)
    {
        var builder = new StringBuilder();

        foreach (var mastery in snapshot.MasteryRecords)
        {
            builder.AppendLine(
                $"Mastery {mastery.MasteryLevel} for micro-skill {mastery.MicroSkillId}: {mastery.Explanation}");
        }

        foreach (var gap in snapshot.Gaps)
        {
            builder.AppendLine(
                $"Gap {gap.Severity}/{gap.Urgency} for micro-skill {gap.MicroSkillId}: {gap.Explanation} " +
                $"(evidence: {gap.EvidenceId})");
        }

        foreach (var diagnostic in snapshot.Diagnostics)
        {
            builder.AppendLine(
                $"Diagnostic {diagnostic.Status} for micro-skill {diagnostic.MicroSkillId}: {diagnostic.Reason} " +
                $"(evidence: {diagnostic.EvidenceId})");
        }

        return new Dictionary<string, string>
        {
            ["studentUserId"] = snapshot.StudentUserId,
            ["slpEvidenceContext"] = builder.ToString().Trim()
        };
    }
}
