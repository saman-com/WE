using WePlatform.Tenancy;
using Xunit;

namespace EvidenceApprovalFlow.Tests;

[Collection(FlowCollection.Name)]
public sealed class EvidenceApprovalHappyPathTests(FlowFixture fixture)
{
    // Demo micro-skills + marks 5/4/3/2 from seed-demo.sh
    private static readonly Guid SkillRead = Guid.Parse("00000000-0000-4000-8000-000000001001");
    private static readonly Guid SkillSubstitute = Guid.Parse("00000000-0000-4000-8000-000000001002");
    private static readonly Guid SkillInverse = Guid.Parse("00000000-0000-4000-8000-000000001003");
    private static readonly Guid SkillIsolate = Guid.Parse("00000000-0000-4000-8000-000000001004");

    [Fact]
    public async Task ApproveEvidence_UpdatesProfileDiagnosticsGapsAndMastery()
    {
        var organisationId = DefaultTenant.Id;
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var classId = Guid.CreateVersion7();
        var assessmentId = Guid.CreateVersion7();
        var submissionId = Guid.CreateVersion7();

        await fixture.SeedStudentProfileAsync(organisationId, studentId);

        var evidenceId = await fixture.ApproveEvidenceAsync(
            organisationId,
            teacherId,
            studentId,
            classId,
            assessmentId,
            submissionId,
            "Algebra sheet",
            [
                (SkillRead, 5m, "Read an equation"),
                (SkillSubstitute, 4m, "Substitute a value"),
                (SkillInverse, 3m, "Use inverse operations"),
                (SkillIsolate, 2m, "Isolate the unknown")
            ]);

        await fixture.WaitForAsync(async () =>
            await fixture.ScalarAsync("we_learning_it",
                $"select count(*) from profile_evidence_entries where id = '{evidenceId}'") == 1
            && await fixture.ScalarAsync("we_diagnostic_it",
                $"select count(*) from micro_skill_diagnostics where evidence_id = '{evidenceId}'") == 4
            && await fixture.ScalarAsync("we_gaps_it",
                $"select count(*) from learning_gaps where evidence_id = '{evidenceId}'") == 2
            && await fixture.ScalarAsync("we_mastery_it",
                $"select count(*) from mastery_records where student_user_id = '{studentId}'") == 4);

        Assert.Equal(1, await fixture.ScalarAsync("we_learning_it",
            $"select count(*) from profile_evidence_entries where id = '{evidenceId}' and title = 'Algebra sheet'"));

        // 5 and 4 => Mastered diagnostic; 3 and 2 => Developing => gaps
        Assert.Equal(2, await fixture.ScalarAsync("we_diagnostic_it",
            $"select count(*) from micro_skill_diagnostics where evidence_id = '{evidenceId}' and status = 'Mastered'"));
        Assert.Equal(2, await fixture.ScalarAsync("we_diagnostic_it",
            $"select count(*) from micro_skill_diagnostics where evidence_id = '{evidenceId}' and status = 'Developing'"));

        Assert.Equal(1, await fixture.ScalarAsync("we_gaps_it",
            $"select count(*) from learning_gaps where evidence_id = '{evidenceId}' and micro_skill_id = '{SkillInverse}' and severity = 'Low'"));
        Assert.Equal(1, await fixture.ScalarAsync("we_gaps_it",
            $"select count(*) from learning_gaps where evidence_id = '{evidenceId}' and micro_skill_id = '{SkillIsolate}' and severity = 'Medium'"));

        // Single-evidence mastery (ProficientMinAverage=3.0, Mastered needs count>=2):
        // marks 5/4/3 => Proficient; mark 2 => Developing.
        Assert.Equal(1, await fixture.ScalarAsync("we_mastery_it",
            $"select count(*) from mastery_records where student_user_id = '{studentId}' and micro_skill_id = '{SkillRead}' and mastery_level = 'Proficient'"));
        Assert.Equal(1, await fixture.ScalarAsync("we_mastery_it",
            $"select count(*) from mastery_records where student_user_id = '{studentId}' and micro_skill_id = '{SkillSubstitute}' and mastery_level = 'Proficient'"));
        Assert.Equal(1, await fixture.ScalarAsync("we_mastery_it",
            $"select count(*) from mastery_records where student_user_id = '{studentId}' and micro_skill_id = '{SkillInverse}' and mastery_level = 'Proficient'"));
        Assert.Equal(1, await fixture.ScalarAsync("we_mastery_it",
            $"select count(*) from mastery_records where student_user_id = '{studentId}' and micro_skill_id = '{SkillIsolate}' and mastery_level = 'Developing'"));
    }
}
