namespace InterventionService.Domain;

public static class InterventionStatuses
{
    public const string Planned = "Planned";
    public const string Active = "Active";
    public const string Completed = "Completed";
    public const string Closed = "Closed";

    public static bool IsValid(string status) =>
        status is Planned or Active or Completed or Closed;
}
