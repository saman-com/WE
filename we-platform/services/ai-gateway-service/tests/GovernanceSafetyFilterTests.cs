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
}
