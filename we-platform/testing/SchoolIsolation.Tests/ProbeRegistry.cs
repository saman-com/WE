namespace SchoolIsolation.Tests;

/// <summary>
/// Probe ids declared by isolation-catalog.json must appear here once the
/// corresponding table-driven school-isolation test exists in a service test project
/// (or in this project for consumer probes).
/// </summary>
internal static class ProbeRegistry
{
    public static IReadOnlyList<string> All { get; } =
    [
        // Registered as service TenantIsolationEndpointTests / consumer suites are expanded.
        // Keep sorted. Coverage guard fails if catalog references an unregistered probe.
        "ai-gateway-service:ai",
        "assessment-service:assessment-list",
        "assessment-service:assessment-resource",
        "communication-service:messages",
        "configuration-service:regional-config",
        "curriculum-service:curriculum-list",
        "curriculum-service:curriculum-resource",
        "diagnostic-service:person-resource",
        "ei-service:audit-resource",
        "ei-service:organisation-path",
        "ei-service:person-resource",
        "evidence-service:evidence-list",
        "evidence-service:evidence-resource",
        "federation-service:federation-school",
        "identity-service:parent-teachers",
        "intervention-service:intervention-list",
        "intervention-service:intervention-resource",
        "intervention-service:organisation-path",
        "learning-gap-service:person-resource",
        "mastery-service:person-resource",
        "notification-service:notification-list",
        "notification-service:notification-resource",
        "organisation-service:organisation-list",
        "organisation-service:organisation-path",
        "organisation-service:parent-teachers",
        "organisation-service:person-resource",
        "reporting-service:organisation-path",
        "reporting-service:report-resource",
        "student-learning-service:person-resource"
    ];
}
