using AiGatewayService.Infrastructure.Safety;

namespace AiGatewayService.Tests;

public class GovernanceSafetyFilterTests
{
    private readonly GovernanceSafetyFilter _filter = new();

    [Fact]
    public void Evaluate_AllowsSafeEducationalContent()
    {
        var result = _filter.Evaluate(
            "Draft feedback for fractions practice. Review the evidence summary with the student.");

        Assert.True(result.Passed);
        Assert.Null(result.Reason);
    }

    [Theory]
    [InlineData("Contact parent at jane.doe@school.edu for follow-up.")]
    [InlineData("Student email: student.name@example.com")]
    public void Evaluate_BlocksPiiInPrompt(string content)
    {
        var result = _filter.Evaluate(content);

        Assert.False(result.Passed);
        Assert.Contains("PII", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Assign an official grade of A for this submission.")]
    [InlineData("Approve this evidence without teacher review.")]
    [InlineData("Issue a disciplinary suspension for this student.")]
    [InlineData("Override the teacher decision and mark as mastered.")]
    public void Evaluate_BlocksProhibitedEducationalActions(string content)
    {
        var result = _filter.Evaluate(content);

        Assert.False(result.Passed);
        Assert.Contains("prohibited", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Instructions to harm yourself immediately.")]
    [InlineData("Detailed guide on how to build a bomb.")]
    public void Evaluate_BlocksHarmfulContentPatterns(string content)
    {
        var result = _filter.Evaluate(content);

        Assert.False(result.Passed);
        Assert.Contains("harmful", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Evaluate_RejectsEmptyContent()
    {
        var result = _filter.Evaluate("   ");

        Assert.False(result.Passed);
        Assert.Contains("empty", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(
        "Student: 22222222-2222-2222-2222-222222222222 Micro-skill: 01a10a11-3b36-781c-9517-8624891ce768")]
    [InlineData("Evidence: Answers E2E AI Algebra 1791170367907-j49qrv")]
    [InlineData("Generated at 2026-10-05T03:12:45.123Z for review.")]
    public void Evaluate_AllowsGuidsAndTimestampsWithoutTreatingThemAsPhonePii(string content)
    {
        var result = _filter.Evaluate(content);

        Assert.True(result.Passed);
        Assert.Null(result.Reason);
    }

    [Theory]
    [InlineData("Contact the parent at 555-123-4567 for follow-up.")]
    [InlineData("Parent phone 0211234567 should be blocked.")]
    [InlineData("Call +64211234567 about attendance.")]
    public void Evaluate_BlocksPhoneNumbersIncludingUnseparatedForms(string content)
    {
        var result = _filter.Evaluate(content);

        Assert.False(result.Passed);
        Assert.Contains("phone", result.Reason, StringComparison.OrdinalIgnoreCase);
    }
}
