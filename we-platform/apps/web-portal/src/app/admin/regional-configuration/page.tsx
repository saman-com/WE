"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { FocusCard, LearningFrame, PrimaryButton } from "@/components/learning-frame";
import { fetchProfile, type UserProfile } from "@/lib/auth";
import {
  fetchRegionalConfiguration,
  updateRegionalConfiguration,
  type RegionalConfiguration,
  type UpdateRegionalConfigurationRequest,
} from "@/lib/regional-configuration";
import { useI18n } from "@/i18n/I18nProvider";

function isTenantAdmin(profile: UserProfile): boolean {
  return (
    profile.roles.includes("SchoolLeader") || profile.roles.includes("SystemAdministrator")
  );
}

function emptyForm(): UpdateRegionalConfigurationRequest {
  return {
    academicCalendar: {
      terms: [{ name: "Term 1", startDate: "2026-02-01", endDate: "2026-04-15" }],
      holidays: [{ name: "Summer Break", date: "2026-12-20" }],
    },
    gradingScale: {
      name: "Letter Grades",
      levels: [
        { label: "A", minScore: 85, maxScore: 100 },
        { label: "B", minScore: 70, maxScore: 84.99 },
      ],
    },
    assessmentModels: [{ name: "Ongoing Checks", category: "Formative" }],
    reportingTemplates: [{ id: "term-summary", name: "Term Summary", format: "pdf" }],
    localeSettings: {
      languageCode: "en-NZ",
      regionCode: "NZ",
      dateFormat: "dd/MM/yyyy",
      timeZone: "Pacific/Auckland",
    },
  };
}

function hasRealUpdate(updatedAt: string | null): boolean {
  if (!updatedAt) {
    return false;
  }
  const time = Date.parse(updatedAt);
  return Number.isFinite(time) && time > Date.UTC(1970, 0, 2);
}

function toForm(config: RegionalConfiguration): UpdateRegionalConfigurationRequest {
  return {
    academicCalendar: config.academicCalendar,
    gradingScale: config.gradingScale,
    assessmentModels: config.assessmentModels,
    reportingTemplates: config.reportingTemplates,
    localeSettings: config.localeSettings,
  };
}

