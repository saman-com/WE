"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { FocusCard, LearningFrame, PrimaryButton } from "@/components/learning-frame";
import { fetchProfile, type UserProfile } from "@/lib/auth";
import { roleHome } from "@/lib/role-home";
import { moveGradingLevel, validateGradingLevels } from "@/lib/grading-levels";
import {
  fetchRegionalConfiguration,
  updateRegionalConfiguration,
  type GradingLevel,
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
  const [draftLevel, setDraftLevel] = useState<GradingLevel>({
    label: "",
    minScore: 0,
    maxScore: 0,
  });

  useEffect(() => {
    const token = localStorage.getItem("we_access_token");
    if (!token) {
      router.replace("/login");
      return;
    }

    fetchProfile(token)
      .then(async (loaded) => {
        if (!isTenantAdmin(loaded)) {
          router.replace(roleHome(loaded.roles));
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

    const problem = validateGradingLevels(form.gradingScale.levels);
    if (problem) {
      setError(t(problem === "overlap" ? "admin.regional.levelOverlap" : "admin.regional.levelRange"));
      setSaving(false);
      return;
    }

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
          <>
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
            <h3 className="font-medium">{t("admin.regional.levelsTitle")}</h3>
            {form.gradingScale.levels.length === 0 ? (
              <p className="text-sm text-black/60">{t("admin.regional.noLevels")}</p>
            ) : (
              <ul className="space-y-4">
                {form.gradingScale.levels.map((level, index) => (
                  <li key={index} className="space-y-2">
                    <LevelFields
                      level={level}
                      onChange={(next) =>
                        setForm((current) => ({
                          ...current,
                          gradingScale: {
                            ...current.gradingScale,
                            levels: current.gradingScale.levels.map((item, itemIndex) =>
                              itemIndex === index ? next : item
                            ),
                          },
                        }))
                      }
                    />
                    <div className="flex flex-wrap gap-3">
                      <TextButton
                        onClick={() =>
                          setForm((current) => ({
                            ...current,
                            gradingScale: {
                              ...current.gradingScale,
                              levels: moveGradingLevel(current.gradingScale.levels, index, -1),
                            },
                          }))
                        }
                      >
                        {t("admin.regional.moveUp")}
                      </TextButton>
                      <TextButton
                        onClick={() =>
                          setForm((current) => ({
                            ...current,
                            gradingScale: {
                              ...current.gradingScale,
                              levels: moveGradingLevel(current.gradingScale.levels, index, 1),
                            },
                          }))
                        }
                      >
                        {t("admin.regional.moveDown")}
                      </TextButton>
                      <TextButton
                        onClick={() =>
                          setForm((current) => ({
                            ...current,
                            gradingScale: {
                              ...current.gradingScale,
                              levels: current.gradingScale.levels.filter(
                                (_, itemIndex) => itemIndex !== index
                              ),
                            },
                          }))
                        }
                      >
                        {t("organisation.manage.remove")}
                      </TextButton>
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </FocusCard>
          <FocusCard>
            <h2 className="font-medium">{t("admin.regional.addLevelTitle")}</h2>
            <p className="text-sm text-black/60">{t("admin.regional.levelsSavedWithConfiguration")}</p>
            <LevelFields level={draftLevel} onChange={setDraftLevel} />
            <SecondaryButton
              onClick={() => {
                const next = [...form.gradingScale.levels, draftLevel];
                const problem = validateGradingLevels(next);
                if (problem) {
                  setSuccess(null);
                  setError(
                    t(problem === "overlap" ? "admin.regional.levelOverlap" : "admin.regional.levelRange")
                  );
                  return;
                }
                setForm((current) => ({
                  ...current,
                  gradingScale: { ...current.gradingScale, levels: next },
                }));
                setDraftLevel({ label: "", minScore: 0, maxScore: 0 });
                setError(null);
              }}
            >
              {t("admin.regional.addLevel")}
            </SecondaryButton>
          </FocusCard>
          </>
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

function LevelFields({
  level,
  onChange,
}: {
  level: GradingLevel;
  onChange: (level: GradingLevel) => void;
}) {
  const { t } = useI18n();
  return (
    <div className="grid gap-3 md:grid-cols-3">
      <label className="block space-y-1 text-sm">
        <span>{t("admin.regional.levelLabel")}</span>
        <input
          className="w-full rounded border border-black/20 px-2 py-1"
          value={level.label}
          onChange={(event) => onChange({ ...level, label: event.target.value })}
        />
      </label>
      <label className="block space-y-1 text-sm">
        <span>{t("admin.regional.minScore")}</span>
        <input
          type="number"
          className="w-full rounded border border-black/20 px-2 py-1"
          value={level.minScore}
          onChange={(event) => onChange({ ...level, minScore: Number(event.target.value) })}
        />
      </label>
      <label className="block space-y-1 text-sm">
        <span>{t("admin.regional.maxScore")}</span>
        <input
          type="number"
          className="w-full rounded border border-black/20 px-2 py-1"
          value={level.maxScore}
          onChange={(event) => onChange({ ...level, maxScore: Number(event.target.value) })}
        />
      </label>
    </div>
  );
}

function SecondaryButton({ children, onClick }: { children: React.ReactNode; onClick: () => void }) {
  return (
    <button
      type="button"
      onClick={onClick}
      className="inline-flex rounded-lg border border-[#1c1917] px-4 py-2.5 text-sm font-semibold text-[#1c1917]"
    >
      {children}
    </button>
  );
}

function TextButton({ children, onClick }: { children: React.ReactNode; onClick: () => void }) {
  return (
    <button
      type="button"
      onClick={onClick}
      className="text-sm text-black/60 underline-offset-2 hover:underline"
    >
      {children}
    </button>
  );
}
