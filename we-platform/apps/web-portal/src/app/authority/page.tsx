"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { fetchProfile, type UserProfile } from "@/lib/auth";
import { roleHome } from "@/lib/role-home";
import {
  NATIONAL_MINIMUM_GROUP_SIZE,
  chartPointsForCountCells,
  fetchCurriculumEffectiveness,
  fetchEquityAnalysis,
  fetchInterventionImpact,
  fetchPolicyTrends,
  formatCountCell,
  formatPercent,
  type CurriculumEffectivenessComparisonResponse,
  type EquityAnalysisResponse,
  type InterventionImpactResponse,
  type PolicyTrendsResponse,
} from "@/lib/policy-dashboards";
import { useI18n } from "@/i18n/I18nProvider";

function isEducationAuthorityOfficer(profile: UserProfile): boolean {
  return profile.roles.includes("EducationAuthorityOfficer");
}

export default function AuthorityPolicyDashboardPage() {
  const router = useRouter();
  const { t } = useI18n();
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [trends, setTrends] = useState<PolicyTrendsResponse | null>(null);
  const [equity, setEquity] = useState<EquityAnalysisResponse | null>(null);
  const [curriculum, setCurriculum] =
    useState<CurriculumEffectivenessComparisonResponse | null>(null);
  const [interventions, setInterventions] = useState<InterventionImpactResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  const suppressedLabel = t("authority.count.suppressed", {
    n: NATIONAL_MINIMUM_GROUP_SIZE,
  });

  useEffect(() => {
    const token = localStorage.getItem("we_access_token");
    if (!token) {
      router.replace("/login");
      return;
    }

    fetchProfile(token)
      .then(async (loaded) => {
        if (!isEducationAuthorityOfficer(loaded)) {
          router.replace(roleHome(loaded.roles));
          return;
        }

        setProfile(loaded);
        const [trendsData, equityData, curriculumData, interventionData] = await Promise.all([
          fetchPolicyTrends(token),
          fetchEquityAnalysis(token),
          fetchCurriculumEffectiveness(token),
          fetchInterventionImpact(token),
        ]);
        setTrends(trendsData);
        setEquity(equityData);
        setCurriculum(curriculumData);
        setInterventions(interventionData);
      })
      .catch(() => {
        setError(t("authority.loadError"));
      })
      .finally(() => {
        setLoading(false);
      });
  }, [router, t]);

  if (loading) {
    return (
      <main className="min-h-screen p-8">
        <p>{t("authority.loading")}</p>
      </main>
    );
  }

  if (!profile) {
    return null;
  }

  const studentChartPoints = trends
    ? chartPointsForCountCells(trends.regions, (region) => region.studentCount)
    : [];

  return (
    <main className="min-h-screen p-8 space-y-8">
      <div className="flex items-center justify-between gap-4">
        <h1 className="text-2xl font-semibold">{t("authority.title")}</h1>
        <Link href="/dashboard" className="text-sm underline">
          {t("common.dashboard")}
        </Link>
      </div>

      <p className="text-sm text-black/70">{t("authority.subtitle")}</p>

      {error ? <p className="text-sm text-red-700">{error}</p> : null}

      {trends ? (
        <section className="space-y-3">
          <h2 className="text-xl font-medium">{t("authority.trends.title")}</h2>
          <p className="text-sm text-black/60">
            {t("authority.asOf", { date: trends.asOfDate })}
          </p>
          <div className="grid gap-3 sm:grid-cols-3">
            <p className="text-sm">
              {t("authority.trends.totalSchools")}: {trends.totalSchools}
            </p>
            <p className="text-sm" data-testid="authority-total-students">
              {t("authority.trends.totalStudents")}:{" "}
              {formatCountCell(trends.totalStudents, suppressedLabel)}
            </p>
            <p className="text-sm">
              {t("authority.trends.nationalMastery")}:{" "}
              {formatPercent(trends.nationalAverageMasteryPercent)}
            </p>
          </div>
          {trends.regions.length === 0 ? (
            <p className="text-sm text-black/60">{t("authority.empty")}</p>
          ) : (
            <ul className="space-y-2">
              {trends.regions.map((region) => (
                <li
                  key={region.regionCode}
                  className="text-sm"
                  data-testid={`authority-region-${region.regionCode}`}
                >
                  {region.regionCode}: {region.schoolCount}{" "}
                  {t("authority.trends.schools")},{" "}
                  {formatCountCell(region.studentCount, suppressedLabel)}{" "}
                  {t("authority.trends.students")},{" "}
                  {formatPercent(region.averageMasteryPercent)}{" "}
                  {t("authority.trends.mastery")}
                </li>
              ))}
            </ul>
          )}
          {/* Chart series omit suppressed cells (never plot 0 for hidden counts). */}
          <ul className="sr-only" data-testid="authority-student-chart-points">
            {studentChartPoints.map((point) => (
              <li key={point.regionCode}>
                {point.regionCode}:{point.value}
              </li>
            ))}
          </ul>
        </section>
      ) : null}

      {equity ? (
        <section className="space-y-3">
          <h2 className="text-xl font-medium">{t("authority.equity.title")}</h2>
          <p className="text-sm text-black/60">{t("authority.equity.subtitle")}</p>
          {equity.distributions.length === 0 ? (
            <p className="text-sm text-black/60">{t("authority.empty")}</p>
          ) : (
            <ul className="space-y-2">
              {equity.distributions.map((item) => (
                <li
                  key={`${item.regionCode}-${item.demographicDimension}-${item.demographicCategory}`}
                  className="text-sm"
                >
                  {item.regionCode} · {item.demographicDimension}/{item.demographicCategory}:{" "}
                  {formatPercent(item.averageMasteryPercent)} (
                  {formatCountCell(item.sampleSize, suppressedLabel)}{" "}
                  {t("authority.equity.sample")})
                </li>
              ))}
            </ul>
          )}
        </section>
      ) : null}

      {curriculum ? (
        <section className="space-y-3">
          <h2 className="text-xl font-medium">{t("authority.curriculum.title")}</h2>
          <p className="text-sm text-black/60">{t("authority.curriculum.subtitle")}</p>
          {curriculum.regions.length === 0 ? (
            <p className="text-sm text-black/60">{t("authority.empty")}</p>
          ) : (
            <ul className="space-y-2">
              {curriculum.regions.map((item) => (
                <li
                  key={`${item.regionCode}-${item.curriculumCode}-${item.subjectCode}`}
                  className="text-sm"
                >
                  {item.regionCode} · {item.curriculumCode}/{item.subjectCode}:{" "}
                  {t("authority.curriculum.mastery")}{" "}
                  {formatPercent(item.masteryRatePercent)},{" "}
                  {t("authority.curriculum.coverage")}{" "}
                  {formatPercent(item.coveragePercent)} (
                  {formatCountCell(item.schoolsReporting, suppressedLabel)}{" "}
                  {t("authority.curriculum.schools")})
                </li>
              ))}
            </ul>
          )}
        </section>
      ) : null}

      {interventions ? (
        <section className="space-y-3">
          <h2 className="text-xl font-medium">{t("authority.interventions.title")}</h2>
          <p className="text-sm text-black/60">{t("authority.interventions.subtitle")}</p>
          {interventions.regions.length === 0 ? (
            <p className="text-sm text-black/60">{t("authority.empty")}</p>
          ) : (
            <ul className="space-y-2">
              {interventions.regions.map((item) => (
                <li
                  key={`${item.regionCode}-${item.interventionType}`}
                  className="text-sm"
                >
                  {item.regionCode} · {item.interventionType}:{" "}
                  {formatCountCell(item.successfulCount, suppressedLabel)}/
                  {formatCountCell(item.totalCount, suppressedLabel)} (
                  {formatPercent(item.successRatePercent)}),{" "}
                  {t("authority.interventions.growth")}{" "}
                  {formatPercent(item.averageGrowthPercent)}
                </li>
              ))}
            </ul>
          )}
        </section>
      ) : null}
    </main>
  );
}
