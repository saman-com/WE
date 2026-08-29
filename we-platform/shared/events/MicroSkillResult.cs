namespace WePlatform.Events;

public sealed record MicroSkillResult(
    Guid MicroSkillId,
    decimal Mark,
    string Feedback);
