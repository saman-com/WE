namespace NotificationService.Domain;

public static class NotificationTypes
{
    public const string AssessmentPublished = "AssessmentPublished";
    public const string FeedbackAvailable = "FeedbackAvailable";
    public const string NewMessage = "NewMessage";

    public static bool IsValid(string type) =>
        type is AssessmentPublished or FeedbackAvailable or NewMessage;
}
