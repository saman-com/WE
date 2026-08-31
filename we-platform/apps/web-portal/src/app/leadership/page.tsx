"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { fetchProfile, type UserProfile } from "@/lib/auth";
import {
  fetchClassLeadershipSummary,
  fetchLeadershipDashboard,
  fetchLeadershipInterventions,
  fetchYearLevelLeadershipDashboard,
  type ClassComparisonSummary,
  type LeadershipDashboard,
  type LeadershipInterventionFilters,
  type LeadershipInterventionItem,
  type YearLevelLeadershipDashboard,
} from "@/lib/leadership-dashboard";
import { listOrganisations, type Organisation } from "@/lib/organisation";

function isSchoolLeader(profile: UserProfile): boolean {
  return profile.roles.includes("SchoolLeader");
}

function formatPercent(rate: number): string {
  return `${Math.round(rate * 100)}%`;
}

const interventionStatusOrder = ["Planned", "Active", "Completed", "Closed"] as const;
const severityOptions = ["High", "Medium", "Low"] as const;

function interventionStatusBadgeClass(status: string): string {
  switch (status) {
    case "Planned":
      return "bg-blue-100 text-blue-800";
    case "Active":
      return "bg-amber-100 text-amber-800";
    case "Completed":
      return "bg-green-100 text-green-800";
    case "Closed":
      return "bg-black/10 text-black/70";
    default:
      return "bg-black/10 text-black/70";
  }
}

function formatTimeline(item: LeadershipInterventionItem): string {
  const start = item.plannedStartAt ? new Date(item.plannedStartAt).toLocaleDateString() : null;
  const end = item.plannedEndAt ? new Date(item.plannedEndAt).toLocaleDateString() : null;
  if (start && end) {
    return `${start} – ${end}`;
  }
  if (start) {
    return `From ${start}`;
  }
  if (end) {
    return `Until ${end}`;
  }
  return "No timeline set";
}

