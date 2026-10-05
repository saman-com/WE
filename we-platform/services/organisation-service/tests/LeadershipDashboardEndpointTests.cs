using System.Net;
using System.Net.Http.Json;
using OrganisationService.Application;

namespace OrganisationService.Tests;

public class LeadershipDashboardEndpointTests : IClassFixture<OrganisationWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly FakeAssessmentDashboardClient _assessmentClient;
    private readonly FakeInterventionDashboardClient _interventionClient;
    private readonly FakeEiInsightsClient _eiClient;

    public LeadershipDashboardEndpointTests(OrganisationWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _assessmentClient = factory.AssessmentClient;
        _interventionClient = factory.InterventionClient;
        _eiClient = factory.EiClient;

        _assessmentClient.Summaries = [];
        _assessmentClient.SummariesByClass = new();
        _assessmentClient.RequestedClasses.Clear();
        _interventionClient.InterventionsByStudent.Clear();
        _interventionClient.OrganisationInterventions.Clear();
        _eiClient.InsightsByClass.Clear();
        _eiClient.RequestedClasses.Clear();
    }

    [Fact]
    public async Task Admin_CanAssignSchoolLeader()
    {
        var adminId = Guid.NewGuid().ToString();
        var leaderId = Guid.NewGuid().ToString();
        var org = await CreateOrganisationAsAdminAsync(adminId, "Leader Assign School", UniqueCode("LAS"));

        using var assign = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/organisations/{org.Id}/leaders",
            adminId,
            org.Id,
            TestJwt.AdminRole);
        assign.Content = JsonContent.Create(new AssignSchoolLeaderRequest(leaderId));
        var assignResponse = await _client.SendAsync(assign);

        Assert.Equal(HttpStatusCode.Created, assignResponse.StatusCode);
        var assigned = await assignResponse.Content.ReadFromJsonAsync<SchoolLeaderResponse>();
        Assert.Equal(leaderId, assigned!.UserId);

        using var duplicate = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/organisations/{org.Id}/leaders",
            adminId,
            org.Id,
            TestJwt.AdminRole);
        duplicate.Content = JsonContent.Create(new AssignSchoolLeaderRequest(leaderId));
        Assert.Equal(HttpStatusCode.Conflict, (await _client.SendAsync(duplicate)).StatusCode);

        var dashboard = await SendAsAsync<LeadershipDashboardResponse>(
            HttpMethod.Get,
            $"/api/v1/organisations/{org.Id}/leadership/dashboard",
            leaderId,
            org.Id,
            TestJwt.SchoolLeaderRole);
        Assert.Equal(org.Id, dashboard.OrganisationId);
    }

    [Fact]
    public async Task SchoolLeader_CanViewLeadershipDashboardForAssignedOrganisation()
    {
        var adminId = Guid.NewGuid().ToString();
        var leaderId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var microSkillId = Guid.CreateVersion7();
        var assessmentId = Guid.CreateVersion7();

        var org = await CreateOrganisationAsAdminAsync(adminId, "Summit School", UniqueCode("SUM"));
        var year = await CreateYearLevelAsAdminAsync(adminId, org.Id, "Year 7", 7);
        var schoolClass = await CreateClassAsAdminAsync(adminId, org.Id, year.Id, "7A", UniqueCode("7A"));
        await EnrollStudentAsync(adminId, org.Id, schoolClass.Id, studentId);
        await AssignSchoolLeaderAsync(adminId, org.Id, leaderId);

        _assessmentClient.Summaries =
        [
            new AssessmentSummaryData(assessmentId, "Term Quiz", "Published", DateTimeOffset.UtcNow.AddDays(5), 1)
        ];
        _interventionClient.InterventionsByStudent[studentId] =
        [
            new ParentInterventionSummaryData(
                Guid.CreateVersion7(),
                "Guided reading support",
                "Active",
                DateTimeOffset.UtcNow.AddDays(-2),
                DateTimeOffset.UtcNow.AddDays(14))
        ];
        _eiClient.InsightsByClass[(org.Id, schoolClass.Id)] = new ClassEiInsightsData(
            org.Id,
            schoolClass.Id,
            [
                new ClassMasteryDistributionData(
                    microSkillId,
                    new Dictionary<string, int> { ["Developing"] = 1 },
                    1,
                    "One student developing.",
                    [])
            ],
            [
                new ClassActiveGapData(
                    Guid.CreateVersion7(),
                    microSkillId,
                    "High",
                    "Medium",
                    studentId,
                    "Gap detected from recent evidence.",
                    Guid.CreateVersion7())
            ],
            [],
            [
                new StudentNeedingAttentionData(studentId, "Active learning gap", "High severity gap.", null)
            ]);

        var dashboard = await SendAsAsync<LeadershipDashboardResponse>(
            HttpMethod.Get,
            $"/api/v1/organisations/{org.Id}/leadership/dashboard",
            leaderId,
            org.Id,
            TestJwt.SchoolLeaderRole);

        Assert.Equal(org.Id, dashboard.OrganisationId);
        Assert.Equal("Summit School", dashboard.OrganisationName);
        Assert.Equal(1, dashboard.Kpis.TotalStudents);
        Assert.Equal(1, dashboard.Kpis.TotalClasses);
        Assert.Equal(1, dashboard.Kpis.ActiveInterventions);
        Assert.Equal(1, dashboard.Kpis.ActiveLearningGaps);
        Assert.Equal(1, dashboard.Kpis.StudentsNeedingAttention);
        Assert.Contains(dashboard.Kpis.MasteryLevelCounts, pair => pair.Key == "Developing" && pair.Value == 1);
        Assert.Single(dashboard.YearLevels);
        Assert.Equal("Year 7", dashboard.YearLevels[0].YearLevelName);
        Assert.Single(dashboard.ClassComparisons);
        Assert.Equal("7A", dashboard.ClassComparisons[0].ClassName);
        Assert.Contains(_assessmentClient.RequestedClasses, pair => pair.ClassId == schoolClass.Id);
        Assert.Contains(_eiClient.RequestedClasses, pair => pair.ClassId == schoolClass.Id);
    }

    [Fact]
    public async Task SchoolLeader_DashboardNumbers_MatchComputedExpectationsFromSeededOrgClassData()
    {
        var adminId = Guid.NewGuid().ToString();
        var leaderId = Guid.NewGuid().ToString();
        var student7A1 = Guid.NewGuid().ToString();
        var student7A2 = Guid.NewGuid().ToString();
        var student7B = Guid.NewGuid().ToString();
        var student8A = Guid.NewGuid().ToString();
        var microSkillId = Guid.CreateVersion7();
        var assessment7A = Guid.CreateVersion7();
        var assessment7B = Guid.CreateVersion7();
        var assessment8A = Guid.CreateVersion7();

        var org = await CreateOrganisationAsAdminAsync(adminId, "Computed Seed School", UniqueCode("CSS"));
        var year7 = await CreateYearLevelAsAdminAsync(adminId, org.Id, "Year 7", 7);
        var year8 = await CreateYearLevelAsAdminAsync(adminId, org.Id, "Year 8", 8);
        var class7A = await CreateClassAsAdminAsync(adminId, org.Id, year7.Id, "7A", UniqueCode("7A"));
        var class7B = await CreateClassAsAdminAsync(adminId, org.Id, year7.Id, "7B", UniqueCode("7B"));
        var class8A = await CreateClassAsAdminAsync(adminId, org.Id, year8.Id, "8A", UniqueCode("8A"));
        await EnrollStudentAsync(adminId, org.Id, class7A.Id, student7A1);
        await EnrollStudentAsync(adminId, org.Id, class7A.Id, student7A2);
        await EnrollStudentAsync(adminId, org.Id, class7B.Id, student7B);
        await EnrollStudentAsync(adminId, org.Id, class8A.Id, student8A);
        await AssignSchoolLeaderAsync(adminId, org.Id, leaderId);

        var assessmentsByClass = new Dictionary<Guid, IReadOnlyList<AssessmentSummaryData>>
        {
            [class7A.Id] =
            [
                new AssessmentSummaryData(assessment7A, "7A Quiz", "Published", DateTimeOffset.UtcNow.AddDays(3), 1)
            ],
            [class7B.Id] =
            [
                new AssessmentSummaryData(assessment7B, "7B Quiz", "Published", DateTimeOffset.UtcNow.AddDays(4), 1)
            ],
            [class8A.Id] =
            [
                new AssessmentSummaryData(assessment8A, "8A Quiz", "Published", DateTimeOffset.UtcNow.AddDays(5), 0)
            ]
        };
        _assessmentClient.SummariesByClass = assessmentsByClass;

        _interventionClient.InterventionsByStudent[student7A1] =
        [
            new ParentInterventionSummaryData(
                Guid.CreateVersion7(), "Support A1", "Active", null, null)
        ];
        _interventionClient.InterventionsByStudent[student7A2] =
        [
            new ParentInterventionSummaryData(
                Guid.CreateVersion7(), "Support A2", "Active", null, null),
            new ParentInterventionSummaryData(
                Guid.CreateVersion7(), "Support A2b", "Active", null, null)
        ];

        var insights7A = new ClassEiInsightsData(
            org.Id,
            class7A.Id,
            [
                new ClassMasteryDistributionData(
                    microSkillId,
                    new Dictionary<string, int> { ["Developing"] = 1, ["Secure"] = 1 },
                    2,
                    "7A mastery.",
                    [])
            ],
            [
                new ClassActiveGapData(
                    Guid.CreateVersion7(), microSkillId, "High", "Medium", student7A1, "Gap A1", Guid.CreateVersion7()),
                new ClassActiveGapData(
                    Guid.CreateVersion7(), microSkillId, "Medium", "Low", student7A2, "Gap A2", Guid.CreateVersion7())
            ],
            [],
            [
                new StudentNeedingAttentionData(student7A1, "Gap", "Needs attention.", null)
            ]);
        var insights7B = new ClassEiInsightsData(
            org.Id,
            class7B.Id,
            [
                new ClassMasteryDistributionData(
                    microSkillId,
                    new Dictionary<string, int> { ["Developing"] = 1 },
                    1,
                    "7B mastery.",
                    [])
            ],
            [
                new ClassActiveGapData(
                    Guid.CreateVersion7(), microSkillId, "Low", "Low", student7B, "Gap B", Guid.CreateVersion7())
            ],
            [],
            []);
        var insights8A = EmptyInsights(org.Id, class8A.Id);

        _eiClient.InsightsByClass[(org.Id, class7A.Id)] = insights7A;
        _eiClient.InsightsByClass[(org.Id, class7B.Id)] = insights7B;
        _eiClient.InsightsByClass[(org.Id, class8A.Id)] = insights8A;

        var expectedAggregations = new[]
        {
            LeadershipDashboardAggregator.AggregateClass(new ClassAggregationInput(
                class7A.Id, "7A", year7.Id, "Year 7",
                [student7A1, student7A2], assessmentsByClass[class7A.Id], ActiveInterventions: 3, insights7A)),
            LeadershipDashboardAggregator.AggregateClass(new ClassAggregationInput(
                class7B.Id, "7B", year7.Id, "Year 7",
                [student7B], assessmentsByClass[class7B.Id], ActiveInterventions: 0, insights7B)),
            LeadershipDashboardAggregator.AggregateClass(new ClassAggregationInput(
                class8A.Id, "8A", year8.Id, "Year 8",
                [student8A], assessmentsByClass[class8A.Id], ActiveInterventions: 0, insights8A))
        };
        var expectedKpis = LeadershipDashboardAggregator.RollUpKpis(expectedAggregations);
        var expectedYearLevels = LeadershipDashboardAggregator.RollUpYearLevels(expectedAggregations);
        var expectedComparisons = LeadershipDashboardAggregator.ToClassComparisons(expectedAggregations);

        var dashboard = await SendAsAsync<LeadershipDashboardResponse>(
            HttpMethod.Get,
            $"/api/v1/organisations/{org.Id}/leadership/dashboard",
            leaderId,
            org.Id,
            TestJwt.SchoolLeaderRole);

        Assert.Equal(org.Id, dashboard.OrganisationId);
        Assert.Equal("Computed Seed School", dashboard.OrganisationName);
        Assert.Equal(expectedKpis.TotalStudents, dashboard.Kpis.TotalStudents);
        Assert.Equal(expectedKpis.TotalClasses, dashboard.Kpis.TotalClasses);
        Assert.Equal(expectedKpis.ActiveInterventions, dashboard.Kpis.ActiveInterventions);
        Assert.Equal(expectedKpis.ActiveLearningGaps, dashboard.Kpis.ActiveLearningGaps);
        Assert.Equal(expectedKpis.StudentsNeedingAttention, dashboard.Kpis.StudentsNeedingAttention);
        Assert.Equal(expectedKpis.AssessmentCompletionRate, dashboard.Kpis.AssessmentCompletionRate);
        Assert.Equal(4, dashboard.Kpis.TotalStudents);
        Assert.Equal(3, dashboard.Kpis.TotalClasses);
        Assert.Equal(3, dashboard.Kpis.ActiveInterventions);
        Assert.Equal(3, dashboard.Kpis.ActiveLearningGaps);
        Assert.Equal(1, dashboard.Kpis.StudentsNeedingAttention);
        Assert.Equal(2, dashboard.Kpis.MasteryLevelCounts["Developing"]);
        Assert.Equal(1, dashboard.Kpis.MasteryLevelCounts["Secure"]);
        foreach (var (level, count) in expectedKpis.MasteryLevelCounts)
        {
            Assert.Equal(count, dashboard.Kpis.MasteryLevelCounts[level]);
        }

        Assert.Equal(expectedYearLevels.Count, dashboard.YearLevels.Count);
        Assert.Equal(expectedYearLevels[0].YearLevelId, dashboard.YearLevels[0].YearLevelId);
        Assert.Equal(2, dashboard.YearLevels.Single(y => y.YearLevelName == "Year 7").ClassCount);
        Assert.Equal(3, dashboard.YearLevels.Single(y => y.YearLevelName == "Year 7").StudentCount);
        Assert.Equal(1, dashboard.YearLevels.Single(y => y.YearLevelName == "Year 8").StudentCount);

        Assert.Equal(expectedComparisons.Count, dashboard.ClassComparisons.Count);
        Assert.Equal(
            expectedComparisons.Single(c => c.ClassId == class7A.Id).AssessmentCompletionRate,
            dashboard.ClassComparisons.Single(c => c.ClassId == class7A.Id).AssessmentCompletionRate);
        Assert.Equal(0.5m, dashboard.ClassComparisons.Single(c => c.ClassId == class7A.Id).AssessmentCompletionRate);
        Assert.Equal(1m, dashboard.ClassComparisons.Single(c => c.ClassId == class7B.Id).AssessmentCompletionRate);
        Assert.Equal(0m, dashboard.ClassComparisons.Single(c => c.ClassId == class8A.Id).AssessmentCompletionRate);
    }

    [Fact]
    public async Task Teacher_CannotViewLeadershipDashboard()
    {
        var adminId = Guid.NewGuid().ToString();
        var teacherId = Guid.NewGuid().ToString();
        var org = await CreateOrganisationAsAdminAsync(adminId, "Valley School", UniqueCode("VAL"));
        var year = await CreateYearLevelAsAdminAsync(adminId, org.Id, "Year 5", 5);
        var schoolClass = await CreateClassAsAdminAsync(adminId, org.Id, year.Id, "5A", UniqueCode("5A"));
        await AssignTeacherAsync(adminId, org.Id, schoolClass.Id, teacherId);

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/organisations/{org.Id}/leadership/dashboard",
            teacherId,
            org.Id,
            TestJwt.TeacherRole);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(request)).StatusCode);
    }

    [Fact]
    public async Task SchoolLeader_CannotViewLeadershipDashboardForUnassignedOrganisation()
    {
        var adminId = Guid.NewGuid().ToString();
        var leaderId = Guid.NewGuid().ToString();
        var org = await CreateOrganisationAsAdminAsync(adminId, "Ridge School", UniqueCode("RID"));
        await CreateYearLevelAsAdminAsync(adminId, org.Id, "Year 6", 6);

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/organisations/{org.Id}/leadership/dashboard",
            leaderId,
            org.Id,
            TestJwt.SchoolLeaderRole);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(request)).StatusCode);
    }

    [Fact]
    public async Task SchoolLeader_CanDrillDownToYearLevel()
    {
        var adminId = Guid.NewGuid().ToString();
        var leaderId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();

        var org = await CreateOrganisationAsAdminAsync(adminId, "Harbour School", UniqueCode("HAR"));
        var year7 = await CreateYearLevelAsAdminAsync(adminId, org.Id, "Year 7", 7);
        var year8 = await CreateYearLevelAsAdminAsync(adminId, org.Id, "Year 8", 8);
        var class7A = await CreateClassAsAdminAsync(adminId, org.Id, year7.Id, "7A", UniqueCode("7A"));
        var class8A = await CreateClassAsAdminAsync(adminId, org.Id, year8.Id, "8A", UniqueCode("8A"));
        await EnrollStudentAsync(adminId, org.Id, class7A.Id, studentId);
        await AssignSchoolLeaderAsync(adminId, org.Id, leaderId);

        _eiClient.InsightsByClass[(org.Id, class7A.Id)] = EmptyInsights(org.Id, class7A.Id);
        _eiClient.InsightsByClass[(org.Id, class8A.Id)] = EmptyInsights(org.Id, class8A.Id);

        var dashboard = await SendAsAsync<YearLevelLeadershipDashboardResponse>(
            HttpMethod.Get,
            $"/api/v1/organisations/{org.Id}/year-levels/{year7.Id}/leadership/dashboard",
            leaderId,
            org.Id,
            TestJwt.SchoolLeaderRole);

        Assert.Equal(year7.Id, dashboard.YearLevelId);
        Assert.Equal("Year 7", dashboard.YearLevelName);
        Assert.Single(dashboard.ClassComparisons);
        Assert.Equal(class7A.Id, dashboard.ClassComparisons[0].ClassId);
    }

    [Fact]
    public async Task SchoolLeader_CanDrillDownToClassSummary()
    {
        var adminId = Guid.NewGuid().ToString();
        var leaderId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var assessmentId = Guid.CreateVersion7();

        var org = await CreateOrganisationAsAdminAsync(adminId, "Cove School", UniqueCode("COV"));
        var year = await CreateYearLevelAsAdminAsync(adminId, org.Id, "Year 9", 9);
        var schoolClass = await CreateClassAsAdminAsync(adminId, org.Id, year.Id, "9A", UniqueCode("9A"));
        await EnrollStudentAsync(adminId, org.Id, schoolClass.Id, studentId);
        await AssignSchoolLeaderAsync(adminId, org.Id, leaderId);

        _assessmentClient.Summaries =
        [
            new AssessmentSummaryData(assessmentId, "Algebra check", "Published", DateTimeOffset.UtcNow.AddDays(1), 1)
        ];
        _eiClient.InsightsByClass[(org.Id, schoolClass.Id)] = EmptyInsights(org.Id, schoolClass.Id);

        var summary = await SendAsAsync<ClassLeadershipSummaryResponse>(
            HttpMethod.Get,
            $"/api/v1/organisations/{org.Id}/classes/{schoolClass.Id}/leadership/summary",
            leaderId,
            org.Id,
            TestJwt.SchoolLeaderRole);

        Assert.Equal(schoolClass.Id, summary.Class.Id);
        Assert.Single(summary.Students);
        Assert.Equal(studentId, summary.Students[0].UserId);
        Assert.Single(summary.RecentAssessments);
        Assert.Equal("Algebra check", summary.RecentAssessments[0].Title);
    }

    [Fact]
    public async Task SchoolLeader_CanListOrganisationsAndClassesInAssignedSchool()
    {
        var adminId = Guid.NewGuid().ToString();
        var leaderId = Guid.NewGuid().ToString();
        var org = await CreateOrganisationAsAdminAsync(adminId, "Lake School", UniqueCode("LAK"));
        var year = await CreateYearLevelAsAdminAsync(adminId, org.Id, "Year 4", 4);
        await CreateClassAsAdminAsync(adminId, org.Id, year.Id, "4A", UniqueCode("4A"));
        await AssignSchoolLeaderAsync(adminId, org.Id, leaderId);

        var organisations = await SendAsAsync<List<OrganisationResponse>>(
            HttpMethod.Get,
            "/api/v1/organisations",
            leaderId,
            org.Id,
            TestJwt.SchoolLeaderRole);
        Assert.Single(organisations);
        Assert.Equal(org.Id, organisations[0].Id);

        var classes = await SendAsAsync<List<ClassResponse>>(
            HttpMethod.Get,
            $"/api/v1/organisations/{org.Id}/classes",
            leaderId,
            org.Id,
            TestJwt.SchoolLeaderRole);
        Assert.Single(classes);
    }

    [Fact]
    public async Task SchoolLeader_CanListLeadershipInterventionsWithAggregation()
    {
        var adminId = Guid.NewGuid().ToString();
        var leaderId = Guid.NewGuid().ToString();
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var learningGapId = Guid.CreateVersion7();
        var interventionId = Guid.CreateVersion7();

        var org = await CreateOrganisationAsAdminAsync(adminId, "Pine School", UniqueCode("PIN"));
        var year = await CreateYearLevelAsAdminAsync(adminId, org.Id, "Year 7", 7);
        var schoolClass = await CreateClassAsAdminAsync(adminId, org.Id, year.Id, "7B", UniqueCode("7B"));
        await EnrollStudentAsync(adminId, org.Id, schoolClass.Id, studentId);
        await AssignSchoolLeaderAsync(adminId, org.Id, leaderId);

        _interventionClient.OrganisationInterventions[(org.Id, null)] =
        [
            new InterventionDetailData(
                interventionId,
                org.Id,
                studentId,
                learningGapId,
                teacherId,
                "Guided reading support.",
                null,
                "Active",
                DateTimeOffset.UtcNow.AddDays(-2),
                DateTimeOffset.UtcNow.AddDays(14),
                null,
                DateTimeOffset.UtcNow.AddDays(-2),
                DateTimeOffset.UtcNow)
        ];
        _eiClient.InsightsByClass[(org.Id, schoolClass.Id)] = new ClassEiInsightsData(
            org.Id,
            schoolClass.Id,
            [],
            [
                new ClassActiveGapData(
                    learningGapId,
                    Guid.CreateVersion7(),
                    "High",
                    "Medium",
                    studentId,
                    "Gap detected.",
                    Guid.CreateVersion7())
            ],
            [],
            []);

        var monitoring = await SendAsAsync<LeadershipInterventionMonitoringResponse>(
            HttpMethod.Get,
            $"/api/v1/organisations/{org.Id}/leadership/interventions",
            leaderId,
            org.Id,
            TestJwt.SchoolLeaderRole);

        Assert.Equal(org.Id, monitoring.OrganisationId);
        var item = Assert.Single(monitoring.Interventions);
        Assert.Equal(interventionId, item.InterventionId);
        Assert.Equal(studentId, item.StudentUserId);
        Assert.Equal(learningGapId, item.LearningGapId);
        Assert.Equal(teacherId, item.AssignedTeacherUserId);
        Assert.Equal("Active", item.Status);
        Assert.Equal(schoolClass.Id, item.ClassId);
        Assert.Equal("7B", item.ClassName);
        Assert.Equal(year.Id, item.YearLevelId);
        Assert.Equal("Year 7", item.YearLevelName);
        Assert.Equal("High", item.GapSeverity);
    }

    [Fact]
    public async Task SchoolLeader_CanFilterLeadershipInterventionsByClassAndSeverity()
    {
        var adminId = Guid.NewGuid().ToString();
        var leaderId = Guid.NewGuid().ToString();
        var student7 = Guid.NewGuid().ToString();
        var student8 = Guid.NewGuid().ToString();
        var gap7 = Guid.CreateVersion7();
        var gap8 = Guid.CreateVersion7();

        var org = await CreateOrganisationAsAdminAsync(adminId, "Oak School", UniqueCode("OAK"));
        var year7 = await CreateYearLevelAsAdminAsync(adminId, org.Id, "Year 7", 7);
        var year8 = await CreateYearLevelAsAdminAsync(adminId, org.Id, "Year 8", 8);
        var class7A = await CreateClassAsAdminAsync(adminId, org.Id, year7.Id, "7A", UniqueCode("7A"));
        var class8A = await CreateClassAsAdminAsync(adminId, org.Id, year8.Id, "8A", UniqueCode("8A"));
        await EnrollStudentAsync(adminId, org.Id, class7A.Id, student7);
        await EnrollStudentAsync(adminId, org.Id, class8A.Id, student8);
        await AssignSchoolLeaderAsync(adminId, org.Id, leaderId);

        _interventionClient.OrganisationInterventions[(org.Id, null)] =
        [
            new InterventionDetailData(
                Guid.CreateVersion7(), org.Id, student7, gap7, Guid.NewGuid().ToString(),
                "Year 7 support.", null, "Active", null, null, null,
                DateTimeOffset.UtcNow, DateTimeOffset.UtcNow),
            new InterventionDetailData(
                Guid.CreateVersion7(), org.Id, student8, gap8, Guid.NewGuid().ToString(),
                "Year 8 support.", null, "Planned", null, null, null,
                DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)
        ];
        _eiClient.InsightsByClass[(org.Id, class7A.Id)] = new ClassEiInsightsData(
            org.Id, class7A.Id, [], [new ClassActiveGapData(gap7, Guid.CreateVersion7(), "High", "Medium", student7, "", Guid.CreateVersion7())], [], []);
        _eiClient.InsightsByClass[(org.Id, class8A.Id)] = new ClassEiInsightsData(
            org.Id, class8A.Id, [], [new ClassActiveGapData(gap8, Guid.CreateVersion7(), "Low", "Low", student8, "", Guid.CreateVersion7())], [], []);

        var classFiltered = await SendAsAsync<LeadershipInterventionMonitoringResponse>(
            HttpMethod.Get,
            $"/api/v1/organisations/{org.Id}/leadership/interventions?classId={class7A.Id}",
            leaderId,
            org.Id,
            TestJwt.SchoolLeaderRole);
        Assert.Single(classFiltered.Interventions);
        Assert.Equal(student7, classFiltered.Interventions[0].StudentUserId);

        var severityFiltered = await SendAsAsync<LeadershipInterventionMonitoringResponse>(
            HttpMethod.Get,
            $"/api/v1/organisations/{org.Id}/leadership/interventions?severity=High",
            leaderId,
            org.Id,
            TestJwt.SchoolLeaderRole);
        Assert.Single(severityFiltered.Interventions);
        Assert.Equal("High", severityFiltered.Interventions[0].GapSeverity);

        var yearFiltered = await SendAsAsync<LeadershipInterventionMonitoringResponse>(
            HttpMethod.Get,
            $"/api/v1/organisations/{org.Id}/leadership/interventions?yearLevelId={year8.Id}",
            leaderId,
            org.Id,
            TestJwt.SchoolLeaderRole);
        Assert.Single(yearFiltered.Interventions);
        Assert.Equal(student8, yearFiltered.Interventions[0].StudentUserId);
    }

    [Fact]
    public async Task Teacher_CannotListLeadershipInterventions()
    {
        var adminId = Guid.NewGuid().ToString();
        var teacherId = Guid.NewGuid().ToString();
        var org = await CreateOrganisationAsAdminAsync(adminId, "Elm School", UniqueCode("ELM"));
        var year = await CreateYearLevelAsAdminAsync(adminId, org.Id, "Year 6", 6);
        var schoolClass = await CreateClassAsAdminAsync(adminId, org.Id, year.Id, "6A", UniqueCode("6A"));
        await AssignTeacherAsync(adminId, org.Id, schoolClass.Id, teacherId);

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/organisations/{org.Id}/leadership/interventions",
            teacherId,
            org.Id,
            TestJwt.TeacherRole);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(request)).StatusCode);
    }

    private static ClassEiInsightsData EmptyInsights(Guid organisationId, Guid classId) =>
        new(organisationId, classId, [], [], [], []);

    private async Task<OrganisationResponse> CreateOrganisationAsAdminAsync(string adminId, string name, string code)
    {
        using var request = TestJwt.Authorized(HttpMethod.Post, "/api/v1/organisations", adminId, TestJwt.AdminRole);
        request.Content = JsonContent.Create(new CreateOrganisationRequest(name, code));
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<OrganisationResponse>())!;
    }

    private async Task<YearLevelResponse> CreateYearLevelAsAdminAsync(
        string adminId,
        Guid organisationId,
        string name,
        int sortOrder)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/organisations/{organisationId}/year-levels",
            adminId,
            organisationId,
            TestJwt.AdminRole);
        request.Content = JsonContent.Create(new CreateYearLevelRequest(name, sortOrder));
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<YearLevelResponse>())!;
    }

    private async Task<ClassResponse> CreateClassAsAdminAsync(
        string adminId,
        Guid organisationId,
        Guid yearLevelId,
        string name,
        string code)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/organisations/{organisationId}/classes",
            adminId,
            organisationId,
            TestJwt.AdminRole);
        request.Content = JsonContent.Create(new CreateClassRequest(name, code, yearLevelId));
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ClassResponse>())!;
    }

    private async Task EnrollStudentAsync(string adminId, Guid organisationId, Guid classId, string studentId)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/organisations/{organisationId}/classes/{classId}/enrollments",
            adminId,
            organisationId,
            TestJwt.AdminRole);
        request.Content = JsonContent.Create(new EnrollStudentRequest(studentId));
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private async Task AssignTeacherAsync(string adminId, Guid organisationId, Guid classId, string teacherId)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/organisations/{organisationId}/classes/{classId}/teachers",
            adminId,
            organisationId,
            TestJwt.AdminRole);
        request.Content = JsonContent.Create(new AssignTeacherRequest(teacherId));
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private async Task AssignSchoolLeaderAsync(string adminId, Guid organisationId, string leaderId)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/organisations/{organisationId}/leaders",
            adminId,
            organisationId,
            TestJwt.AdminRole);
        request.Content = JsonContent.Create(new AssignSchoolLeaderRequest(leaderId));
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private async Task<T> SendAsAsync<T>(HttpMethod method, string url, string userId, Guid? tenantId, string role)
    {
        using var request = tenantId.HasValue
            ? TestJwt.Authorized(method, url, userId, tenantId.Value, role)
            : TestJwt.Authorized(method, url, userId, role);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    private static string UniqueCode(string prefix) => $"{prefix}{Guid.NewGuid():N}"[..12].ToUpperInvariant();
}
