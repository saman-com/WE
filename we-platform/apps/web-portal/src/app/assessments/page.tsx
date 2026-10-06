"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { DataText } from "@/components/data-text";
import { fetchProfile, listDirectoryUsers, personName, type DirectoryUser, type UserProfile } from "@/lib/auth";
import {
  listClasses,
  listOrganisations,
  type Organisation,
  type SchoolClass,
} from "@/lib/organisation";
import {
  createAssessment,
  listAssessments,
  publishAssessment,
  type Assessment,
} from "@/lib/assessment";
import {
  getCurriculumTree,
  listCurricula,
  type Curriculum,
  type CurriculumTree,
} from "@/lib/curriculum";
import { ApiError } from "@/lib/api-error";
import { useI18n } from "@/i18n/I18nProvider";

function canManageAssessments(profile: UserProfile): boolean {
  return (
    profile.roles.includes("Teacher") ||
    profile.roles.includes("SystemAdministrator")
  );
}

export default function AssessmentsPage() {
  const router = useRouter();
  const { t, translateError } = useI18n();
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [token, setToken] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const [organisations, setOrganisations] = useState<Organisation[]>([]);
  const [organisationId, setOrganisationId] = useState("");
  const [classes, setClasses] = useState<SchoolClass[]>([]);
  const [selectedClassId, setSelectedClassId] = useState("");
  const [selectedClass, setSelectedClass] = useState<SchoolClass | null>(null);

  const [curricula, setCurricula] = useState<Curriculum[]>([]);
  const [selectedCurriculumId, setSelectedCurriculumId] = useState("");
  const [tree, setTree] = useState<CurriculumTree | null>(null);

  const [assessments, setAssessments] = useState<Assessment[]>([]);
  const [assessmentsHasMore, setAssessmentsHasMore] = useState(false);
  const [assessmentsCursor, setAssessmentsCursor] = useState<string | null>(null);
  const [loadingMoreAssessments, setLoadingMoreAssessments] = useState(false);
  const [people, setPeople] = useState<DirectoryUser[]>([]);

  const [title, setTitle] = useState("");
  const [instructions, setInstructions] = useState("");
  const [dueAt, setDueAt] = useState("");
  const [selectedLearningObjectiveIds, setSelectedLearningObjectiveIds] =
    useState<string[]>([]);
  const [selectedMicroSkillIds, setSelectedMicroSkillIds] = useState<string[]>(
    []
  );
  const [defaultsReady, setDefaultsReady] = useState(false);

  useEffect(() => {
    if (!defaultsReady) {
      setTitle(t("assessments.defaults.title"));
      setInstructions(t("assessments.defaults.instructions"));
      setDefaultsReady(true);
    }
  }, [defaultsReady, t]);

  useEffect(() => {
    const stored = localStorage.getItem("we_access_token");
    if (!stored) {
      router.replace("/login");
      return;
    }

    setToken(stored);
    fetchProfile(stored)
      .then(async (loaded) => {
        if (!canManageAssessments(loaded)) {
          setError(t("assessments.errorNoPermission"));
          return;
        }
        setProfile(loaded);
        listDirectoryUsers(stored).then(setPeople).catch(() => setPeople([]));
        try {
          const loadedOrgs = await listOrganisations(stored);
          setOrganisations(loadedOrgs);
          if (loadedOrgs[0]) {
            setOrganisationId(loadedOrgs[0].id);
          }
        } catch {
          setOrganisations([]);
        }
      })
      .catch(() => {
        localStorage.removeItem("we_access_token");
        router.replace("/login");
      });
  }, [router, t]);

  useEffect(() => {
    if (!token || !organisationId) {
      return;
    }

    listClasses(token, organisationId)
      .then((loaded) => {
        setClasses(loaded);
        if (loaded[0]) {
          setSelectedClassId(loaded[0].id);
          setSelectedClass(loaded[0]);
        } else {
          setSelectedClassId("");
          setSelectedClass(null);
        }
      })
      .catch(() => setClasses([]));

    listCurricula(token, organisationId)
      .then(async (items) => {
        setCurricula(items);
        if (items[0]) {
          setSelectedCurriculumId(items[0].id);
          const loadedTree = await getCurriculumTree(token, items[0].id);
          setTree(loadedTree);
        } else {
          setSelectedCurriculumId("");
          setTree(null);
        }
      })
      .catch(() => {
        setCurricula([]);
        setTree(null);
      });
  }, [token, organisationId]);

  useEffect(() => {
    if (!token || !organisationId || !selectedClassId) {
      setAssessments([]);
      setAssessmentsHasMore(false);
      setAssessmentsCursor(null);
      return;
    }

    listAssessments(token, organisationId, selectedClassId)
      .then((page) => {
        setAssessments(page.items);
        setAssessmentsHasMore(page.hasMore);
        setAssessmentsCursor(page.nextCursor);
        setError(null);
      })
      .catch(() => {
        setAssessments([]);
        setAssessmentsHasMore(false);
        setAssessmentsCursor(null);
        setError(t("common.requestFailed"));
      });
  }, [token, organisationId, selectedClassId, message, t]);

  async function loadMoreAssessments() {
    if (!token || !organisationId || !selectedClassId || !assessmentsCursor || loadingMoreAssessments) {
      return;
    }
    setLoadingMoreAssessments(true);
    try {
      const page = await listAssessments(token, organisationId, selectedClassId, {
        cursor: assessmentsCursor,
      });
      setAssessments((current) => [...current, ...page.items]);
      setAssessmentsHasMore(page.hasMore);
      setAssessmentsCursor(page.nextCursor);
    } catch {
      setError(t("common.requestFailed"));
    } finally {
      setLoadingMoreAssessments(false);
    }
  }

  useEffect(() => {
    const schoolClass = classes.find((item) => item.id === selectedClassId) ?? null;
    setSelectedClass(schoolClass);
  }, [classes, selectedClassId]);

  async function run(action: () => Promise<void>) {
    setBusy(true);
    setError(null);
    setMessage(null);
    try {
      await action();
    } catch (err) {
      if (err instanceof ApiError) {
        setError(translateError(err.code, "common.requestFailed"));
      } else {
        setError(t("common.requestFailed"));
      }
    } finally {
      setBusy(false);
    }
  }

  function toggleLearningObjective(id: string) {
    setSelectedLearningObjectiveIds((current) =>
      current.includes(id) ? current.filter((item) => item !== id) : [...current, id]
    );
  }

  function toggleMicroSkill(id: string) {
    setSelectedMicroSkillIds((current) =>
      current.includes(id) ? current.filter((item) => item !== id) : [...current, id]
    );
  }

  async function handleCreateAssessment() {
    if (!token || !organisationId || !selectedClassId) {
      return;
    }

    await run(async () => {
      await createAssessment(token, {
        organisationId,
        classId: selectedClassId,
        title,
        instructions: instructions || null,
        dueAt: dueAt ? new Date(dueAt).toISOString() : null,
        learningObjectiveIds: selectedLearningObjectiveIds,
        microSkillIds: selectedMicroSkillIds,
      });
      setMessage(t("assessments.message.createdDraft"));
    });
  }

  async function handlePublish(assessmentId: string) {
    if (!token) {
      return;
    }

    await run(async () => {
      await publishAssessment(token, assessmentId);
      setMessage(t("assessments.message.published"));
    });
  }

  if (error && !profile) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6">
        <div className="space-y-4 text-center">
          <p className="text-red-600">{error}</p>
          <Link href="/dashboard" className="underline">
            {t("common.backToDashboard")}
          </Link>
        </div>
      </div>
    );
  }

  if (!profile || !token) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6">
        <p>{t("assessments.loading")}</p>
      </div>
    );
  }

  return (
    <div className="min-h-screen p-8">
      <div className="max-w-3xl mx-auto space-y-6">
        <div className="flex items-center justify-between">
          <h1 className="text-2xl font-semibold">{t("assessments.title")}</h1>
          <Link href="/dashboard" className="text-sm underline">
            {t("common.dashboard")}
          </Link>
        </div>

        {error ? <p className="text-red-600">{error}</p> : null}
        {message ? <p className="text-green-700">{message}</p> : null}

        <div className="rounded-lg border border-black/10 p-6 space-y-4">
          <h2 className="font-medium">{t("assessments.scope.title")}</h2>
          <label className="block space-y-1">
            <span className="text-sm">{t("assessments.scope.organisationLabel")}</span>
            <select
              className="w-full border rounded px-3 py-2"
              value={organisationId}
              onChange={(event) => setOrganisationId(event.target.value)}
            >
              {organisations.map((organisation) => (
                <option key={organisation.id} value={organisation.id} dir="auto">
                  {organisation.name}
                </option>
              ))}
            </select>
          </label>

          <label className="block space-y-1">
            <span className="text-sm">{t("assessments.scope.classLabel")}</span>
            <select
              className="w-full border rounded px-3 py-2"
              value={selectedClassId}
              onChange={(event) => setSelectedClassId(event.target.value)}
            >
              {classes.map((schoolClass) => (
                <option key={schoolClass.id} value={schoolClass.id} dir="auto">
                  {schoolClass.name} ({schoolClass.code})
                </option>
              ))}
            </select>
          </label>

          {selectedClass ? (
            <div className="text-sm space-y-1">
              <p className="font-medium">{t("assessments.scope.rosterTitle")}</p>
              {selectedClass.studentUserIds && selectedClass.studentUserIds.length > 0 ? (
                <ul className="list-disc pl-5">
                  {selectedClass.studentUserIds.map((studentUserId) => (
                    <li key={studentUserId}>
                      <DataText>
                        {personName(people, studentUserId, t("organisation.manage.unknownPerson"))}
                      </DataText>
                    </li>
                  ))}
                </ul>
              ) : (
                <p className="text-black/60">{t("assessments.scope.noStudentsEnrolled")}</p>
              )}
            </div>
          ) : null}
        </div>

        <div className="rounded-lg border border-black/10 p-6 space-y-4">
          <h2 className="font-medium">{t("assessments.create.title")}</h2>

          <label className="block space-y-1">
            <span className="text-sm">{t("assessments.create.curriculumLabel")}</span>
            <select
              className="w-full border rounded px-3 py-2"
              value={selectedCurriculumId}
              onChange={async (event) => {
                const curriculumId = event.target.value;
                setSelectedCurriculumId(curriculumId);
                if (curriculumId) {
                  const loadedTree = await getCurriculumTree(token, curriculumId);
                  setTree(loadedTree);
                } else {
                  setTree(null);
                }
              }}
            >
              {curricula.map((curriculum) => (
                <option key={curriculum.id} value={curriculum.id} dir="auto">
                  {curriculum.name} ({curriculum.version})
                </option>
              ))}
            </select>
          </label>

          {tree ? (
            <div className="text-sm space-y-3 max-h-64 overflow-y-auto border rounded p-3">
              {tree.subjects.flatMap((subject) =>
                subject.units.flatMap((unit) =>
                  unit.learningObjectives.map((objective) => (
                    <div key={objective.id} className="space-y-1">
                      <label className="flex items-center gap-2">
                        <input
                          type="checkbox"
                          checked={selectedLearningObjectiveIds.includes(objective.id)}
                          onChange={() => toggleLearningObjective(objective.id)}
                        />
                        <span>{objective.title}</span>
                      </label>
                      <ul className="pl-6 space-y-1">
                        {objective.microSkills.map((microSkill) => (
                          <li key={microSkill.id}>
                            <label className="flex items-center gap-2">
                              <input
                                type="checkbox"
                                checked={selectedMicroSkillIds.includes(microSkill.id)}
                                onChange={() => toggleMicroSkill(microSkill.id)}
                              />
                              <span>{microSkill.name}</span>
                            </label>
                          </li>
                        ))}
                      </ul>
                    </div>
                  ))
                )
              )}
            </div>
          ) : (
            <p className="text-sm text-black/60">
              {t("assessments.create.noCurriculumTree")}
            </p>
          )}

          <label className="block space-y-1">
            <span className="text-sm">{t("assessments.create.titleLabel")}</span>
            <input
              className="w-full border rounded px-3 py-2"
              value={title}
              onChange={(event) => setTitle(event.target.value)}
            />
          </label>

          <label className="block space-y-1">
            <span className="text-sm">{t("assessments.create.instructionsLabel")}</span>
            <textarea
              className="w-full border rounded px-3 py-2"
              rows={3}
              value={instructions}
              onChange={(event) => setInstructions(event.target.value)}
            />
          </label>

          <label className="block space-y-1">
            <span className="text-sm">{t("assessments.create.dueDateLabel")}</span>
            <input
              type="datetime-local"
              className="w-full border rounded px-3 py-2"
              value={dueAt}
              onChange={(event) => setDueAt(event.target.value)}
            />
          </label>

          <button
            type="button"
            disabled={busy || !selectedClassId}
            onClick={handleCreateAssessment}
            className="rounded bg-black text-white px-4 py-2 text-sm disabled:opacity-50"
          >
            {t("assessments.create.submit")}
          </button>
        </div>

        <div className="rounded-lg border border-black/10 p-6 space-y-4">
          <h2 className="font-medium">{t("assessments.list.title")}</h2>
          {assessments.length === 0 ? (
            <p className="text-sm text-black/60">{t("assessments.list.empty")}</p>
          ) : (
            <ul className="space-y-4">
              {assessments.map((assessment) => (
                <li key={assessment.id} className="border rounded p-4 space-y-2">
                  <div className="flex items-center justify-between gap-4">
                    <p className="font-medium">
                      <DataText>{assessment.title}</DataText>
                    </p>
                    <span className="text-xs tracking-wide">
                      {t(`assessments.status.${assessment.status}`)}
                    </span>
                  </div>
                  {assessment.instructions ? (
                    <p className="text-sm text-black/70">
                      <DataText>{assessment.instructions}</DataText>
                    </p>
                  ) : null}
                  <p className="text-sm">
                    {t("assessments.list.loMicroSkillsSummary", {
                      loCount: assessment.learningObjectiveIds.length,
                      microSkillCount: assessment.microSkillIds.length,
                    })}
                  </p>
                  {assessment.dueAt ? (
                    <p className="text-sm">
                      {t("assessments.list.duePrefix")}{" "}
                      {new Date(assessment.dueAt).toLocaleString()}
                    </p>
                  ) : null}
                  {assessment.status === "Draft" ? (
                    <button
                      type="button"
                      disabled={busy}
                      onClick={() => handlePublish(assessment.id)}
                      className="text-sm underline disabled:opacity-50"
                    >
                      {t("assessments.list.publish")}
                    </button>
                  ) : (
                    <Link
                      href={`/assessments/${assessment.id}/review`}
                      className="text-sm underline"
                    >
                      {t("assessments.list.reviewSubmissions")}
                    </Link>
                  )}
                </li>
              ))}
            </ul>
          )}
          {assessmentsHasMore ? (
            <button
              type="button"
              disabled={loadingMoreAssessments}
              onClick={loadMoreAssessments}
              className="text-sm underline disabled:opacity-50"
            >
              {loadingMoreAssessments ? t("common.loading") : t("common.loadMore")}
            </button>
          ) : null}
        </div>
      </div>
    </div>
  );
}
