using System.Net;
using System.Net.Http.Json;
using CurriculumService.Application;
using CurriculumService.Domain;

namespace CurriculumService.Tests;

public class CurriculumVariantEndpointTests : IClassFixture<CurriculumWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CurriculumVariantEndpointTests(CurriculumWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Authority_CanDefineRegionalCurriculumVariant_WithSubjectsUnitsAndLearningObjectives()
    {
        var authorityId = Guid.NewGuid().ToString();
        var authorityOrgId = Guid.CreateVersion7();

        var regional = await CreateRegionalVariantAsync(
            authorityId,
            authorityOrgId,
            "NZ Science Framework",
            "2026",
            "NZ-NCEA");
        Assert.Equal(CurriculumScopes.Regional, regional.Scope);
        Assert.Equal("NZ-NCEA", regional.RegionCode);
        Assert.Null(regional.ParentCurriculumId);

        var subject = await CreateSubjectAsync(
            authorityId,
            authorityOrgId,
            regional.Id,
            "Science",
            "SCI",
            1);
        var unit = await CreateUnitAsync(
            authorityId,
            authorityOrgId,
            regional.Id,
            subject.Id,
            "Living World",
            1);
        var objective = await CreateLearningObjectiveAsync(
            authorityId,
            authorityOrgId,
            regional.Id,
            subject.Id,
            unit.Id,
            "Describe ecosystems",
            1);

        var tree = await SendAsAsync<CurriculumTreeResponse>(
            HttpMethod.Get,
            $"/api/v1/curriculum/{regional.Id}/tree",
            authorityId,
            authorityOrgId);

        Assert.Equal(CurriculumScopes.Regional, tree.Scope);
        Assert.Equal("NZ-NCEA", tree.RegionCode);
        var subjectNode = Assert.Single(tree.Subjects);
        Assert.Equal(subject.Id, subjectNode.Id);
        var unitNode = Assert.Single(subjectNode.Units);
        Assert.Equal(unit.Id, unitNode.Id);
        Assert.Equal(objective.Id, Assert.Single(unitNode.LearningObjectives).Id);
    }

    [Fact]
    public async Task School_InheritsRegionalCurriculumVariant_WithClonedTreeAndSourceLinks()
    {
        var authorityId = Guid.NewGuid().ToString();
        var authorityOrgId = Guid.CreateVersion7();
        var schoolTeacherId = Guid.NewGuid().ToString();
        var schoolOrgId = Guid.CreateVersion7();

        var regional = await SeedRegionalScienceAsync(authorityId, authorityOrgId);
        var regionalTree = await SendAsAsync<CurriculumTreeResponse>(
            HttpMethod.Get,
            $"/api/v1/curriculum/{regional.Id}/tree",
            authorityId,
            authorityOrgId);
        var regionalSubject = Assert.Single(regionalTree.Subjects);
        var regionalUnit = Assert.Single(regionalSubject.Units);
        var regionalObjective = Assert.Single(regionalUnit.LearningObjectives);
        var regionalMicroSkill = Assert.Single(regionalObjective.MicroSkills);

        var inherited = await InheritAsync(schoolTeacherId, schoolOrgId, regional.Id);
        Assert.Equal(CurriculumScopes.School, inherited.Scope);
        Assert.Equal(regional.Id, inherited.ParentCurriculumId);
        Assert.Equal(regional.RegionCode, inherited.RegionCode);
        Assert.Equal(schoolOrgId, inherited.OrganisationId);

        var schoolTree = await SendAsAsync<CurriculumTreeResponse>(
            HttpMethod.Get,
            $"/api/v1/curriculum/{inherited.Id}/tree",
            schoolTeacherId,
            schoolOrgId);

        var schoolSubject = Assert.Single(schoolTree.Subjects);
        Assert.Equal(regionalSubject.Name, schoolSubject.Name);
        Assert.Equal(regionalSubject.Id, schoolSubject.SourceNodeId);
        Assert.False(schoolSubject.IsOverridden);

        var schoolUnit = Assert.Single(schoolSubject.Units);
        Assert.Equal(regionalUnit.Name, schoolUnit.Name);
        Assert.Equal(regionalUnit.Id, schoolUnit.SourceNodeId);
        Assert.False(schoolUnit.IsOverridden);

        var schoolObjective = Assert.Single(schoolUnit.LearningObjectives);
        Assert.Equal(regionalObjective.Title, schoolObjective.Title);
        Assert.Equal(regionalObjective.Id, schoolObjective.SourceNodeId);
        Assert.NotEqual(regionalObjective.Id, schoolObjective.Id);
        Assert.False(schoolObjective.IsOverridden);

        var schoolMicroSkill = Assert.Single(schoolObjective.MicroSkills);
        Assert.Equal(regionalMicroSkill.Name, schoolMicroSkill.Name);
        Assert.Equal(regionalMicroSkill.Id, schoolMicroSkill.SourceNodeId);
        Assert.NotEqual(regionalMicroSkill.Id, schoolMicroSkill.Id);
    }

    [Fact]
    public async Task School_CanOverrideUnitAndLearningObjective_WithoutAffectingRegionalOrOtherSchools()
    {
        var authorityId = Guid.NewGuid().ToString();
        var authorityOrgId = Guid.CreateVersion7();
        var schoolATeacherId = Guid.NewGuid().ToString();
        var schoolAOrgId = Guid.CreateVersion7();
        var schoolBTeacherId = Guid.NewGuid().ToString();
        var schoolBOrgId = Guid.CreateVersion7();

        var regional = await SeedRegionalScienceAsync(authorityId, authorityOrgId);
        var schoolA = await InheritAsync(schoolATeacherId, schoolAOrgId, regional.Id);
        var schoolB = await InheritAsync(schoolBTeacherId, schoolBOrgId, regional.Id);

        var schoolATree = await SendAsAsync<CurriculumTreeResponse>(
            HttpMethod.Get,
            $"/api/v1/curriculum/{schoolA.Id}/tree",
            schoolATeacherId,
            schoolAOrgId);
        var schoolASubject = Assert.Single(schoolATree.Subjects);
        var schoolAUnit = Assert.Single(schoolASubject.Units);
        var schoolAObjective = Assert.Single(schoolAUnit.LearningObjectives);

        using var overrideUnit = TestJwt.Authorized(
            HttpMethod.Put,
            $"/api/v1/curriculum/{schoolA.Id}/subjects/{schoolASubject.Id}/units/{schoolAUnit.Id}",
            schoolATeacherId,
            schoolAOrgId,
            TestJwt.TeacherRole);
        overrideUnit.Content = JsonContent.Create(new UpdateUnitRequest("Living World — School Pathway", 1));
        var overrideUnitResponse = await _client.SendAsync(overrideUnit);
        Assert.Equal(HttpStatusCode.OK, overrideUnitResponse.StatusCode);

        using var overrideObjective = TestJwt.Authorized(
            HttpMethod.Put,
            $"/api/v1/curriculum/{schoolA.Id}/subjects/{schoolASubject.Id}/units/{schoolAUnit.Id}/learning-objectives/{schoolAObjective.Id}",
            schoolATeacherId,
            schoolAOrgId,
            TestJwt.TeacherRole);
        overrideObjective.Content = JsonContent.Create(
            new UpdateLearningObjectiveRequest("Describe local ecosystems", 1));
        var overrideObjectiveResponse = await _client.SendAsync(overrideObjective);
        Assert.Equal(HttpStatusCode.OK, overrideObjectiveResponse.StatusCode);

        var overriddenTree = await SendAsAsync<CurriculumTreeResponse>(
            HttpMethod.Get,
            $"/api/v1/curriculum/{schoolA.Id}/tree",
            schoolATeacherId,
            schoolAOrgId);
        var overriddenSubject = Assert.Single(overriddenTree.Subjects);
        var overriddenUnit = Assert.Single(overriddenSubject.Units);
        Assert.Equal("Living World — School Pathway", overriddenUnit.Name);
        Assert.True(overriddenUnit.IsOverridden);
        var overriddenObjective = Assert.Single(overriddenUnit.LearningObjectives);
        Assert.Equal("Describe local ecosystems", overriddenObjective.Title);
        Assert.True(overriddenObjective.IsOverridden);

        var regionalTree = await SendAsAsync<CurriculumTreeResponse>(
            HttpMethod.Get,
            $"/api/v1/curriculum/{regional.Id}/tree",
            authorityId,
            authorityOrgId);
        var regionalUnit = Assert.Single(Assert.Single(regionalTree.Subjects).Units);
        Assert.Equal("Living World", regionalUnit.Name);
        Assert.False(regionalUnit.IsOverridden);
        Assert.Equal(
            "Describe ecosystems",
            Assert.Single(regionalUnit.LearningObjectives).Title);

        var schoolBTree = await SendAsAsync<CurriculumTreeResponse>(
            HttpMethod.Get,
            $"/api/v1/curriculum/{schoolB.Id}/tree",
            schoolBTeacherId,
            schoolBOrgId);
        var schoolBUnit = Assert.Single(Assert.Single(schoolBTree.Subjects).Units);
        Assert.Equal("Living World", schoolBUnit.Name);
        Assert.False(schoolBUnit.IsOverridden);
        Assert.Equal(
            "Describe ecosystems",
            Assert.Single(schoolBUnit.LearningObjectives).Title);
    }

    [Fact]
    public async Task CurriculumVariants_AreIsolatedPerTenantAndRegion()
    {
        var nzAuthorityId = Guid.NewGuid().ToString();
        var nzOrgId = Guid.CreateVersion7();
        var auAuthorityId = Guid.NewGuid().ToString();
        var auOrgId = Guid.CreateVersion7();
        var schoolTeacherId = Guid.NewGuid().ToString();
        var schoolOrgId = Guid.CreateVersion7();

        var nzRegional = await CreateRegionalVariantAsync(
            nzAuthorityId,
            nzOrgId,
            "NZ Framework",
            "2026",
            "NZ-NCEA");
        var auRegional = await CreateRegionalVariantAsync(
            auAuthorityId,
            auOrgId,
            "AU Framework",
            "2026",
            "AU-ACARA");

        var schoolVariant = await InheritAsync(schoolTeacherId, schoolOrgId, nzRegional.Id);

        using var crossTenantGet = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/curriculum/{schoolVariant.Id}",
            auAuthorityId,
            auOrgId,
            TestJwt.TeacherRole);
        var crossTenantResponse = await _client.SendAsync(crossTenantGet);
        Assert.Equal(HttpStatusCode.Forbidden, crossTenantResponse.StatusCode);

        using var crossRegionInherit = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/curriculum/{auRegional.Id}/inherit",
            schoolTeacherId,
            schoolOrgId,
            TestJwt.TeacherRole);
        crossRegionInherit.Content = JsonContent.Create(new InheritCurriculumRequest(schoolOrgId));
        var crossRegionResponse = await _client.SendAsync(crossRegionInherit);
        Assert.Equal(HttpStatusCode.Created, crossRegionResponse.StatusCode);
        var auInherited = await crossRegionResponse.Content.ReadFromJsonAsync<CurriculumResponse>();
        Assert.Equal("AU-ACARA", auInherited!.RegionCode);
        Assert.NotEqual(schoolVariant.Id, auInherited.Id);

        var nzListed = await SendAsAsync<List<CurriculumResponse>>(
            HttpMethod.Get,
            $"/api/v1/curriculum?organisationId={schoolOrgId}&regionCode=NZ-NCEA",
            schoolTeacherId,
            schoolOrgId);
        Assert.Contains(nzListed, item => item.Id == schoolVariant.Id);
        Assert.DoesNotContain(nzListed, item => item.Id == auInherited.Id);

        var auListed = await SendAsAsync<List<CurriculumResponse>>(
            HttpMethod.Get,
            $"/api/v1/curriculum?organisationId={schoolOrgId}&regionCode=AU-ACARA",
            schoolTeacherId,
            schoolOrgId);
        Assert.Contains(auListed, item => item.Id == auInherited.Id);
        Assert.DoesNotContain(auListed, item => item.Id == schoolVariant.Id);
    }

    [Fact]
    public async Task AssessmentsCanLinkToVariantSpecificLearningObjectivesAndMicroSkills()
    {
        var authorityId = Guid.NewGuid().ToString();
        var authorityOrgId = Guid.CreateVersion7();
        var schoolTeacherId = Guid.NewGuid().ToString();
        var schoolOrgId = Guid.CreateVersion7();

        var regional = await SeedRegionalScienceAsync(authorityId, authorityOrgId);
        var regionalTree = await SendAsAsync<CurriculumTreeResponse>(
            HttpMethod.Get,
            $"/api/v1/curriculum/{regional.Id}/tree",
            authorityId,
            authorityOrgId);
        var regionalObjective = Assert.Single(Assert.Single(Assert.Single(regionalTree.Subjects).Units).LearningObjectives);
        var regionalMicroSkill = Assert.Single(regionalObjective.MicroSkills);

        var schoolVariant = await InheritAsync(schoolTeacherId, schoolOrgId, regional.Id);
        var schoolTree = await SendAsAsync<CurriculumTreeResponse>(
            HttpMethod.Get,
            $"/api/v1/curriculum/{schoolVariant.Id}/tree",
            schoolTeacherId,
            schoolOrgId);
        var schoolObjective = Assert.Single(Assert.Single(Assert.Single(schoolTree.Subjects).Units).LearningObjectives);
        var schoolMicroSkill = Assert.Single(schoolObjective.MicroSkills);

        Assert.NotEqual(regionalObjective.Id, schoolObjective.Id);
        Assert.NotEqual(regionalMicroSkill.Id, schoolMicroSkill.Id);
        Assert.Equal(regionalObjective.Id, schoolObjective.SourceNodeId);
        Assert.Equal(regionalMicroSkill.Id, schoolMicroSkill.SourceNodeId);

        var links = await SendAsAsync<VariantCurriculumLinksResponse>(
            HttpMethod.Get,
            $"/api/v1/curriculum/{schoolVariant.Id}/variant-links",
            schoolTeacherId,
            schoolOrgId);
        Assert.Contains(schoolObjective.Id, links.LearningObjectiveIds);
        Assert.Contains(schoolMicroSkill.Id, links.MicroSkillIds);
        Assert.DoesNotContain(regionalObjective.Id, links.LearningObjectiveIds);
        Assert.DoesNotContain(regionalMicroSkill.Id, links.MicroSkillIds);
    }

    [Fact]
    public async Task VariantLinks_ReturnsOnlyVariantSpecificLearningObjectiveAndMicroSkillIds()
    {
        var authorityId = Guid.NewGuid().ToString();
        var authorityOrgId = Guid.CreateVersion7();
        var schoolTeacherId = Guid.NewGuid().ToString();
        var schoolOrgId = Guid.CreateVersion7();

        var regional = await SeedRegionalScienceAsync(authorityId, authorityOrgId);
        var schoolVariant = await InheritAsync(schoolTeacherId, schoolOrgId, regional.Id);

        var schoolTree = await SendAsAsync<CurriculumTreeResponse>(
            HttpMethod.Get,
            $"/api/v1/curriculum/{schoolVariant.Id}/tree",
            schoolTeacherId,
            schoolOrgId);
        var schoolObjective = Assert.Single(Assert.Single(Assert.Single(schoolTree.Subjects).Units).LearningObjectives);
        var schoolMicroSkill = Assert.Single(schoolObjective.MicroSkills);

        var regionalTree = await SendAsAsync<CurriculumTreeResponse>(
            HttpMethod.Get,
            $"/api/v1/curriculum/{regional.Id}/tree",
            authorityId,
            authorityOrgId);
        var regionalObjective = Assert.Single(Assert.Single(Assert.Single(regionalTree.Subjects).Units).LearningObjectives);
        var regionalMicroSkill = Assert.Single(regionalObjective.MicroSkills);

        var schoolLinks = await SendAsAsync<VariantCurriculumLinksResponse>(
            HttpMethod.Get,
            $"/api/v1/curriculum/{schoolVariant.Id}/variant-links",
            schoolTeacherId,
            schoolOrgId);

        Assert.Equal(schoolVariant.Id, schoolLinks.CurriculumId);
        Assert.Equal([schoolObjective.Id], schoolLinks.LearningObjectiveIds);
        Assert.Equal([schoolMicroSkill.Id], schoolLinks.MicroSkillIds);
        Assert.DoesNotContain(regionalObjective.Id, schoolLinks.LearningObjectiveIds);
        Assert.DoesNotContain(regionalMicroSkill.Id, schoolLinks.MicroSkillIds);

        var regionalLinks = await SendAsAsync<VariantCurriculumLinksResponse>(
            HttpMethod.Get,
            $"/api/v1/curriculum/{regional.Id}/variant-links",
            authorityId,
            authorityOrgId);

        Assert.Equal(regional.Id, regionalLinks.CurriculumId);
        Assert.Equal([regionalObjective.Id], regionalLinks.LearningObjectiveIds);
        Assert.Equal([regionalMicroSkill.Id], regionalLinks.MicroSkillIds);
        Assert.DoesNotContain(schoolObjective.Id, regionalLinks.LearningObjectiveIds);
        Assert.DoesNotContain(schoolMicroSkill.Id, regionalLinks.MicroSkillIds);
    }

    private async Task<CurriculumResponse> SeedRegionalScienceAsync(string userId, Guid organisationId)
    {
        var regional = await CreateRegionalVariantAsync(
            userId,
            organisationId,
            "NZ Science Framework",
            "2026",
            "NZ-NCEA");
        var subject = await CreateSubjectAsync(userId, organisationId, regional.Id, "Science", "SCI", 1);
        var unit = await CreateUnitAsync(userId, organisationId, regional.Id, subject.Id, "Living World", 1);
        var objective = await CreateLearningObjectiveAsync(
            userId,
            organisationId,
            regional.Id,
            subject.Id,
            unit.Id,
            "Describe ecosystems",
            1);
        await CreateMicroSkillAsync(
            userId,
            organisationId,
            regional.Id,
            subject.Id,
            unit.Id,
            objective.Id,
            "Identify producers",
            1);
        return regional;
    }

    private async Task<CurriculumResponse> CreateRegionalVariantAsync(
        string userId,
        Guid organisationId,
        string name,
        string version,
        string regionCode)
    {
        using var request = TestJwt.Authorized(HttpMethod.Post, "/api/v1/curriculum", userId, organisationId, TestJwt.TeacherRole);
        request.Content = JsonContent.Create(new CreateCurriculumRequest(
            organisationId,
            name,
            version,
            "Draft",
            regionCode,
            CurriculumScopes.Regional));
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<CurriculumResponse>();
        return payload ?? throw new InvalidOperationException("Missing curriculum payload.");
    }

    private async Task<CurriculumResponse> InheritAsync(string userId, Guid organisationId, Guid parentCurriculumId)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/curriculum/{parentCurriculumId}/inherit",
            userId,
            organisationId,
            TestJwt.TeacherRole);
        request.Content = JsonContent.Create(new InheritCurriculumRequest(organisationId));
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<CurriculumResponse>();
        return payload ?? throw new InvalidOperationException("Missing inherited curriculum payload.");
    }

    private async Task<SubjectResponse> CreateSubjectAsync(
        string userId,
        Guid organisationId,
        Guid curriculumId,
        string name,
        string code,
        int sortOrder)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/curriculum/{curriculumId}/subjects",
            userId,
            organisationId,
            TestJwt.TeacherRole);
        request.Content = JsonContent.Create(new CreateSubjectRequest(name, code, sortOrder));
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<SubjectResponse>();
        return payload ?? throw new InvalidOperationException("Missing subject payload.");
    }

    private async Task<UnitResponse> CreateUnitAsync(
        string userId,
        Guid organisationId,
        Guid curriculumId,
        Guid subjectId,
        string name,
        int sortOrder)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/curriculum/{curriculumId}/subjects/{subjectId}/units",
            userId,
            organisationId,
            TestJwt.TeacherRole);
        request.Content = JsonContent.Create(new CreateUnitRequest(name, sortOrder));
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<UnitResponse>();
        return payload ?? throw new InvalidOperationException("Missing unit payload.");
    }

    private async Task<LearningObjectiveResponse> CreateLearningObjectiveAsync(
        string userId,
        Guid organisationId,
        Guid curriculumId,
        Guid subjectId,
        Guid unitId,
        string title,
        int sortOrder)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/curriculum/{curriculumId}/subjects/{subjectId}/units/{unitId}/learning-objectives",
            userId,
            organisationId,
            TestJwt.TeacherRole);
        request.Content = JsonContent.Create(new CreateLearningObjectiveRequest(title, sortOrder));
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<LearningObjectiveResponse>();
        return payload ?? throw new InvalidOperationException("Missing learning objective payload.");
    }

    private async Task CreateMicroSkillAsync(
        string userId,
        Guid organisationId,
        Guid curriculumId,
        Guid subjectId,
        Guid unitId,
        Guid learningObjectiveId,
        string name,
        int sortOrder)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/curriculum/{curriculumId}/subjects/{subjectId}/units/{unitId}/learning-objectives/{learningObjectiveId}/micro-skills",
            userId,
            organisationId,
            TestJwt.TeacherRole);
        request.Content = JsonContent.Create(new CreateMicroSkillRequest(name, sortOrder));
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private async Task<T> SendAsAsync<T>(HttpMethod method, string url, string userId, Guid tenantId)
    {
        using var request = TestJwt.Authorized(method, url, userId, tenantId, TestJwt.TeacherRole);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<T>();
        return payload ?? throw new InvalidOperationException("Missing response payload.");
    }
}
