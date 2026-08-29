"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { fetchProfile, type UserProfile } from "@/lib/auth";
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

function canManageAssessments(profile: UserProfile): boolean {
  return (
    profile.roles.includes("Teacher") ||
    profile.roles.includes("SystemAdministrator")
  );
}

export default function AssessmentsPage() {
  const router = useRouter();
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

  const [title, setTitle] = useState("Unit assessment");
  const [instructions, setInstructions] = useState(
    "Complete all questions by the due date."
  );
  const [dueAt, setDueAt] = useState("");
  const [selectedLearningObjectiveIds, setSelectedLearningObjectiveIds] =
    useState<string[]>([]);
  const [selectedMicroSkillIds, setSelectedMicroSkillIds] = useState<string[]>(
    []
  );

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
          setError("You do not have assessment-management permission.");
          return;
        }
        setProfile(loaded);
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
  }, [router]);

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
      return;
    }

    listAssessments(token, organisationId, selectedClassId)
      .then(setAssessments)
      .catch(() => setAssessments([]));
  }, [token, organisationId, selectedClassId, message]);

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
      setError(err instanceof Error ? err.message : "Request failed.");
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
      setMessage("Assessment created in draft.");
    });
  }

  async function handlePublish(assessmentId: string) {
    if (!token) {
      return;
    }

    await run(async () => {
      await publishAssessment(token, assessmentId);
      setMessage("Assessment published for students.");
    });
  }

  if (error && !profile) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6">
        <div className="space-y-4 text-center">
          <p className="text-red-600">{error}</p>
          <Link href="/dashboard" className="underline">
            Back to dashboard
          </Link>
        </div>
      </div>
    );
  }

  if (!profile || !token) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6">
        <p>Loading...</p>
      </div>
    );
  }

  return (
    <div className="min-h-screen p-8">
      <div className="max-w-3xl mx-auto space-y-6">
        <div className="flex items-center justify-between">
          <h1 className="text-2xl font-semibold">Assessments</h1>
          <Link href="/dashboard" className="text-sm underline">
            Dashboard
          </Link>
        </div>

        {error ? <p className="text-red-600">{error}</p> : null}
        {message ? <p className="text-green-700">{message}</p> : null}

        <div className="rounded-lg border border-black/10 p-6 space-y-4">
          <h2 className="font-medium">Scope</h2>
          <label className="block space-y-1">
            <span className="text-sm">Organisation</span>
            <select
              className="w-full border rounded px-3 py-2"
              value={organisationId}
              onChange={(event) => setOrganisationId(event.target.value)}
            >
              {organisations.map((organisation) => (
                <option key={organisation.id} value={organisation.id}>
                  {organisation.name}
                </option>
              ))}
            </select>
          </label>

          <label className="block space-y-1">
            <span className="text-sm">Class</span>
            <select
              className="w-full border rounded px-3 py-2"
              value={selectedClassId}
              onChange={(event) => setSelectedClassId(event.target.value)}
            >
              {classes.map((schoolClass) => (
                <option key={schoolClass.id} value={schoolClass.id}>
                  {schoolClass.name} ({schoolClass.code})
                </option>
              ))}
            </select>
          </label>

          {selectedClass ? (
            <div className="text-sm space-y-1">
              <p className="font-medium">Class roster</p>
              {selectedClass.studentUserIds && selectedClass.studentUserIds.length > 0 ? (
                <ul className="list-disc pl-5">
                  {selectedClass.studentUserIds.map((studentUserId) => (
                    <li key={studentUserId}>{studentUserId}</li>
                  ))}
                </ul>
              ) : (
                <p className="text-black/60">No students enrolled.</p>
              )}
            </div>
          ) : null}
        </div>

        <div className="rounded-lg border border-black/10 p-6 space-y-4">
          <h2 className="font-medium">Create draft assessment</h2>

          <label className="block space-y-1">
            <span className="text-sm">Curriculum (for LO / micro-skill links)</span>
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
                <option key={curriculum.id} value={curriculum.id}>
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
              No curriculum tree available. Create LOs and micro-skills under Curriculum first.
            </p>
          )}

          <label className="block space-y-1">
            <span className="text-sm">Title</span>
            <input
              className="w-full border rounded px-3 py-2"
              value={title}
              onChange={(event) => setTitle(event.target.value)}
            />
          </label>

          <label className="block space-y-1">
            <span className="text-sm">Instructions</span>
            <textarea
              className="w-full border rounded px-3 py-2"
              rows={3}
              value={instructions}
              onChange={(event) => setInstructions(event.target.value)}
            />
          </label>

          <label className="block space-y-1">
            <span className="text-sm">Due date</span>
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
            Create draft
          </button>
        </div>

        <div className="rounded-lg border border-black/10 p-6 space-y-4">
          <h2 className="font-medium">Assessments for class</h2>
          {assessments.length === 0 ? (
            <p className="text-sm text-black/60">No assessments yet.</p>
          ) : (
            <ul className="space-y-4">
              {assessments.map((assessment) => (
                <li key={assessment.id} className="border rounded p-4 space-y-2">
                  <div className="flex items-center justify-between gap-4">
                    <p className="font-medium">{assessment.title}</p>
                    <span className="text-xs uppercase tracking-wide">{assessment.status}</span>
                  </div>
                  {assessment.instructions ? (
                    <p className="text-sm text-black/70">{assessment.instructions}</p>
                  ) : null}
                  <p className="text-sm">
                    LOs: {assessment.learningObjectiveIds.length} · Micro-skills:{" "}
                    {assessment.microSkillIds.length}
                  </p>
                  {assessment.dueAt ? (
                    <p className="text-sm">Due: {new Date(assessment.dueAt).toLocaleString()}</p>
                  ) : null}
                  {assessment.status === "Draft" ? (
                    <button
                      type="button"
                      disabled={busy}
                      onClick={() => handlePublish(assessment.id)}
                      className="text-sm underline disabled:opacity-50"
                    >
                      Publish
                    </button>
                  ) : null}
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>
    </div>
  );
}
