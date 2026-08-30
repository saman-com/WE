"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useParams, useRouter, useSearchParams } from "next/navigation";
import { fetchProfile, type UserProfile } from "@/lib/auth";
import {
  fetchClassEiInsights,
  type ClassEiInsights,
} from "@/lib/ei-insights";
import {
  fetchClassDashboard,
  type ClassDashboard,
} from "@/lib/teacher-workspace";

function isTeacher(profile: UserProfile): boolean {
  return profile.roles.includes("Teacher");
}

function formatDate(value: string | null): string {
  if (!value) {
    return "—";
  }
  return new Date(value).toLocaleDateString();
}

export default function TeacherClassDetailPage() {
  const router = useRouter();
  const params = useParams<{ classId: string }>();
  const searchParams = useSearchParams();
  const organisationId = searchParams.get("organisationId");
  const classId = params.classId;

  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [dashboard, setDashboard] = useState<ClassDashboard | null>(null);
  const [insights, setInsights] = useState<ClassEiInsights | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const token = localStorage.getItem("we_access_token");
    if (!token) {
      router.replace("/login");
      return;
    }

    if (!organisationId) {
      setError("Missing organisation context for this class.");
      return;
    }

    fetchProfile(token)
      .then(async (loaded) => {
        if (!isTeacher(loaded)) {
          router.replace("/dashboard");
          return;
        }
        setProfile(loaded);
        const loadedDashboard = await fetchClassDashboard(
          token,
          organisationId,
          classId
        );
        setDashboard(loadedDashboard);

        try {
          const loadedInsights = await fetchClassEiInsights(
            token,
            organisationId,
            classId
          );
          setInsights(loadedInsights);
        } catch {
          setInsights({
            organisationId,
            classId,
            masteryDistribution: [],
            activeLearningGaps: [],
            recentDiagnosticTrends: [],
            studentsNeedingAttention: [],
          });
        }
      })
      .catch(() => {
        setError("Unable to load class dashboard.");
      });
  }, [router, organisationId, classId]);

  if (error) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6">
        <div className="space-y-4 text-center">
          <p className="text-red-600">{error}</p>
          <Link href="/teacher" className="underline">
            Back to teacher workspace
          </Link>
        </div>
      </div>
    );
  }

  if (!profile || !dashboard) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6">
        <p>Loading class dashboard...</p>
      </div>
    );
  }

  return (
    <div className="min-h-screen p-8">
      <div className="max-w-4xl mx-auto space-y-6">
        <div className="space-y-2">
          <Link href="/teacher" className="text-sm underline">
            Back to my classes
          </Link>
          <h1 className="text-2xl font-semibold">
            {dashboard.class.name} ({dashboard.class.code})
          </h1>
        </div>

        <div className="rounded-lg border border-black/10 p-6 space-y-3">
          <h2 className="font-medium">Class roster</h2>
          {dashboard.roster.length === 0 ? (
            <p className="text-sm text-black/60">No students enrolled.</p>
          ) : (
            <table className="w-full text-sm">
              <thead>
                <tr className="text-left border-b border-black/10">
                  <th className="py-2">Student</th>
                  <th className="py-2">Evidence</th>
                  <th className="py-2">Latest activity</th>
                  <th className="py-2">SLP</th>
                </tr>
              </thead>
              <tbody>
                {dashboard.roster.map((student) => (
                  <tr key={student.studentUserId} className="border-b border-black/5">
                    <td className="py-2">{student.studentUserId}</td>
                    <td className="py-2">{student.evidenceCount}</td>
                    <td className="py-2">{formatDate(student.latestActivityAt)}</td>
                    <td className="py-2">
                      <Link
                        href={`/students/${student.studentUserId}/profile`}
                        className="underline"
                      >
                        View SLP summary
                      </Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>

        {insights ? (
          <div className="rounded-lg border border-black/10 p-6 space-y-4">
            <div>
              <h2 className="font-medium">Educational Intelligence</h2>
              <p className="text-sm text-black/60 mt-1">
                Class-level mastery, gaps, and diagnostic trends with linked evidence.
              </p>
            </div>

            <div className="space-y-2">
              <h3 className="text-sm font-medium">Mastery distribution</h3>
              {insights.masteryDistribution.length === 0 ? (
                <p className="text-sm text-black/60">
                  No mastery data yet. Insights appear after approved evidence is analysed.
                </p>
              ) : (
                <ul className="space-y-3">
                  {insights.masteryDistribution.map((item) => (
                    <li key={item.microSkillId} className="rounded border border-black/5 p-3 space-y-1">
                      <p className="text-sm font-medium">Micro-skill: {item.microSkillId}</p>
                      <p className="text-xs text-black/60">
                        Mastered {item.levelCounts.Mastered ?? 0} · Proficient{" "}
                        {item.levelCounts.Proficient ?? 0} · Developing{" "}
                        {item.levelCounts.Developing ?? 0} · Not started{" "}
                        {item.levelCounts.NotStarted ?? 0} · {item.totalStudents} students
                      </p>
                      <p className="text-sm">{item.explanation}</p>
                      {item.linkedEvidenceIds.length > 0 ? (
                        <p className="text-xs text-black/60">
                          Linked evidence: {item.linkedEvidenceIds.join(", ")}
                        </p>
                      ) : null}
                    </li>
                  ))}
                </ul>
              )}
            </div>

            <div className="space-y-2">
              <h3 className="text-sm font-medium">Active learning gaps</h3>
              {insights.activeLearningGaps.length === 0 ? (
                <p className="text-sm text-black/60">No active learning gaps identified.</p>
              ) : (
                <ul className="space-y-3">
                  {insights.activeLearningGaps.map((gap) => (
                    <li key={gap.gapId} className="rounded border border-black/5 p-3 space-y-1">
                      <p className="text-sm font-medium">
                        {gap.severity} severity · {gap.urgency} urgency ·{" "}
                        <Link href={`/students/${gap.studentUserId}/profile`} className="underline">
                          {gap.studentUserId}
                        </Link>
                      </p>
                      <p className="text-xs text-black/60">Micro-skill: {gap.microSkillId}</p>
                      <p className="text-sm">{gap.explanation}</p>
                      <p className="text-xs text-black/60">Evidence: {gap.evidenceId}</p>
                    </li>
                  ))}
                </ul>
              )}
            </div>

            <div className="space-y-2">
              <h3 className="text-sm font-medium">Recent diagnostic trends</h3>
              {insights.recentDiagnosticTrends.length === 0 ? (
                <p className="text-sm text-black/60">No recent diagnostic trends.</p>
              ) : (
                <ul className="space-y-3">
                  {insights.recentDiagnosticTrends.map((trend) => (
                    <li
                      key={`${trend.microSkillId}-${trend.status}`}
                      className="rounded border border-black/5 p-3 space-y-1"
                    >
                      <p className="text-sm font-medium">
                        {trend.status} · {trend.occurrenceCount} occurrence
                        {trend.occurrenceCount === 1 ? "" : "s"}
                      </p>
                      <p className="text-xs text-black/60">
                        Micro-skill: {trend.microSkillId} · Latest:{" "}
                        {new Date(trend.latestAt).toLocaleDateString()}
                      </p>
                      <p className="text-sm">{trend.explanation}</p>
                      <p className="text-xs text-black/60">Evidence: {trend.evidenceId}</p>
                    </li>
                  ))}
                </ul>
              )}
            </div>

            <div className="space-y-2">
              <h3 className="text-sm font-medium">Students needing attention</h3>
              {insights.studentsNeedingAttention.length === 0 ? (
                <p className="text-sm text-black/60">No students flagged for attention.</p>
              ) : (
                <ul className="space-y-3">
                  {insights.studentsNeedingAttention.map((student) => (
                    <li
                      key={student.studentUserId}
                      className="rounded border border-black/5 p-3 space-y-1"
                    >
                      <p className="text-sm font-medium">
                        <Link href={`/students/${student.studentUserId}/profile`} className="underline">
                          {student.studentUserId}
                        </Link>{" "}
                        — {student.reason}
                      </p>
                      <p className="text-sm">{student.explanation}</p>
                      {student.evidenceId ? (
                        <p className="text-xs text-black/60">Evidence: {student.evidenceId}</p>
                      ) : null}
                    </li>
                  ))}
                </ul>
              )}
            </div>
          </div>
        ) : null}

        <div className="rounded-lg border border-black/10 p-6 space-y-3">
          <h2 className="font-medium">Recent assessments</h2>
          {dashboard.recentAssessments.length === 0 ? (
            <p className="text-sm text-black/60">No assessments for this class yet.</p>
          ) : (
            <ul className="space-y-3">
              {dashboard.recentAssessments.map((assessment) => (
                <li key={assessment.id} className="border border-black/10 rounded p-4">
                  <div className="flex items-start justify-between gap-4">
                    <div>
                      <p className="font-medium">{assessment.title}</p>
                      <p className="text-sm text-black/60">
                        Status: {assessment.status} · Due: {formatDate(assessment.dueAt)}
                      </p>
                      <p className="text-sm text-black/60">
                        {assessment.submissionCount} submitted ·{" "}
                        {assessment.reviewedCount} reviewed ·{" "}
                        {Math.max(assessment.submissionCount - assessment.reviewedCount, 0)}{" "}
                        pending review
                      </p>
                    </div>
                    <Link
                      href={`/assessments/${assessment.id}/review`}
                      className="text-sm underline shrink-0"
                    >
                      Review submissions
                    </Link>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>
    </div>
  );
}
