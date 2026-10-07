"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { DataText } from "@/components/data-text";
import { fetchProfile, personName, type DirectoryUser, type UserProfile } from "@/lib/auth";
import { learningLabel, loadGapLabels, loadLearningNames, loadPeople } from "@/lib/display-names";
import { roleHome } from "@/lib/role-home";
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
import {
  downloadReportPdf,
  generateSchoolSummaryReport,
  type ReportResponse,
  type SchoolSummaryReportContent,
} from "@/lib/reports";
import {
  fetchOrganisationLongitudinal,
  type OrganisationLongitudinalResponse,
} from "@/lib/longitudinal-analytics";
import {
  fetchOrganisationEffectiveness,
  type OrganisationEffectivenessResponse,
} from "@/lib/effectiveness-analytics";
import { MasteryTrendChart } from "@/components/longitudinal-charts";
import {
  CurriculumEffectivenessTable,
  InterventionEffectivenessTable,
} from "@/components/effectiveness-charts";
import { useI18n } from "@/i18n/I18nProvider";
import { codeLabel } from "@/lib/code-labels";

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

type Translate = ReturnType<typeof useI18n>["t"];

function formatTimeline(item: LeadershipInterventionItem, t: Translate): string {
  const start = item.plannedStartAt ? new Date(item.plannedStartAt).toLocaleDateString() : null;
  const end = item.plannedEndAt ? new Date(item.plannedEndAt).toLocaleDateString() : null;
  if (start && end) {
    return `${start} – ${end}`;
  }
  if (start) {
    return t("leadership.timeline.from", { date: start });
  }
  if (end) {
    return t("leadership.timeline.until", { date: end });
  }
  return t("leadership.timeline.none");
}

