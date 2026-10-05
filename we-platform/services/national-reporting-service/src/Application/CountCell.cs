namespace NationalReportingService.Application;

/// <summary>
/// Aggregate count cell that may be withheld under small-count suppression.
/// </summary>
public record CountCell(int? Value, bool Suppressed)
{
    public static CountCell Visible(int value) => new(value, false);

    public static CountCell Hidden() => new(null, true);

    public static CountCell FromCount(int count, int minimumGroupSize) =>
        count < minimumGroupSize ? Hidden() : Visible(count);

    /// <summary>
    /// Roll-up that suppresses the parent whenever any child is suppressed,
    /// so a hidden child cannot be recovered by subtracting visible siblings.
    /// </summary>
    public static CountCell SumOrSuppress(IEnumerable<CountCell> cells)
    {
        var list = cells as IList<CountCell> ?? cells.ToList();
        if (list.Count == 0)
        {
            return Visible(0);
        }

        if (list.Any(c => c.Suppressed))
        {
            return Hidden();
        }

        return Visible(list.Sum(c => c.Value!.Value));
    }
}