export default function LeadershipDashboardPage() {
  const router = useRouter();
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [organisations, setOrganisations] = useState<Organisation[]>([]);
  const [selectedOrgId, setSelectedOrgId] = useState<string | null>(null);
  const [dashboard, setDashboard] = useState<LeadershipDashboard | null>(null);
  const [yearLevelDashboard, setYearLevelDashboard] =
    useState<YearLevelLeadershipDashboard | null>(null);
  const [selectedYearLevelId, setSelectedYearLevelId] = useState<string | null>(null);
  const [selectedClassId, setSelectedClassId] = useState<string | null>(null);
  const [classSummary, setClassSummary] = useState<Awaited<
    ReturnType<typeof fetchClassLeadershipSummary>
  > | null>(null);
  const [interventions, setInterventions] = useState<LeadershipInterventionItem[]>([]);
  const [interventionFilters, setInterventionFilters] = useState<LeadershipInterventionFilters>({});
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const token = localStorage.getItem("we_access_token");
    if (!token) {
      router.replace("/login");
      return;
    }

    fetchProfile(token)
      .then(async (loaded) => {
        if (!isSchoolLeader(loaded) && !loaded.roles.includes("SystemAdministrator")) {
          router.replace("/dashboard");
          return;
        }
        setProfile(loaded);
        const orgs = await listOrganisations(token);
        setOrganisations(orgs);
        if (orgs.length > 0) {
          const firstOrg = orgs[0].id;
          setSelectedOrgId(firstOrg);
          const data = await fetchLeadershipDashboard(token, firstOrg);
          setDashboard(data);
          const monitoring = await fetchLeadershipInterventions(token, firstOrg);
          setInterventions(monitoring.interventions);
        }
      })
      .catch(() => {
        localStorage.removeItem("we_access_token");
        setError("Session expired. Please sign in again.");
      });
  }, [router]);

  async function handleSelectOrganisation(organisationId: string) {
    const token = localStorage.getItem("we_access_token");
    if (!token) {
      return;
    }

    setSelectedOrgId(organisationId);
    setSelectedYearLevelId(null);
    setSelectedClassId(null);
    setYearLevelDashboard(null);
    setClassSummary(null);
    setDashboard(null);
    setInterventions([]);
    setInterventionFilters({});

    try {
      const data = await fetchLeadershipDashboard(token, organisationId);
      setDashboard(data);
      const monitoring = await fetchLeadershipInterventions(token, organisationId);
      setInterventions(monitoring.interventions);
    } catch {
      setError("Unable to load leadership dashboard.");
    }
  }

  async function handleSelectYearLevel(yearLevelId: string) {
    const token = localStorage.getItem("we_access_token");
    if (!token || !selectedOrgId) {
      return;
    }

    setSelectedYearLevelId(yearLevelId);
    setSelectedClassId(null);
    setClassSummary(null);
    setYearLevelDashboard(null);

    try {
      const data = await fetchYearLevelLeadershipDashboard(token, selectedOrgId, yearLevelId);
      setYearLevelDashboard(data);
    } catch {
      setError("Unable to load year level dashboard.");
    }
  }

  async function handleSelectClass(classComparison: ClassComparisonSummary) {
    const token = localStorage.getItem("we_access_token");
    if (!token || !selectedOrgId) {
      return;
    }

    setSelectedClassId(classComparison.classId);
    setClassSummary(null);

    try {
      const summary = await fetchClassLeadershipSummary(
        token,
        selectedOrgId,
        classComparison.classId
      );
      setClassSummary(summary);
    } catch {
      setError("Unable to load class summary.");
    }
  }

  async function handleInterventionFilterChange(filters: LeadershipInterventionFilters) {
    const token = localStorage.getItem("we_access_token");
    if (!token || !selectedOrgId) {
      return;
    }

    setInterventionFilters(filters);

    try {
      const monitoring = await fetchLeadershipInterventions(token, selectedOrgId, filters);
      setInterventions(monitoring.interventions);
    } catch {
      setError("Unable to load intervention monitoring data.");
    }
  }

  if (error) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6">
        <div className="space-y-4 text-center">
          <p className="text-red-600">{error}</p>
          <Link href="/login" className="underline">
            Back to login
          </Link>
        </div>
      </div>
    );
  }

  if (!profile || !dashboard) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6">
        <p>Loading leadership dashboard...</p>
      </div>
    );
  }

  const classComparisons = yearLevelDashboard?.classComparisons ?? dashboard.classComparisons;

  return (
    <div className="min-h-screen p-8">
      <div className="max-w-4xl mx-auto space-y-6">
        <div className="flex items-center justify-between">
          <h1 className="text-2xl font-semibold">School Leadership Dashboard</h1>
          <Link href="/dashboard" className="text-sm underline">
            Back to hub
          </Link>
        </div>

        {organisations.length > 1 ? (
          <div className="rounded-lg border border-black/10 p-4">
            <label className="text-sm font-medium" htmlFor="organisation-select">
              School
            </label>
            <select
              id="organisation-select"
              className="mt-2 block w-full rounded border border-black/20 p-2"
              value={selectedOrgId ?? ""}
              onChange={(event) => handleSelectOrganisation(event.target.value)}
            >
              {organisations.map((org) => (
                <option key={org.id} value={org.id}>
                  {org.name}
                </option>
              ))}
            </select>
          </div>
        ) : (
          <p className="text-sm text-black/70">{dashboard.organisationName}</p>
        )}

        <div className="grid grid-cols-2 md:grid-cols-3 gap-4">
          <KpiCard label="Students" value={dashboard.kpis.totalStudents} />
          <KpiCard label="Classes" value={dashboard.kpis.totalClasses} />
          <KpiCard label="Active interventions" value={dashboard.kpis.activeInterventions} />
          <KpiCard label="Active learning gaps" value={dashboard.kpis.activeLearningGaps} />
          <KpiCard
            label="Students needing attention"
            value={dashboard.kpis.studentsNeedingAttention}
          />
          <KpiCard
            label="Assessment completion"
            value={formatPercent(dashboard.kpis.assessmentCompletionRate)}
          />
        </div>

        {Object.keys(dashboard.kpis.masteryLevelCounts).length > 0 ? (
          <div className="rounded-lg border border-black/10 p-6 space-y-2">
            <h2 className="font-medium">Mastery distribution</h2>
            <ul className="space-y-1 text-sm">
              {Object.entries(dashboard.kpis.masteryLevelCounts).map(([level, count]) => (
                <li key={level}>
                  {level}: {count}
                </li>
              ))}
            </ul>
          </div>
        ) : null}

        <div className="rounded-lg border border-black/10 p-6 space-y-3">
          <h2 className="font-medium">Drill-down: year levels</h2>
          {dashboard.yearLevels.length === 0 ? (
            <p className="text-sm text-black/60">No year levels configured.</p>
          ) : (
            <ul className="space-y-2">
              {dashboard.yearLevels.map((yearLevel) => (
                <li key={yearLevel.yearLevelId}>
                  <button
                    type="button"
                    className={`text-sm underline ${selectedYearLevelId === yearLevel.yearLevelId ? "font-semibold" : ""}`}
                    onClick={() => handleSelectYearLevel(yearLevel.yearLevelId)}
                  >
                    {yearLevel.yearLevelName} — {yearLevel.classCount} classes,{" "}
                    {yearLevel.studentCount} students
                  </button>
                </li>
              ))}
            </ul>
          )}
        </div>

        <div className="rounded-lg border border-black/10 p-6 space-y-3">
          <h2 className="font-medium">
            {yearLevelDashboard ? `${yearLevelDashboard.yearLevelName} classes` : "Class comparisons"}
          </h2>
          {classComparisons.length === 0 ? (
            <p className="text-sm text-black/60">No classes in scope.</p>
          ) : (
            <ul className="space-y-3">
              {classComparisons.map((schoolClass) => (
                <li key={schoolClass.classId} className="text-sm">
                  <button
                    type="button"
                    className={`underline ${selectedClassId === schoolClass.classId ? "font-semibold" : ""}`}
                    onClick={() => handleSelectClass(schoolClass)}
                  >
                    {schoolClass.className} ({schoolClass.yearLevelName})
                  </button>
                  <p className="text-black/60 mt-1">
                    {schoolClass.studentCount} students · {schoolClass.activeInterventions}{" "}
                    interventions · {schoolClass.activeLearningGaps} gaps ·{" "}
                    {formatPercent(schoolClass.assessmentCompletionRate)} assessments complete
                  </p>
                </li>
              ))}
            </ul>
          )}
        </div>

        <div className="rounded-lg border border-black/10 p-6 space-y-4">
          <div>
            <h2 className="font-medium">Intervention monitoring</h2>
            <p className="text-sm text-black/60 mt-1">
              School-wide oversight of active interventions. Read-only view for leadership.
            </p>
          </div>

          <div className="flex flex-wrap gap-3 text-sm">
            <label className="flex flex-col gap-1">
              <span className="text-xs text-black/60">Department (year level)</span>
              <select
                className="rounded border border-black/20 p-2"
                value={interventionFilters.yearLevelId ?? ""}
                onChange={(event) =>
                  handleInterventionFilterChange({
                    ...interventionFilters,
                    yearLevelId: event.target.value || undefined,
                    classId: undefined,
                  })
                }
              >
                <option value="">All year levels</option>
                {dashboard.yearLevels.map((yearLevel) => (
                  <option key={yearLevel.yearLevelId} value={yearLevel.yearLevelId}>
                    {yearLevel.yearLevelName}
                  </option>
                ))}
              </select>
            </label>

            <label className="flex flex-col gap-1">
              <span className="text-xs text-black/60">Class</span>
              <select
                className="rounded border border-black/20 p-2"
                value={interventionFilters.classId ?? ""}
                onChange={(event) =>
                  handleInterventionFilterChange({
                    ...interventionFilters,
                    classId: event.target.value || undefined,
                  })
                }
              >
                <option value="">All classes</option>
                {classComparisons
                  .filter(
                    (schoolClass) =>
                      !interventionFilters.yearLevelId ||
                      schoolClass.yearLevelId === interventionFilters.yearLevelId
                  )
                  .map((schoolClass) => (
                    <option key={schoolClass.classId} value={schoolClass.classId}>
                      {schoolClass.className} ({schoolClass.yearLevelName})
                    </option>
                  ))}
              </select>
            </label>

            <label className="flex flex-col gap-1">
              <span className="text-xs text-black/60">Severity</span>
              <select
                className="rounded border border-black/20 p-2"
                value={interventionFilters.severity ?? ""}
                onChange={(event) =>
                  handleInterventionFilterChange({
                    ...interventionFilters,
                    severity: event.target.value || undefined,
                  })
                }
              >
                <option value="">All severities</option>
                {severityOptions.map((severity) => (
                  <option key={severity} value={severity}>
                    {severity}
                  </option>
                ))}
              </select>
            </label>

            <label className="flex flex-col gap-1">
              <span className="text-xs text-black/60">Status</span>
              <select
                className="rounded border border-black/20 p-2"
                value={interventionFilters.status ?? ""}
                onChange={(event) =>
                  handleInterventionFilterChange({
                    ...interventionFilters,
                    status: event.target.value || undefined,
                  })
                }
              >
                <option value="">Active (Planned + Active)</option>
                {interventionStatusOrder.map((status) => (
                  <option key={status} value={status}>
                    {status}
                  </option>
                ))}
              </select>
            </label>
          </div>

          {interventions.length === 0 ? (
            <p className="text-sm text-black/60">No interventions match the current filters.</p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="text-left text-xs text-black/60 border-b border-black/10">
                    <th className="py-2 pr-4">Student</th>
                    <th className="py-2 pr-4">Gap</th>
                    <th className="py-2 pr-4">Teacher</th>
                    <th className="py-2 pr-4">Class</th>
                    <th className="py-2 pr-4">Severity</th>
                    <th className="py-2 pr-4">Status</th>
                    <th className="py-2">Timeline</th>
                  </tr>
                </thead>
                <tbody>
                  {interventions.map((item) => (
                    <tr key={item.interventionId} className="border-b border-black/5">
                      <td className="py-3 pr-4">
                        <Link
                          href={`/students/${item.studentUserId}/profile`}
                          className="underline"
                        >
                          {item.studentUserId}
                        </Link>
                      </td>
                      <td className="py-3 pr-4 font-mono text-xs">{item.learningGapId}</td>
                      <td className="py-3 pr-4">{item.assignedTeacherUserId}</td>
                      <td className="py-3 pr-4">
                        {item.className}
                        <span className="text-black/50"> ({item.yearLevelName})</span>
                      </td>
                      <td className="py-3 pr-4">{item.gapSeverity ?? "—"}</td>
                      <td className="py-3 pr-4">
                        <span
                          className={`text-xs rounded px-2 py-0.5 ${interventionStatusBadgeClass(item.status)}`}
                        >
                          {item.status}
                        </span>
                      </td>
                      <td className="py-3">{formatTimeline(item)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>

        {classSummary ? (
          <div className="rounded-lg border border-black/10 p-6 space-y-3">
            <h2 className="font-medium">Class: {classSummary.class.name}</h2>
            <p className="text-sm">
              Active interventions: {classSummary.activeInterventions}
            </p>
            {classSummary.recentAssessments.length > 0 ? (
              <div>
                <h3 className="text-sm font-medium">Recent assessments</h3>
                <ul className="list-disc pl-5 text-sm mt-1">
                  {classSummary.recentAssessments.map((assessment) => (
                    <li key={assessment.id}>
                      {assessment.title} — {assessment.submissionCount} submissions
                    </li>
                  ))}
                </ul>
              </div>
            ) : null}
            <div>
              <h3 className="text-sm font-medium">Students</h3>
              {classSummary.students.length === 0 ? (
                <p className="text-sm text-black/60">No students enrolled.</p>
              ) : (
                <ul className="list-disc pl-5 text-sm mt-1">
                  {classSummary.students.map((student) => (
                    <li key={student.userId}>
                      <Link
                        href={`/students/${student.userId}/profile`}
                        className="underline"
                      >
                        View profile — {student.userId}
                      </Link>
                    </li>
                  ))}
                </ul>
              )}
            </div>
          </div>
        ) : null}
      </div>
    </div>
  );
}

function KpiCard({ label, value }: { label: string; value: number | string }) {
  return (
    <div className="rounded-lg border border-black/10 p-4">
      <p className="text-xs text-black/60">{label}</p>
      <p className="text-xl font-semibold mt-1">{value}</p>
    </div>
  );
}