export default function LeadershipDashboardPage() {
  const router = useRouter();
  const { t } = useI18n();
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
  const [schoolReport, setSchoolReport] = useState<ReportResponse | null>(null);
  const [orgLongitudinal, setOrgLongitudinal] = useState<OrganisationLongitudinalResponse | null>(null);
  const [orgEffectiveness, setOrgEffectiveness] = useState<OrganisationEffectivenessResponse | null>(null);
  const [reportMessage, setReportMessage] = useState<string | null>(null);
  const [reportBusy, setReportBusy] = useState(false);
  const [people, setPeople] = useState<DirectoryUser[]>([]);
  const [labels, setLabels] = useState<Record<string, string>>({});

  useEffect(() => {
    const token = localStorage.getItem("we_access_token");
    if (!token) {
      router.replace("/login");
      return;
    }

    fetchProfile(token)
      .then(async (loaded) => {
        if (!isSchoolLeader(loaded)) {
          router.replace(roleHome(loaded.roles));
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
          try {
            const longitudinal = await fetchOrganisationLongitudinal(token, firstOrg);
            setOrgLongitudinal(longitudinal);
          } catch {
            setOrgLongitudinal(null);
          }
          try {
            const effectiveness = await fetchOrganisationEffectiveness(token, firstOrg);
            setOrgEffectiveness(effectiveness);
          } catch {
            setOrgEffectiveness(null);
          }
        }
      })
      .catch(() => {
        localStorage.removeItem("we_access_token");
        setError(t("common.sessionExpired"));
      });
  }, [router, t]);

  useEffect(() => {
    const token = localStorage.getItem("we_access_token");
    if (!token || !selectedOrgId) {
      return;
    }

    let cancelled = false;
    Promise.all([loadPeople(token), loadLearningNames(token, selectedOrgId)])
      .then(async ([directory, names]) => {
        const gapLabels = await loadGapLabels(
          token,
          interventions.map((item) => item.studentUserId),
          names
        );
        if (!cancelled) {
          setPeople(directory);
          setLabels({ ...names, ...gapLabels });
        }
      })
      .catch(() => {
        if (!cancelled) {
          setPeople([]);
          setLabels({});
        }
      });

    return () => {
      cancelled = true;
    };
  }, [selectedOrgId, interventions]);

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
    setOrgLongitudinal(null);
    setOrgEffectiveness(null);

    try {
      const data = await fetchLeadershipDashboard(token, organisationId);
      setDashboard(data);
      const monitoring = await fetchLeadershipInterventions(token, organisationId);
      setInterventions(monitoring.interventions);
      try {
        const longitudinal = await fetchOrganisationLongitudinal(token, organisationId);
        setOrgLongitudinal(longitudinal);
      } catch {
        setOrgLongitudinal(null);
      }
      try {
        const effectiveness = await fetchOrganisationEffectiveness(token, organisationId);
        setOrgEffectiveness(effectiveness);
      } catch {
        setOrgEffectiveness(null);
      }
    } catch {
      setError(t("leadership.loadDashboardError"));
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
      setError(t("leadership.loadYearLevelError"));
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
      setError(t("leadership.loadClassSummaryError"));
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
      setError(t("leadership.loadInterventionsError"));
    }
  }

  async function handleGenerateSchoolSummaryReport() {
    const token = localStorage.getItem("we_access_token");
    if (!token || !selectedOrgId) {
      return;
    }

    setReportBusy(true);
    setReportMessage(null);
    try {
      const report = await generateSchoolSummaryReport(token, selectedOrgId);
      setSchoolReport(report);
      setReportMessage(t("leadership.report.generated"));
    } catch {
      setReportMessage(t("leadership.report.generateError"));
    } finally {
      setReportBusy(false);
    }
  }

  async function handleDownloadSchoolReportPdf() {
    const token = localStorage.getItem("we_access_token");
    if (!token || !schoolReport) {
      return;
    }

    setReportBusy(true);
    setReportMessage(null);
    try {
      await downloadReportPdf(
        token,
        schoolReport.id,
        `school-summary-${selectedOrgId}.pdf`
      );
      setReportMessage(t("teacher.class.report.pdfExported"));
    } catch {
      setReportMessage(t("teacher.class.report.pdfError"));
    } finally {
      setReportBusy(false);
    }
  }

  function schoolReportContent(): SchoolSummaryReportContent | null {
    if (!schoolReport || "className" in schoolReport.content) {
      return null;
    }
    return schoolReport.content;
  }

  if (error) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6">
        <div className="space-y-4 text-center">
          <p className="text-red-600">{error}</p>
          <Link href="/login" className="underline">
            {t("common.backToLogin")}
          </Link>
        </div>
      </div>
    );
  }

  if (!profile || !dashboard) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6">
        <p>{t("leadership.loading")}</p>
      </div>
    );
  }

  const classComparisons = yearLevelDashboard?.classComparisons ?? dashboard.classComparisons;

  return (
    <div className="min-h-screen p-8">
      <div className="max-w-4xl mx-auto space-y-6">
        <div className="flex items-center justify-between">
          <h1 className="text-2xl font-semibold">{t("leadership.title")}</h1>
          <Link href="/dashboard" className="text-sm underline">
            {t("leadership.backToHub")}
          </Link>
        </div>

        {organisations.length > 1 ? (
          <div className="rounded-lg border border-black/10 p-4">
            <label className="text-sm font-medium" htmlFor="organisation-select">
              {t("leadership.schoolLabel")}
            </label>
            <select
              id="organisation-select"
              className="mt-2 block w-full rounded border border-black/20 p-2"
              value={selectedOrgId ?? ""}
              onChange={(event) => handleSelectOrganisation(event.target.value)}
            >
              {organisations.map((org) => (
                <option key={org.id} value={org.id} dir="auto">
                  {org.name}
                </option>
              ))}
            </select>
          </div>
        ) : (
          <p className="text-sm text-black/70">{dashboard.organisationName}</p>
        )}

        <div className="grid grid-cols-2 md:grid-cols-3 gap-4">
          <KpiCard label={t("leadership.kpi.students")} value={dashboard.kpis.totalStudents} />
          <KpiCard label={t("leadership.kpi.classes")} value={dashboard.kpis.totalClasses} />
          <KpiCard label={t("leadership.kpi.activeInterventions")} value={dashboard.kpis.activeInterventions} />
          <KpiCard
            label={t("leadership.kpi.activeLearningGaps")}
            value={metricText(dashboard.kpis.activeLearningGaps, t("leadership.kpi.notAvailable"))}
          />
          <KpiCard
            label={t("leadership.kpi.studentsNeedingAttention")}
            value={metricText(dashboard.kpis.studentsNeedingAttention, t("leadership.kpi.notAvailable"))}
            detail={[
              attentionDetail(dashboard.kpis.studentsNeedingAttentionReason, t),
              t("leadership.kpi.attention.criterion"),
            ].filter(Boolean).join(" ")}
          />
          <KpiCard
            label={t("leadership.kpi.assessmentCompletion")}
            value={formatPercent(dashboard.kpis.assessmentCompletionRate)}
          />
        </div>

        <div className="rounded-lg border border-black/10 p-6 space-y-4">
          <div>
            <h2 className="font-medium">{t("leadership.report.title")}</h2>
            <p className="text-sm text-black/60 mt-1">
              {t("leadership.report.subtitle")}
            </p>
          </div>
          <div className="flex gap-3">
            <button
              type="button"
              onClick={handleGenerateSchoolSummaryReport}
              disabled={reportBusy || !selectedOrgId}
              className="rounded bg-black text-white px-4 py-2 text-sm disabled:opacity-50"
            >
              {t("teacher.class.report.generate")}
            </button>
            {schoolReport ? (
              <button
                type="button"
                onClick={handleDownloadSchoolReportPdf}
                disabled={reportBusy}
                className="rounded border border-black/20 px-4 py-2 text-sm disabled:opacity-50"
              >
                {t("teacher.class.report.exportPdf")}
              </button>
            ) : null}
          </div>
          {schoolReportContent() ? (
            <div className="space-y-2 text-sm">
              <p>
                {t("leadership.report.generatedLine", {
                  name: schoolReportContent()!.organisationName,
                  date: new Date(schoolReport!.generatedAt).toLocaleString(),
                })}
              </p>
              <p>
                {t("leadership.report.counts", {
                  students: schoolReportContent()!.kpis.totalStudents,
                  yearLevels: schoolReportContent()!.yearLevels.length,
                  classes: schoolReportContent()!.classComparisons.length,
                })}
              </p>
            </div>
          ) : null}
          {reportMessage ? (
            <p className="text-sm text-black/70">{reportMessage}</p>
          ) : null}
        </div>

        {orgLongitudinal ? (
          <div className="rounded-lg border border-black/10 p-6 space-y-6">
            <div>
              <h2 className="font-medium">{t("leadership.longitudinal.title")}</h2>
              <p className="text-sm text-black/60 mt-1">
                {t("leadership.longitudinal.subtitle")}
              </p>
            </div>
            <MasteryTrendChart
              points={orgLongitudinal.schoolMasteryTrend}
              title={t("leadership.longitudinal.trendTitle")}
            />
            {orgLongitudinal.studentSummaries.length > 0 ? (
              <div className="space-y-2">
                <h3 className="text-sm font-medium">{t("leadership.longitudinal.summariesTitle")}</h3>
                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="text-left text-black/60 border-b border-black/10">
                        <th className="py-2 pr-4">{t("teacher.class.roster.student")}</th>
                        <th className="py-2 pr-4">{t("leadership.longitudinal.cumulativeMicroSkills")}</th>
                        <th className="py-2 pr-4">{t("teacher.workspace.nav.interventions")}</th>
                        <th className="py-2">{t("leadership.longitudinal.gapEvents")}</th>
                      </tr>
                    </thead>
                    <tbody>
                      {orgLongitudinal.studentSummaries.map((summary) => (
                        <tr key={summary.studentUserId} className="border-b border-black/5">
                          <td className="py-2 pr-4">
                            {personName(people, summary.studentUserId, t("organisation.manage.unknownPerson"))}
                          </td>
                          <td className="py-2 pr-4">{summary.cumulativeMicroSkills}</td>
                          <td className="py-2 pr-4">{summary.interventionCount}</td>
                          <td className="py-2">{summary.gapEventCount}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>
            ) : (
              <p className="text-sm text-black/60">{t("leadership.longitudinal.empty")}</p>
            )}
          </div>
        ) : null}

        {orgEffectiveness ? (
          <div className="rounded-lg border border-black/10 p-6 space-y-6">
            <div>
              <h2 className="font-medium">{t("leadership.effectiveness.title")}</h2>
              <p className="text-sm text-black/60 mt-1">
                {t("leadership.effectiveness.subtitle")}
              </p>
            </div>

            <div className="space-y-3">
              <h3 className="text-sm font-medium">{t("leadership.effectiveness.curriculumTitle")}</h3>
              <p className="text-sm text-black/60">
                {t("leadership.effectiveness.curriculumHint")}
              </p>
              <CurriculumEffectivenessTable
                items={orgEffectiveness.curriculumEffectiveness}
              />
            </div>

            <div className="space-y-3">
              <h3 className="text-sm font-medium">{t("leadership.effectiveness.interventionTitle")}</h3>
              <InterventionEffectivenessTable
                items={orgEffectiveness.interventionEffectiveness}
              />
            </div>
          </div>
        ) : null}

        {Object.keys(dashboard.kpis.masteryLevelCounts).length > 0 ? (
          <div className="rounded-lg border border-black/10 p-6 space-y-2">
            <h2 className="font-medium">{t("teacher.class.ei.masteryTitle")}</h2>
            <ul className="space-y-1 text-sm">
              {Object.entries(dashboard.kpis.masteryLevelCounts).map(([level, count]) => (
                <li key={level}>
                  {codeLabel(t, level)}: {count}
                </li>
              ))}
            </ul>
          </div>
        ) : null}

        <div className="rounded-lg border border-black/10 p-6 space-y-3">
          <h2 className="font-medium">{t("leadership.yearLevels.title")}</h2>
          {dashboard.yearLevels.length === 0 ? (
            <p className="text-sm text-black/60">{t("leadership.yearLevels.empty")}</p>
          ) : (
            <ul className="space-y-2">
              {dashboard.yearLevels.map((yearLevel) => (
                <li key={yearLevel.yearLevelId}>
                  <button
                    type="button"
                    className={`text-sm underline ${selectedYearLevelId === yearLevel.yearLevelId ? "font-semibold" : ""}`}
                    onClick={() => handleSelectYearLevel(yearLevel.yearLevelId)}
                  >
                    {t("leadership.yearLevels.line", {
                      name: yearLevel.yearLevelName,
                      classes: yearLevel.classCount,
                      students: yearLevel.studentCount,
                    })}
                  </button>
                </li>
              ))}
            </ul>
          )}
        </div>

        <div className="rounded-lg border border-black/10 p-6 space-y-3">
          <h2 className="font-medium">
            {yearLevelDashboard
              ? t("leadership.classes.yearLevelTitle", { name: yearLevelDashboard.yearLevelName })
              : t("leadership.classes.comparisonsTitle")}
          </h2>
          {classComparisons.length === 0 ? (
            <p className="text-sm text-black/60">{t("leadership.classes.empty")}</p>
          ) : (
            <ul className="space-y-3">
              {classComparisons.map((schoolClass) => (
                <li key={schoolClass.classId} className="text-sm">
                  <button
                    type="button"
                    className={`underline ${selectedClassId === schoolClass.classId ? "font-semibold" : ""}`}
                    onClick={() => handleSelectClass(schoolClass)}
                  >
                    <DataText>{`${schoolClass.className} (${schoolClass.yearLevelName})`}</DataText>
                  </button>
                  <p className="text-black/60 mt-1">
                    {t("leadership.classes.line", {
                      students: schoolClass.studentCount,
                      interventions: schoolClass.activeInterventions,
                      gaps: metricText(schoolClass.activeLearningGaps, t("leadership.kpi.notAvailable")),
                      completion: formatPercent(schoolClass.assessmentCompletionRate),
                    })}
                  </p>
                </li>
              ))}
            </ul>
          )}
        </div>

        <div className="rounded-lg border border-black/10 p-6 space-y-4">
          <div>
            <h2 className="font-medium">{t("leadership.monitoring.title")}</h2>
            <p className="text-sm text-black/60 mt-1">
              {t("leadership.monitoring.subtitle")}
            </p>
          </div>

          <div className="flex flex-wrap gap-3 text-sm">
            <label className="flex flex-col gap-1">
              <span className="text-xs text-black/60">{t("leadership.monitoring.department")}</span>
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
                <option value="">{t("leadership.monitoring.allYearLevels")}</option>
                {dashboard.yearLevels.map((yearLevel) => (
                  <option key={yearLevel.yearLevelId} value={yearLevel.yearLevelId} dir="auto">
                    {yearLevel.yearLevelName}
                  </option>
                ))}
              </select>
            </label>

            <label className="flex flex-col gap-1">
              <span className="text-xs text-black/60">{t("assessments.scope.classLabel")}</span>
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
                <option value="">{t("leadership.monitoring.allClasses")}</option>
                {classComparisons
                  .filter(
                    (schoolClass) =>
                      !interventionFilters.yearLevelId ||
                      schoolClass.yearLevelId === interventionFilters.yearLevelId
                  )
                  .map((schoolClass) => (
                    <option key={schoolClass.classId} value={schoolClass.classId} dir="auto">
                      {`${schoolClass.className} (${schoolClass.yearLevelName})`}
                    </option>
                  ))}
              </select>
            </label>

            <label className="flex flex-col gap-1">
              <span className="text-xs text-black/60">{t("leadership.monitoring.severity")}</span>
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
                <option value="">{t("leadership.monitoring.allSeverities")}</option>
                {severityOptions.map((severity) => (
                  <option key={severity} value={severity}>
                    {codeLabel(t, severity)}
                  </option>
                ))}
              </select>
            </label>

            <label className="flex flex-col gap-1">
              <span className="text-xs text-black/60">{t("charts.curriculum.status")}</span>
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
                <option value="">{t("leadership.monitoring.defaultStatus")}</option>
                {interventionStatusOrder.map((status) => (
                  <option key={status} value={status}>
                    {codeLabel(t, status)}
                  </option>
                ))}
              </select>
            </label>
          </div>

          {interventions.length === 0 ? (
            <p className="text-sm text-black/60">{t("leadership.monitoring.empty")}</p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="text-left text-xs text-black/60 border-b border-black/10">
                    <th className="py-2 pr-4">{t("teacher.class.roster.student")}</th>
                    <th className="py-2 pr-4">{t("leadership.monitoring.col.gap")}</th>
                    <th className="py-2 pr-4">{t("leadership.monitoring.col.teacher")}</th>
                    <th className="py-2 pr-4">{t("assessments.scope.classLabel")}</th>
                    <th className="py-2 pr-4">{t("leadership.monitoring.severity")}</th>
                    <th className="py-2 pr-4">{t("charts.curriculum.status")}</th>
                    <th className="py-2">{t("leadership.monitoring.col.timeline")}</th>
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
                          {personName(people, item.studentUserId, t("organisation.manage.unknownPerson"))}
                        </Link>
                      </td>
                      <td className="py-3 pr-4">
                        {learningLabel(labels, item.learningGapId, t("assessments.review.unknownSkill"))}
                      </td>
                      <td className="py-3 pr-4">
                        {personName(people, item.assignedTeacherUserId, t("organisation.manage.unknownPerson"))}
                      </td>
                      <td className="py-3 pr-4">
                        <DataText>{item.className}</DataText>
                        <span className="text-black/50"> ({item.yearLevelName})</span>
                      </td>
                      <td className="py-3 pr-4">
                        {item.gapSeverity ? codeLabel(t, item.gapSeverity) : "—"}
                      </td>
                      <td className="py-3 pr-4">
                        <span
                          className={`text-xs rounded px-2 py-0.5 ${interventionStatusBadgeClass(item.status)}`}
                        >
                          {codeLabel(t, item.status)}
                        </span>
                      </td>
                      <td className="py-3">{formatTimeline(item, t)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>

        {classSummary ? (
          <div className="rounded-lg border border-black/10 p-6 space-y-3">
            <h2 className="font-medium">
              {t("leadership.classSummary.title", { name: classSummary.class.name })}
            </h2>
            <p className="text-sm">
              {t("leadership.classSummary.activeInterventions", {
                count: classSummary.activeInterventions,
              })}
            </p>
            {classSummary.recentAssessments.length > 0 ? (
              <div>
                <h3 className="text-sm font-medium">{t("teacher.class.assessments.title")}</h3>
                <ul className="list-disc pl-5 text-sm mt-1">
                  {classSummary.recentAssessments.map((assessment) => (
                    <li key={assessment.id}>
                      {t("leadership.classSummary.assessmentLine", {
                        title: assessment.title,
                        count: assessment.submissionCount,
                      })}
                    </li>
                  ))}
                </ul>
              </div>
            ) : null}
            <div>
              <h3 className="text-sm font-medium">{t("leadership.kpi.students")}</h3>
              {classSummary.students.length === 0 ? (
                <p className="text-sm text-black/60">{t("common.noStudentsEnrolled")}</p>
              ) : (
                <ul className="list-disc pl-5 text-sm mt-1">
                  {classSummary.students.map((student) => (
                    <li key={student.userId}>
                      <Link
                        href={`/students/${student.userId}/profile`}
                        className="underline"
                      >
                        {t("dashboard.classes.viewProfile", {
                          studentUserId: personName(people, student.userId, t("organisation.manage.unknownPerson")),
                        })}
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

function metricText(value: number | null, unavailable: string) {
  return value === null ? unavailable : String(value);
}

function attentionDetail(
  reason: string | null | undefined,
  t: (key: string) => string
) {
  if (reason === "high-severity-gap") {
    return t("leadership.kpi.attention.highSeverityGap");
  }
  if (reason === "struggling-diagnostic") {
    return t("leadership.kpi.attention.strugglingDiagnostic");
  }
  return null;
}

function KpiCard({
  label,
  value,
  detail,
}: {
  label: string;
  value: number | string;
  detail?: string | null;
}) {
  return (
    <div className="rounded-lg border border-black/10 p-4">
      <p className="text-xs text-black/60">{label}</p>
      <p className="text-xl font-semibold mt-1">{value}</p>
      {detail ? <p className="text-xs text-black/60 mt-1">{detail}</p> : null}
    </div>
  );
}
