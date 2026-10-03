"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { fetchProfile, type UserProfile } from "@/lib/auth";
import { listOrganisations, type Organisation } from "@/lib/organisation";
import {
  createCurriculum,
  createLearningObjective,
  createMicroSkill,
  createSubject,
  createTopic,
  createUnit,
  getCurriculumTree,
  inheritCurriculum,
  listCurricula,
  updateLearningObjective,
  updateUnit,
  type Curriculum,
  type CurriculumTree,
} from "@/lib/curriculum";

function canManageCurriculum(profile: UserProfile): boolean {
  return (
    profile.roles.includes("Teacher") ||
    profile.roles.includes("SystemAdministrator")
  );
}

export default function CurriculumPage() {
  const router = useRouter();
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [token, setToken] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const [organisations, setOrganisations] = useState<Organisation[]>([]);
  const [organisationId, setOrganisationId] = useState("");
  const [curricula, setCurricula] = useState<Curriculum[]>([]);
  const [selectedCurriculumId, setSelectedCurriculumId] = useState("");
  const [tree, setTree] = useState<CurriculumTree | null>(null);
  const [expandedSubjects, setExpandedSubjects] = useState<string[]>([]);

  const [curriculumName, setCurriculumName] = useState("Cambridge Science");
  const [curriculumVersion, setCurriculumVersion] = useState("2027");
  const [regionCode, setRegionCode] = useState("NZ-NCEA");
  const [createAsRegional, setCreateAsRegional] = useState(false);
  const [parentCurriculumId, setParentCurriculumId] = useState("");
  const [overrideUnitName, setOverrideUnitName] = useState("");
  const [overrideObjectiveTitle, setOverrideObjectiveTitle] = useState("");
  const [subjectName, setSubjectName] = useState("Chemistry");
  const [subjectCode, setSubjectCode] = useState("CHEM");
  const [unitName, setUnitName] = useState("Chemical Reactions");
  const [unitSubjectId, setUnitSubjectId] = useState("");
  const [topicName, setTopicName] = useState("Physical Changes");
  const [topicUnitId, setTopicUnitId] = useState("");
  const [objectiveTitle, setObjectiveTitle] = useState("Balance chemical equations");
  const [objectiveUnitId, setObjectiveUnitId] = useState("");
  const [microSkillName, setMicroSkillName] = useState("Identify reactants and products");
  const [microSkillObjectiveId, setMicroSkillObjectiveId] = useState("");

  useEffect(() => {
    const stored = localStorage.getItem("we_access_token");
    if (!stored) {
      router.replace("/login");
      return;
    }

    setToken(stored);
    fetchProfile(stored)
      .then(async (loaded) => {
        if (!canManageCurriculum(loaded)) {
          setError("You do not have curriculum-management permission.");
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

  async function refreshTree(accessToken: string, curriculumId: string) {
    const loaded = await getCurriculumTree(accessToken, curriculumId);
    setTree(loaded);
    setExpandedSubjects(loaded.subjects.map((subject) => subject.id));
    if (loaded.subjects[0] && !unitSubjectId) {
      setUnitSubjectId(loaded.subjects[0].id);
    }
    const firstUnit = loaded.subjects.flatMap((subject) => subject.units)[0];
    if (firstUnit && !topicUnitId) {
      setTopicUnitId(firstUnit.id);
    }
    if (firstUnit && !objectiveUnitId) {
      setObjectiveUnitId(firstUnit.id);
    }
    const firstObjective = loaded.subjects
      .flatMap((subject) => subject.units)
      .flatMap((unit) => unit.learningObjectives)[0];
    if (firstObjective && !microSkillObjectiveId) {
      setMicroSkillObjectiveId(firstObjective.id);
    }
  }

  async function loadCurricula(accessToken: string, orgId: string) {
    const items = await listCurricula(accessToken, orgId);
    setCurricula(items);
    if (items[0]) {
      setSelectedCurriculumId(items[0].id);
      await refreshTree(accessToken, items[0].id);
    } else {
      setSelectedCurriculumId("");
      setTree(null);
    }
  }

  useEffect(() => {
    if (!token || !organisationId || !profile) {
      return;
    }

    let cancelled = false;
    listCurricula(token, organisationId)
      .then(async (items) => {
        if (cancelled) {
          return;
        }
        setCurricula(items);
        if (items[0]) {
          setSelectedCurriculumId(items[0].id);
          const loaded = await getCurriculumTree(token, items[0].id);
          if (cancelled) {
            return;
          }
          setTree(loaded);
          setExpandedSubjects(loaded.subjects.map((subject) => subject.id));
          if (loaded.subjects[0]) {
            setUnitSubjectId(loaded.subjects[0].id);
          }
          const firstUnit = loaded.subjects.flatMap((subject) => subject.units)[0];
          if (firstUnit) {
            setTopicUnitId(firstUnit.id);
            setObjectiveUnitId(firstUnit.id);
          }
          const firstObjective = loaded.subjects
            .flatMap((subject) => subject.units)
            .flatMap((unit) => unit.learningObjectives)[0];
          if (firstObjective) {
            setMicroSkillObjectiveId(firstObjective.id);
          }
        } else {
          setSelectedCurriculumId("");
          setTree(null);
        }
      })
      .catch(() => {
        if (!cancelled) {
          setError("Unable to load curricula for this organisation.");
        }
      });

    return () => {
      cancelled = true;
    };
  }, [token, organisationId, profile]);

  async function run(action: () => Promise<void>) {
    if (!token) {
      return;
    }
    setBusy(true);
    setError(null);
    setMessage(null);
    try {
      await action();
    } catch {
      setError("Request failed. Check the form values and try again.");
    } finally {
      setBusy(false);
    }
  }

  function toggleSubject(subjectId: string) {
    setExpandedSubjects((current) =>
      current.includes(subjectId)
        ? current.filter((id) => id !== subjectId)
        : [...current, subjectId]
    );
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
        <p>Loading curriculum...</p>
      </div>
    );
  }

  const selectedSubject = tree?.subjects.find((subject) => subject.id === unitSubjectId);
  const selectedUnit = tree?.subjects
    .flatMap((subject) => subject.units.map((unit) => ({ ...unit, subjectId: subject.id })))
    .find((unit) => unit.id === topicUnitId);
  const selectedObjectiveUnit = tree?.subjects
    .flatMap((subject) => subject.units.map((unit) => ({ ...unit, subjectId: subject.id })))
    .find((unit) => unit.id === objectiveUnitId);
  const selectedObjective = tree?.subjects
    .flatMap((subject) =>
      subject.units.flatMap((unit) =>
        unit.learningObjectives.map((objective) => ({
          ...objective,
          unitId: unit.id,
          subjectId: subject.id,
        }))
      )
    )
    .find((objective) => objective.id === microSkillObjectiveId);

  return (
    <div className="min-h-screen p-8">
      <div className="max-w-3xl mx-auto space-y-6">
        <div className="flex items-center justify-between">
          <h1 className="text-2xl font-semibold">Curriculum</h1>
          <Link href="/dashboard" className="text-sm underline">
            Dashboard
          </Link>
        </div>

        <p className="text-sm text-black/60">
          Define regional curriculum variants, inherit them into schools, and override
          units or learning objectives. Tree: subject → units → topics, learning
          objectives, and micro-skills.
        </p>

        {error ? <p className="text-sm text-red-600">{error}</p> : null}
        {message ? <p className="text-sm text-green-700">{message}</p> : null}

        <section className="rounded-lg border border-black/10 p-6 space-y-3">
          <h2 className="font-medium">Organisation</h2>
          {organisations.length > 0 ? (
            <select
              className="w-full rounded border border-black/20 px-3 py-2"
              value={organisationId}
              onChange={(event) => setOrganisationId(event.target.value)}
            >
              {organisations.map((organisation) => (
                <option key={organisation.id} value={organisation.id}>
                  {organisation.name} ({organisation.code})
                </option>
              ))}
            </select>
          ) : (
            <input
              className="w-full rounded border border-black/20 px-3 py-2"
              value={organisationId}
              onChange={(event) => setOrganisationId(event.target.value)}
              placeholder="Organisation id from organisation setup"
              required
            />
          )}
        </section>

        <form
          className="rounded-lg border border-black/10 p-6 space-y-3"
          onSubmit={(event) => {
            event.preventDefault();
            if (!organisationId) {
              setError("Choose or enter an organisation id.");
              return;
            }
            if (createAsRegional && !regionCode.trim()) {
              setError("Regional variants require a region code.");
              return;
            }
            void run(async () => {
              const created = await createCurriculum(
                token,
                organisationId,
                curriculumName,
                curriculumVersion,
                createAsRegional
                  ? { regionCode: regionCode.trim(), scope: "Regional" }
                  : { scope: "School" }
              );
              setMessage(
                createAsRegional
                  ? `Created regional variant ${created.name} (${created.regionCode}).`
                  : `Created curriculum ${created.name}.`
              );
              await loadCurricula(token, organisationId);
              setSelectedCurriculumId(created.id);
              await refreshTree(token, created.id);
            });
          }}
        >
          <h2 className="font-medium">Create curriculum / regional variant</h2>
          <input
            className="w-full rounded border border-black/20 px-3 py-2"
            value={curriculumName}
            onChange={(event) => setCurriculumName(event.target.value)}
            placeholder="Curriculum name"
            required
          />
          <input
            className="w-full rounded border border-black/20 px-3 py-2"
            value={curriculumVersion}
            onChange={(event) => setCurriculumVersion(event.target.value)}
            placeholder="Version"
            required
          />
          <label className="flex items-center gap-2 text-sm">
            <input
              type="checkbox"
              checked={createAsRegional}
              onChange={(event) => setCreateAsRegional(event.target.checked)}
            />
            Define as regional / authority variant
          </label>
          {createAsRegional ? (
            <input
              className="w-full rounded border border-black/20 px-3 py-2"
              value={regionCode}
              onChange={(event) => setRegionCode(event.target.value)}
              placeholder="Region code (e.g. NZ-NCEA)"
              required
            />
          ) : null}
          <button
            type="submit"
            disabled={busy || !organisationId}
            className="rounded bg-black text-white px-4 py-2 text-sm disabled:opacity-60"
          >
            {createAsRegional ? "Create regional variant" : "Create curriculum"}
          </button>
        </form>

        <form
          className="rounded-lg border border-black/10 p-6 space-y-3"
          onSubmit={(event) => {
            event.preventDefault();
            if (!organisationId || !parentCurriculumId.trim()) {
              setError("Organisation and regional parent curriculum id are required to inherit.");
              return;
            }
            void run(async () => {
              const inherited = await inheritCurriculum(
                token,
                parentCurriculumId.trim(),
                organisationId
              );
              setMessage(
                `Inherited regional variant into school curriculum ${inherited.name} (${inherited.regionCode}).`
              );
              await loadCurricula(token, organisationId);
              setSelectedCurriculumId(inherited.id);
              await refreshTree(token, inherited.id);
            });
          }}
        >
          <h2 className="font-medium">Inherit regional variant into school</h2>
          <input
            className="w-full rounded border border-black/20 px-3 py-2"
            value={parentCurriculumId}
            onChange={(event) => setParentCurriculumId(event.target.value)}
            placeholder="Regional parent curriculum id"
            required
          />
          <button
            type="submit"
            disabled={busy || !organisationId}
            className="rounded bg-black text-white px-4 py-2 text-sm disabled:opacity-60"
          >
            Inherit into this organisation
          </button>
        </form>

        {curricula.length > 0 ? (
          <section className="rounded-lg border border-black/10 p-6 space-y-3">
            <h2 className="font-medium">Select curriculum</h2>
            <select
              className="w-full rounded border border-black/20 px-3 py-2"
              value={selectedCurriculumId}
              onChange={(event) => {
                const nextId = event.target.value;
                setSelectedCurriculumId(nextId);
                if (token && nextId) {
                  void refreshTree(token, nextId).catch(() => {
                    setError("Unable to load curriculum tree.");
                  });
                }
              }}
            >
              {curricula.map((item) => (
                <option key={item.id} value={item.id}>
                  {item.name} ({item.version}) — {item.scope}
                  {item.regionCode ? ` / ${item.regionCode}` : ""}
                </option>
              ))}
            </select>
            {tree ? (
              <p className="text-sm text-black/60">
                Scope: {tree.scope}
                {tree.regionCode ? ` · Region: ${tree.regionCode}` : ""}
                {tree.parentCurriculumId
                  ? ` · Inherited from ${tree.parentCurriculumId}`
                  : ""}
              </p>
            ) : null}
          </section>
        ) : null}

        <section className="rounded-lg border border-black/10 p-6 space-y-3">
          <h2 className="font-medium">Curriculum tree</h2>
          {!tree ? (
            <p className="text-sm text-black/60">
              Create or select a curriculum to browse subjects and units.
            </p>
          ) : tree.subjects.length === 0 ? (
            <p className="text-sm text-black/60">No subjects yet.</p>
          ) : (
            <ul className="space-y-2">
              {tree.subjects.map((subject) => {
                const expanded = expandedSubjects.includes(subject.id);
                return (
                  <li key={subject.id}>
                    <button
                      type="button"
                      className="text-left font-medium underline"
                      onClick={() => toggleSubject(subject.id)}
                    >
                      {expanded ? "▾" : "▸"} {subject.name} ({subject.code})
                    </button>
                    {expanded ? (
                      <ul className="ml-6 mt-1 list-disc space-y-1">
                        {subject.units.length === 0 ? (
                          <li className="text-sm text-black/60">No units</li>
                        ) : (
                          subject.units.map((unit) => (
                            <li key={unit.id}>
                              {unit.name}
                              {unit.isOverridden ? (
                                <span className="ml-2 text-xs text-amber-700">(overridden)</span>
                              ) : null}
                              {unit.topics.length > 0 ? (
                                <ul className="ml-5 list-[circle]">
                                  {unit.topics.map((topic) => (
                                    <li key={topic.id}>{topic.name}</li>
                                  ))}
                                </ul>
                              ) : null}
                              {unit.learningObjectives.length > 0 ? (
                                <ul className="ml-5 mt-1 space-y-1">
                                  {unit.learningObjectives.map((objective) => (
                                    <li key={objective.id}>
                                      <span className="font-medium">LO:</span> {objective.title}
                                      {objective.isOverridden ? (
                                        <span className="ml-2 text-xs text-amber-700">
                                          (overridden)
                                        </span>
                                      ) : null}
                                      {objective.microSkills.length > 0 ? (
                                        <ul className="ml-5 list-[square]">
                                          {objective.microSkills.map((skill) => (
                                            <li key={skill.id}>{skill.name}</li>
                                          ))}
                                        </ul>
                                      ) : null}
                                    </li>
                                  ))}
                                </ul>
                              ) : null}
                            </li>
                          ))
                        )}
                      </ul>
                    ) : null}
                  </li>
                );
              })}
            </ul>
          )}
        </section>

        {tree?.parentCurriculumId && selectedUnit ? (
          <form
            className="rounded-lg border border-black/10 p-6 space-y-3"
            onSubmit={(event) => {
              event.preventDefault();
              if (!selectedCurriculumId || !overrideUnitName.trim()) {
                setError("Enter an overridden unit name.");
                return;
              }
              void run(async () => {
                await updateUnit(
                  token,
                  selectedCurriculumId,
                  selectedUnit.subjectId,
                  selectedUnit.id,
                  overrideUnitName.trim(),
                  selectedUnit.sortOrder
                );
                setMessage(`Overrode unit ${selectedUnit.id}.`);
                await refreshTree(token, selectedCurriculumId);
              });
            }}
          >
            <h2 className="font-medium">Override unit (school variant)</h2>
            <p className="text-sm text-black/60">
              Selected unit: {selectedUnit.name}
              {selectedUnit.isOverridden ? " (already overridden)" : ""}
            </p>
            <input
              className="w-full rounded border border-black/20 px-3 py-2"
              value={overrideUnitName}
              onChange={(event) => setOverrideUnitName(event.target.value)}
              placeholder="School-specific unit name"
              required
            />
            <button
              type="submit"
              disabled={busy || !selectedCurriculumId}
              className="rounded bg-black text-white px-4 py-2 text-sm disabled:opacity-60"
            >
              Override unit
            </button>
          </form>
        ) : null}

        {tree?.parentCurriculumId && selectedObjectiveUnit && selectedObjective ? (
          <form
            className="rounded-lg border border-black/10 p-6 space-y-3"
            onSubmit={(event) => {
              event.preventDefault();
              if (!selectedCurriculumId || !overrideObjectiveTitle.trim()) {
                setError("Enter an overridden learning objective title.");
                return;
              }
              void run(async () => {
                await updateLearningObjective(
                  token,
                  selectedCurriculumId,
                  selectedObjective.subjectId,
                  selectedObjective.unitId,
                  selectedObjective.id,
                  overrideObjectiveTitle.trim(),
                  selectedObjective.sortOrder
                );
                setMessage(`Overrode learning objective ${selectedObjective.id}.`);
                await refreshTree(token, selectedCurriculumId);
              });
            }}
          >
            <h2 className="font-medium">Override learning objective (school variant)</h2>
            <p className="text-sm text-black/60">
              Selected LO: {selectedObjective.title}
              {selectedObjective.isOverridden ? " (already overridden)" : ""}
            </p>
            <input
              className="w-full rounded border border-black/20 px-3 py-2"
              value={overrideObjectiveTitle}
              onChange={(event) => setOverrideObjectiveTitle(event.target.value)}
              placeholder="School-specific learning objective"
              required
            />
            <button
              type="submit"
              disabled={busy || !selectedCurriculumId}
              className="rounded bg-black text-white px-4 py-2 text-sm disabled:opacity-60"
            >
              Override learning objective
            </button>
          </form>
        ) : null}

        <form
          className="rounded-lg border border-black/10 p-6 space-y-3"
          onSubmit={(event) => {
            event.preventDefault();
            if (!selectedCurriculumId) {
              setError("Create a curriculum first.");
              return;
            }
            void run(async () => {
              const created = await createSubject(
                token,
                selectedCurriculumId,
                subjectName,
                subjectCode,
                (tree?.subjects.length ?? 0) + 1
              );
              setUnitSubjectId(created.id);
              setMessage(`Added subject ${created.name}.`);
              await refreshTree(token, selectedCurriculumId);
            });
          }}
        >
          <h2 className="font-medium">Add subject</h2>
          <input
            className="w-full rounded border border-black/20 px-3 py-2"
            value={subjectName}
            onChange={(event) => setSubjectName(event.target.value)}
            placeholder="Subject name"
            required
          />
          <input
            className="w-full rounded border border-black/20 px-3 py-2"
            value={subjectCode}
            onChange={(event) => setSubjectCode(event.target.value)}
            placeholder="Subject code"
            required
          />
          <button
            type="submit"
            disabled={busy || !selectedCurriculumId}
            className="rounded bg-black text-white px-4 py-2 text-sm disabled:opacity-60"
          >
            Add subject
          </button>
        </form>

        <form
          className="rounded-lg border border-black/10 p-6 space-y-3"
          onSubmit={(event) => {
            event.preventDefault();
            if (!selectedCurriculumId || !unitSubjectId) {
              setError("Add a subject first.");
              return;
            }
            void run(async () => {
              const created = await createUnit(
                token,
                selectedCurriculumId,
                unitSubjectId,
                unitName,
                (selectedSubject?.units.length ?? 0) + 1
              );
              setTopicUnitId(created.id);
              setObjectiveUnitId(created.id);
              setMessage(`Added unit ${created.name}.`);
              await refreshTree(token, selectedCurriculumId);
            });
          }}
        >
          <h2 className="font-medium">Add unit</h2>
          <select
            className="w-full rounded border border-black/20 px-3 py-2"
            value={unitSubjectId}
            onChange={(event) => setUnitSubjectId(event.target.value)}
            disabled={!tree || tree.subjects.length === 0}
          >
            {(tree?.subjects ?? []).map((subject) => (
              <option key={subject.id} value={subject.id}>
                {subject.name}
              </option>
            ))}
          </select>
          <input
            className="w-full rounded border border-black/20 px-3 py-2"
            value={unitName}
            onChange={(event) => setUnitName(event.target.value)}
            placeholder="Unit name"
            required
          />
          <button
            type="submit"
            disabled={busy || !unitSubjectId}
            className="rounded bg-black text-white px-4 py-2 text-sm disabled:opacity-60"
          >
            Add unit
          </button>
        </form>

        <form
          className="rounded-lg border border-black/10 p-6 space-y-3"
          onSubmit={(event) => {
            event.preventDefault();
            if (!selectedCurriculumId || !selectedUnit) {
              setError("Add a unit first.");
              return;
            }
            void run(async () => {
              const created = await createTopic(
                token,
                selectedCurriculumId,
                selectedUnit.subjectId,
                selectedUnit.id,
                topicName,
                selectedUnit.topics.length + 1
              );
              setMessage(`Added topic ${created.name}.`);
              await refreshTree(token, selectedCurriculumId);
            });
          }}
        >
          <h2 className="font-medium">Add topic</h2>
          <select
            className="w-full rounded border border-black/20 px-3 py-2"
            value={topicUnitId}
            onChange={(event) => setTopicUnitId(event.target.value)}
            disabled={!tree || tree.subjects.every((subject) => subject.units.length === 0)}
          >
            {(tree?.subjects ?? []).flatMap((subject) =>
              subject.units.map((unit) => (
                <option key={unit.id} value={unit.id}>
                  {subject.name} / {unit.name}
                </option>
              ))
            )}
          </select>
          <input
            className="w-full rounded border border-black/20 px-3 py-2"
            value={topicName}
            onChange={(event) => setTopicName(event.target.value)}
            placeholder="Topic name"
            required
          />
          <button
            type="submit"
            disabled={busy || !topicUnitId}
            className="rounded bg-black text-white px-4 py-2 text-sm disabled:opacity-60"
          >
            Add topic
          </button>
        </form>

        <form
          className="rounded-lg border border-black/10 p-6 space-y-3"
          onSubmit={(event) => {
            event.preventDefault();
            if (!selectedCurriculumId || !selectedObjectiveUnit) {
              setError("Add a unit first.");
              return;
            }
            void run(async () => {
              const created = await createLearningObjective(
                token,
                selectedCurriculumId,
                selectedObjectiveUnit.subjectId,
                selectedObjectiveUnit.id,
                objectiveTitle,
                (selectedObjectiveUnit.learningObjectives?.length ?? 0) + 1
              );
              setMicroSkillObjectiveId(created.id);
              setMessage(`Added learning objective ${created.title}.`);
              await refreshTree(token, selectedCurriculumId);
            });
          }}
        >
          <h2 className="font-medium">Add learning objective</h2>
          <select
            className="w-full rounded border border-black/20 px-3 py-2"
            value={objectiveUnitId}
            onChange={(event) => setObjectiveUnitId(event.target.value)}
            disabled={!tree || tree.subjects.every((subject) => subject.units.length === 0)}
          >
            {(tree?.subjects ?? []).flatMap((subject) =>
              subject.units.map((unit) => (
                <option key={unit.id} value={unit.id}>
                  {subject.name} / {unit.name}
                </option>
              ))
            )}
          </select>
          <input
            className="w-full rounded border border-black/20 px-3 py-2"
            value={objectiveTitle}
            onChange={(event) => setObjectiveTitle(event.target.value)}
            placeholder="Learning objective title"
            required
          />
          <button
            type="submit"
            disabled={busy || !objectiveUnitId}
            className="rounded bg-black text-white px-4 py-2 text-sm disabled:opacity-60"
          >
            Add learning objective
          </button>
        </form>

        <form
          className="rounded-lg border border-black/10 p-6 space-y-3"
          onSubmit={(event) => {
            event.preventDefault();
            if (!selectedCurriculumId || !selectedObjective) {
              setError("Add a learning objective first.");
              return;
            }
            void run(async () => {
              const created = await createMicroSkill(
                token,
                selectedCurriculumId,
                selectedObjective.subjectId,
                selectedObjective.unitId,
                selectedObjective.id,
                microSkillName,
                selectedObjective.microSkills.length + 1
              );
              setMessage(`Added micro-skill ${created.name} (id ${created.id}).`);
              await refreshTree(token, selectedCurriculumId);
            });
          }}
        >
          <h2 className="font-medium">Add micro-skill</h2>
          <select
            className="w-full rounded border border-black/20 px-3 py-2"
            value={microSkillObjectiveId}
            onChange={(event) => setMicroSkillObjectiveId(event.target.value)}
            disabled={
              !tree ||
              tree.subjects.every((subject) =>
                subject.units.every((unit) => unit.learningObjectives.length === 0)
              )
            }
          >
            {(tree?.subjects ?? []).flatMap((subject) =>
              subject.units.flatMap((unit) =>
                unit.learningObjectives.map((objective) => (
                  <option key={objective.id} value={objective.id}>
                    {subject.name} / {unit.name} / {objective.title}
                  </option>
                ))
              )
            )}
          </select>
          <input
            className="w-full rounded border border-black/20 px-3 py-2"
            value={microSkillName}
            onChange={(event) => setMicroSkillName(event.target.value)}
            placeholder="Micro-skill name"
            required
          />
          <button
            type="submit"
            disabled={busy || !microSkillObjectiveId}
            className="rounded bg-black text-white px-4 py-2 text-sm disabled:opacity-60"
          >
            Add micro-skill
          </button>
        </form>
      </div>
    </div>
  );
}
