namespace EiService.Application;

public interface IClassInsightsAggregationEngine
{
    ClassEiInsightsResponse Aggregate(ClassInsightsInput input);
}
