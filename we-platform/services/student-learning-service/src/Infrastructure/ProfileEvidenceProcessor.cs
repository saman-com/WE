using Microsoft.EntityFrameworkCore;
using StudentLearningService.Application;
using StudentLearningService.Domain;
using StudentLearningService.Infrastructure.Data;
using StudentLearningService.Infrastructure.Persistence;
using WePlatform.Events;

namespace StudentLearningService.Infrastructure;

public sealed class ProfileEvidenceProcessor(StudentLearningDbContext db) : IProfileEvidenceProcessor
{
    public async Task ProcessEvidenceCreatedAsync(
        EvidenceCreated evidence,
        CancellationToken cancellationToken = default)
    {
        var profile = await db.Profiles
            .Include(p => p.EvidenceEntries)
            .FirstOrDefaultAsync(p => p.StudentUserId == evidence.StudentUserId, cancellationToken);

        if (profile is null)
        {
            throw new InvalidOperationException(
                $"Student learning profile not found for student {evidence.StudentUserId}.");
        }

        if (profile.EvidenceEntries.Any(e => e.Id == evidence.EvidenceId))
        {
            return;
        }

        var entry = new ProfileEvidenceEntry
        {
            Id = evidence.EvidenceId,
            ProfileId = profile.Id,
            AssessmentId = evidence.AssessmentId,
            Title = string.IsNullOrWhiteSpace(evidence.Title) ? "Evidence" : evidence.Title.Trim(),
            RecordedAt = evidence.ApprovedAt
        };

        foreach (var microSkillId in evidence.MicroSkillMarks.Select(m => m.MicroSkillId).Distinct())
        {
            entry.MicroSkills.Add(new ProfileEvidenceMicroSkill
            {
                EvidenceEntryId = evidence.EvidenceId,
                MicroSkillId = microSkillId
            });
        }

        db.EvidenceEntries.Add(entry);
        profile.UpdatedAt = DateTimeOffset.UtcNow;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (UniqueConstraint.IsViolation(ex))
        {
            db.ChangeTracker.Clear();
        }
    }
}