export default function RegionalConfigurationAdminPage() {
  const router = useRouter();
  const { t } = useI18n();
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [form, setForm] = useState<UpdateRegionalConfigurationRequest>(emptyForm);
  const [updatedAt, setUpdatedAt] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [tab, setTab] = useState("calendar");

  useEffect(() => {
    const token = localStorage.getItem("we_access_token");
    if (!token) {
      router.replace("/login");
      return;
    }

    fetchProfile(token)
      .then(async (loaded) => {
        if (!isTenantAdmin(loaded)) {
          router.replace("/dashboard");
          return;
        }

        setProfile(loaded);
        const config = await fetchRegionalConfiguration(token);
        setForm(toForm(config));
        setUpdatedAt(config.updatedAt);
      })
      .catch(() => {
        localStorage.removeItem("we_access_token");
        setError(t("common.sessionExpired"));
      })
      .finally(() => setLoading(false));
  }, [router, t]);

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const token = localStorage.getItem("we_access_token");
    if (!token) {
      router.replace("/login");
      return;
    }

    setSaving(true);
    setError(null);
    setSuccess(null);

    try {
      const saved = await updateRegionalConfiguration(token, form);
      setForm(toForm(saved));
      setUpdatedAt(saved.updatedAt);
      setSuccess(t("admin.regional.saved"));
    } catch {
      setError(t("admin.regional.saveError"));
    } finally {
      setSaving(false);
    }
  }

  if (error && !profile) {
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

  if (loading || !profile) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6">
        <p>{t("admin.regional.loading")}</p>
      </div>
    );
  }

  if (!profile) {
    return null;
  }

  return (
    <LearningFrame
      eyebrow="WE"
      title={profile.name}
      onSignOut={() => {
        localStorage.removeItem("we_access_token");
        router.push("/login");
      }}
      signOutLabel={t("common.signOut")}
      tabs={[
        { id: "calendar", label: t("admin.regional.calendarTitle") },
        { id: "grading", label: t("admin.regional.gradingTitle") },
        { id: "models", label: t("admin.regional.modelsTitle") },
        { id: "templates", label: t("admin.regional.templatesTitle") },
        { id: "locale", label: t("admin.regional.localeTitle") },
      ]}
      activeTab={tab}
      onTabChange={setTab}
    >
      <div className="space-y-6">
        <div>
          <h1 className="text-2xl font-semibold">{t("dashboard.nav.regionalConfiguration")}</h1>
          {hasRealUpdate(updatedAt) ? (
            <p className="mt-1 text-xs text-black/50">
              {t("admin.regional.lastUpdated", {
                date: new Date(updatedAt!).toLocaleString(),
              })}
            </p>
          ) : null}
        </div>

        {error || success ? (
          <p role="status" className={`text-sm ${error ? "text-red-700" : "text-green-800"}`}>
            {error ?? success}
          </p>
        ) : null}

        <form onSubmit={handleSubmit} className="space-y-6">
          {tab === "calendar" ? (
          <FocusCard>
            <h2 className="font-medium">{t("admin.regional.calendarTitle")}</h2>
            <label className="text-sm space-y-1 block">
              <span>{t("admin.regional.firstTermName")}</span>
              <input
                className="w-full border border-black/20 rounded px-2 py-1"
                value={form.academicCalendar.terms[0]?.name ?? ""}
                onChange={(event) =>
                  setForm((current) => ({
                    ...current,
                    academicCalendar: {
                      ...current.academicCalendar,
                      terms: [
                        {
                          ...current.academicCalendar.terms[0],
                          name: event.target.value,
                        },
                      ],
                    },
                  }))
                }
              />
            </label>
            <div className="grid gap-3 md:grid-cols-2">
              <label className="text-sm space-y-1">
                <span>{t("admin.regional.termStart")}</span>
                <input
                  type="date"
                  className="w-full border border-black/20 rounded px-2 py-1"
                  value={form.academicCalendar.terms[0]?.startDate ?? ""}
                  onChange={(event) =>
                    setForm((current) => ({
                      ...current,
                      academicCalendar: {
                        ...current.academicCalendar,
                        terms: [
                          {
                            ...current.academicCalendar.terms[0],
                            startDate: event.target.value,
                          },
                        ],
                      },
                    }))
                  }
                />
              </label>
              <label className="text-sm space-y-1">
                <span>{t("admin.regional.termEnd")}</span>
                <input
                  type="date"
                  className="w-full border border-black/20 rounded px-2 py-1"
                  value={form.academicCalendar.terms[0]?.endDate ?? ""}
                  onChange={(event) =>
                    setForm((current) => ({
                      ...current,
                      academicCalendar: {
                        ...current.academicCalendar,
                        terms: [
                          {
                            ...current.academicCalendar.terms[0],
                            endDate: event.target.value,
                          },
                        ],
                      },
                    }))
                  }
                />
              </label>
            </div>
            <label className="text-sm space-y-1 block">
              <span>{t("admin.regional.holidayName")}</span>
              <input
                className="w-full border border-black/20 rounded px-2 py-1"
                value={form.academicCalendar.holidays[0]?.name ?? ""}
                onChange={(event) =>
                  setForm((current) => ({
                    ...current,
                    academicCalendar: {
                      ...current.academicCalendar,
                      holidays: [
                        {
                          ...current.academicCalendar.holidays[0],
                          name: event.target.value,
                        },
                      ],
                    },
                  }))
                }
              />
            </label>
            <label className="text-sm space-y-1 block">
              <span>{t("admin.regional.holidayDate")}</span>
              <input
                type="date"
                className="w-full border border-black/20 rounded px-2 py-1"
                value={form.academicCalendar.holidays[0]?.date ?? ""}
                onChange={(event) =>
                  setForm((current) => ({
                    ...current,
                    academicCalendar: {
                      ...current.academicCalendar,
                      holidays: [
                        {
                          ...current.academicCalendar.holidays[0],
                          date: event.target.value,
                        },
                      ],
                    },
                  }))
                }
              />
            </label>
          </FocusCard>
          ) : null}

          {tab === "grading" ? (
          <FocusCard>
            <h2 className="font-medium">{t("admin.regional.gradingTitle")}</h2>
            <label className="text-sm space-y-1 block">
              <span>{t("admin.regional.scaleName")}</span>
              <input
                className="w-full border border-black/20 rounded px-2 py-1"
                value={form.gradingScale.name}
                onChange={(event) =>
                  setForm((current) => ({
                    ...current,
                    gradingScale: { ...current.gradingScale, name: event.target.value },
                  }))
                }
              />
            </label>
          </FocusCard>
          ) : null}

          {tab === "models" ? (
          <FocusCard>
            <h2 className="font-medium">{t("admin.regional.modelsTitle")}</h2>
            <label className="text-sm space-y-1 block">
              <span>{t("admin.regional.modelName")}</span>
              <input
                className="w-full border border-black/20 rounded px-2 py-1"
                value={form.assessmentModels[0]?.name ?? ""}
                onChange={(event) =>
                  setForm((current) => ({
                    ...current,
                    assessmentModels: [
                      { ...current.assessmentModels[0], name: event.target.value },
                    ],
                  }))
                }
              />
            </label>
            <label className="text-sm space-y-1 block">
              <span>{t("admin.regional.category")}</span>
              <input
                className="w-full border border-black/20 rounded px-2 py-1"
                value={form.assessmentModels[0]?.category ?? ""}
                onChange={(event) =>
                  setForm((current) => ({
                    ...current,
                    assessmentModels: [
                      { ...current.assessmentModels[0], category: event.target.value },
                    ],
                  }))
                }
              />
            </label>
          </FocusCard>
          ) : null}

          {tab === "templates" ? (
          <FocusCard>
            <h2 className="font-medium">{t("admin.regional.templatesTitle")}</h2>
            <label className="text-sm space-y-1 block">
              <span>{t("admin.regional.templateName")}</span>
              <input
                className="w-full border border-black/20 rounded px-2 py-1"
                value={form.reportingTemplates[0]?.name ?? ""}
                onChange={(event) =>
                  setForm((current) => ({
                    ...current,
                    reportingTemplates: [
                      { ...current.reportingTemplates[0], name: event.target.value },
                    ],
                  }))
                }
              />
            </label>
          </FocusCard>
          ) : null}

          {tab === "locale" ? (
          <FocusCard>
            <h2 className="font-medium">{t("admin.regional.localeTitle")}</h2>
            <div className="grid gap-3 md:grid-cols-2">
              <label className="text-sm space-y-1">
                <span>{t("admin.regional.languageCode")}</span>
                <input
                  className="w-full border border-black/20 rounded px-2 py-1"
                  value={form.localeSettings.languageCode}
                  onChange={(event) =>
                    setForm((current) => ({
                      ...current,
                      localeSettings: {
                        ...current.localeSettings,
                        languageCode: event.target.value,
                      },
                    }))
                  }
                />
              </label>
              <label className="text-sm space-y-1">
                <span>{t("admin.regional.regionCode")}</span>
                <input
                  className="w-full border border-black/20 rounded px-2 py-1"
                  value={form.localeSettings.regionCode}
                  onChange={(event) =>
                    setForm((current) => ({
                      ...current,
                      localeSettings: {
                        ...current.localeSettings,
                        regionCode: event.target.value,
                      },
                    }))
                  }
                />
              </label>
              <label className="text-sm space-y-1">
                <span>{t("admin.regional.dateFormat")}</span>
                <input
                  className="w-full border border-black/20 rounded px-2 py-1"
                  value={form.localeSettings.dateFormat}
                  onChange={(event) =>
                    setForm((current) => ({
                      ...current,
                      localeSettings: {
                        ...current.localeSettings,
                        dateFormat: event.target.value,
                      },
                    }))
                  }
                />
              </label>
              <label className="text-sm space-y-1">
                <span>{t("admin.regional.timeZone")}</span>
                <input
                  className="w-full border border-black/20 rounded px-2 py-1"
                  value={form.localeSettings.timeZone}
                  onChange={(event) =>
                    setForm((current) => ({
                      ...current,
                      localeSettings: {
                        ...current.localeSettings,
                        timeZone: event.target.value,
                      },
                    }))
                  }
                />
              </label>
            </div>
          </FocusCard>
          ) : null}

          <PrimaryButton type="submit" disabled={saving}>
            {saving ? t("common.saving") : t("admin.regional.saveConfiguration")}
          </PrimaryButton>
        </form>
      </div>
    </LearningFrame>
  );
}
