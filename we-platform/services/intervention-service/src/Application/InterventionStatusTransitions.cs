using InterventionService.Domain;

namespace InterventionService.Application;

public static class InterventionStatusTransitions
{
    private static readonly Dictionary<string, HashSet<string>> AllowedTransitions = new(StringComparer.Ordinal)
    {
        [InterventionStatuses.Planned] = [InterventionStatuses.Active, InterventionStatuses.Completed],
        [InterventionStatuses.Active] = [InterventionStatuses.Completed],
        [InterventionStatuses.Completed] = [InterventionStatuses.Closed],
        [InterventionStatuses.Closed] = []
    };

    public static bool CanTransition(string currentStatus, string nextStatus)
    {
        if (!InterventionStatuses.IsValid(currentStatus) || !InterventionStatuses.IsValid(nextStatus))
        {
            return false;
        }

        if (currentStatus == nextStatus)
        {
            return true;
        }

        return AllowedTransitions.TryGetValue(currentStatus, out var allowed)
            && allowed.Contains(nextStatus);
    }
}
