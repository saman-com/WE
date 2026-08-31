using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ReportingService.Application;

namespace ReportingService.Infrastructure.Export;

public sealed class QuestPdfReportExporter : IReportPdfExporter
{
    public QuestPdfReportExporter()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] ExportClassProgressReport(ClassProgressReportContent content) =>
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.Header().Text($"Class Progress Report — {content.ClassName}").SemiBold().FontSize(18);
                page.Content().Column(column =>
                {
                    column.Spacing(12);
                    column.Item().Text($"Generated: {content.GeneratedAt:u}");
                    column.Item().Text("Mastery distribution").SemiBold();
                    foreach (var item in content.MasteryDistribution)
                    {
                        column.Item().Text(
                            $"Micro-skill {item.MicroSkillId}: {string.Join(", ", item.LevelCounts.Select(pair => $"{pair.Key} {pair.Value}"))}");
                        column.Item().Text(item.Explanation).FontSize(10);
                    }

                    column.Item().Text("Active learning gaps").SemiBold();
                    foreach (var gap in content.ActiveLearningGaps)
                    {
                        column.Item().Text(
                            $"{gap.StudentUserId}: {gap.Severity} severity — {gap.Explanation}").FontSize(10);
                    }

                    column.Item().Text("Assessment summary").SemiBold();
                    foreach (var assessment in content.AssessmentSummary)
                    {
                        column.Item().Text(
                            $"{assessment.Title}: {assessment.SubmissionCount} submissions, {assessment.ReviewedCount} reviewed")
                            .FontSize(10);
                    }
                });
            });
        }).GeneratePdf();

    public byte[] ExportSchoolSummaryReport(SchoolSummaryReportContent content) =>
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.Header().Text($"School Summary Report — {content.OrganisationName}").SemiBold().FontSize(18);
                page.Content().Column(column =>
                {
                    column.Spacing(12);
                    column.Item().Text($"Generated: {content.GeneratedAt:u}");
                    column.Item().Text(
                        $"Students: {content.Kpis.TotalStudents} · Classes: {content.Kpis.TotalClasses} · " +
                        $"Active interventions: {content.Kpis.ActiveInterventions} · Active gaps: {content.Kpis.ActiveLearningGaps}");
                    column.Item().Text("Year levels").SemiBold();
                    foreach (var yearLevel in content.YearLevels)
                    {
                        column.Item().Text(
                            $"{yearLevel.YearLevelName}: {yearLevel.ClassCount} classes, {yearLevel.StudentCount} students")
                            .FontSize(10);
                    }

                    column.Item().Text("Class comparisons").SemiBold();
                    foreach (var schoolClass in content.ClassComparisons)
                    {
                        column.Item().Text(
                            $"{schoolClass.ClassName}: {schoolClass.StudentCount} students, " +
                            $"{schoolClass.ActiveInterventions} interventions, {schoolClass.ActiveLearningGaps} gaps")
                            .FontSize(10);
                    }
                });
            });
        }).GeneratePdf();
}
