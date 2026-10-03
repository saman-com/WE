"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
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

  return (
    <div className="min-h-screen p-8">
      <div className="max-w-3xl mx-auto space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-semibold">{t("dashboard.nav.regionalConfiguration")}</h1>
            <p className="text-sm text-black/70">
              {t("admin.regional.subtitle")}
            </p>
            {updatedAt ? (
              <p className="text-xs text-black/50 mt-1">
                {t("admin.regional.lastUpdated", {
                  date: new Date(updatedAt).toLocaleString(),
                })}
              </p>
            ) : null}
          </div>
          <Link href="/dashboard" className="text-sm underline">
            {t("common.backToDashboard")}
          </Link>
        </div>

        {error ? <p className="text-red-600 text-sm">{error}</p> : null}
        {success ? <p className="text-green-700 text-sm">{success}</p> : null}

        <form onSubmit={handleSubmit} className="space-y-6">
          <section className="rounded-lg border border-black/10 p-4 space-y-3">
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
          </section>

          <section className="rounded-lg border border-black/10 p-4 space-y-3">
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
          </section>

          <section className="rounded-lg border border-black/10 p-4 space-y-3">
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
          </section>

          <section className="rounded-lg border border-black/10 p-4 space-y-3">
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
          </section>

          <section className="rounded-lg border border-black/10 p-4 space-y-3">
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
          </section>

          <button
            type="submit"
            disabled={saving}
            className="rounded border border-black/20 px-4 py-2 text-sm disabled:opacity-50"
          >
            {saving ? t("common.saving") : t("admin.regional.saveConfiguration")}
          </button>
        </form>
      </div>
    </div>
  );
}
