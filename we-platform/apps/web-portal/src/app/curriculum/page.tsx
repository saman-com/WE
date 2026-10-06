"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { DataText } from "@/components/data-text";
import { FocusCard, LearningFrame, PrimaryButton } from "@/components/learning-frame";
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
import { useI18n } from "@/i18n/I18nProvider";

function canManageCurriculum(profile: UserProfile): boolean {
  return (
    profile.roles.includes("Teacher") ||
    profile.roles.includes("SystemAdministrator")
  );
}

export default function CurriculumPage() {
  const router = useRouter();
  const { t } = useI18n();
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
  const [tab, setTab] = useState("tree");
  const [addKind, setAddKind] = useState("subject");
  const [overrideKind, setOverrideKind] = useState("unit");

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
          setError(t("curriculum.errorNoPermission"));
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
  }, [router, t]);

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
          setError(t("curriculum.loadCurriculaError"));
        }
      });

    return () => {
      cancelled = true;
    };
  }, [token, organisationId, profile, t]);

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
      setError(t("organisation.requestFailedCheckForm"));
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
            {t("common.backToDashboard")}
          </Link>
        </div>
      </div>
    );
  }

  if (!profile || !token) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6">
        <p>{t("curriculum.loading")}</p>
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

  const tabs = [
    { id: "tree", label: t("curriculum.tab.tree") },
    { id: "add", label: t("curriculum.tab.add") },
    { id: "inherit", label: t("curriculum.tab.inherit") },
    ...(tree?.parentCurriculumId
      ? [{ id: "override", label: t("curriculum.tab.override") }]
      : []),
  ];

  return (
    <LearningFrame
      eyebrow="WE"
      title={profile.name}
      onSignOut={() => {
        localStorage.removeItem("we_access_token");
        router.push("/login");
      }}
      signOutLabel={t("common.signOut")}
      tabs={tabs}
      activeTab={tab}
      onTabChange={setTab}
    >
      <div className="space-y-6">
        <h1 className="text-2xl font-semibold">{t("dashboard.nav.curriculum")}</h1>
        {error || message ? (
          <p role="status" className={`text-sm ${error ? "text-red-700" : "text-green-800"}`}>
            {error ?? message}
          </p>
        ) : null}

        {tab === "tree" ? (
          <FocusCard>
            <label className="block space-y-1 text-sm">
              <span>{t("curriculum.label.school")}</span>
              {organisations.length > 0 ? (
                <select
                  className="w-full rounded-lg border border-black/10 bg-white px-3 py-2"
                  value={organisationId}
                  onChange={(event) => setOrganisationId(event.target.value)}
                >
                  {organisations.map((organisation) => (
                    <option key={organisation.id} value={organisation.id}>
                      <DataText>{organisation.name}</DataText>
                    </option>
                  ))}
                </select>
              ) : (
                <input
                  className="w-full rounded-lg border border-black/10 bg-white px-3 py-2"
                  value={organisationId}
                  onChange={(event) => setOrganisationId(event.target.value)}
                  required
                />
              )}
            </label>
            {curricula.length > 0 ? (
              <label className="mt-3 block space-y-1 text-sm">
                <span>{t("curriculum.label.curriculum")}</span>
                <select
                  className="w-full rounded-lg border border-black/10 bg-white px-3 py-2"
                  value={selectedCurriculumId}
                  onChange={(event) => {
                    const nextId = event.target.value;
                    setSelectedCurriculumId(nextId);
                    if (token && nextId) {
                      void refreshTree(token, nextId).catch(() => {
                        setError(t("curriculum.loadTreeError"));
                      });
                    }
                  }}
                >
                  {curricula.map((item) => (
                    <option key={item.id} value={item.id}>
                      <DataText>{item.name}</DataText>
                    </option>
                  ))}
                </select>
              </label>
            ) : null}
            <h2 className="mt-4 font-medium">{tree?.name ?? t("curriculum.treeTitle")}</h2>
            {!tree ? (
              <p className="mt-2 text-sm text-black/60">{t("curriculum.treeEmpty")}</p>
            ) : tree.subjects.length === 0 ? (
              <p className="mt-2 text-sm text-black/60">{t("curriculum.noSubjects")}</p>
            ) : (
              <ul className="mt-3 space-y-2">
                {tree.subjects.map((subject) => {
                  const expanded = expandedSubjects.includes(subject.id);
                  return (
                    <li key={subject.id}>
                      <button
                        type="button"
                        className="text-start font-medium underline"
                        onClick={() => toggleSubject(subject.id)}
                      >
                        {expanded ? "▾" : "▸"}{" "}
                        <DataText>{`${subject.name} (${subject.code})`}</DataText>
                      </button>
                      {expanded ? (
                        <ul className="ms-6 mt-1 list-disc space-y-1">
                          {subject.units.length === 0 ? (
                            <li className="text-sm text-black/60">{t("curriculum.noUnits")}</li>
                          ) : (
                            subject.units.map((unit) => (
                              <li key={unit.id}>
                                <DataText>{unit.name}</DataText>
                                {unit.isOverridden ? (
                                  <span className="ms-2 text-xs text-amber-700">
                                    {t("curriculum.overridden")}
                                  </span>
                                ) : null}
                                {unit.topics.length > 0 ? (
                                  <ul className="ms-5 list-[circle]">
                                    {unit.topics.map((topic) => (
                                      <li key={topic.id}>
                                        <DataText>{topic.name}</DataText>
                                      </li>
                                    ))}
                                  </ul>
                                ) : null}
                                {unit.learningObjectives.length > 0 ? (
                                  <ul className="ms-5 mt-1 space-y-1">
                                    {unit.learningObjectives.map((objective) => (
                                      <li key={objective.id}>
                                        <span className="font-medium">{t("curriculum.loLabel")}</span>{" "}
                                        <DataText>{objective.title}</DataText>
                                        {objective.microSkills.length > 0 ? (
                                          <ul className="ms-5 list-[square]">
                                            {objective.microSkills.map((skill) => (
                                              <li key={skill.id}>
                                                <DataText>{skill.name}</DataText>
                                              </li>
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
          </FocusCard>
        ) : null}

        {tab === "add" ? (
          <FocusCard>
            <form
              className="space-y-3"
              onSubmit={(event) => {
                event.preventDefault();
                if (addKind === "curriculum") {
                  if (!organisationId) {
                    setError(t("curriculum.chooseOrganisation"));
                    return;
                  }
                  if (createAsRegional && !regionCode.trim()) {
                    setError(t("curriculum.regionCodeRequired"));
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
                        ? t("curriculum.createdRegional", {
                            name: created.name,
                            region: created.regionCode ?? "",
                          })
                        : t("curriculum.createdCurriculum", { name: created.name })
                    );
                    await loadCurricula(token, organisationId);
                    setSelectedCurriculumId(created.id);
                    await refreshTree(token, created.id);
                  });
                  return;
                }
                if (!selectedCurriculumId) {
                  setError(t("curriculum.createCurriculumFirst"));
                  return;
                }
                if (addKind === "subject") {
                  void run(async () => {
                    const created = await createSubject(
                      token,
                      selectedCurriculumId,
                      subjectName,
                      subjectCode,
                      (tree?.subjects.length ?? 0) + 1
                    );
                    setUnitSubjectId(created.id);
                    setMessage(t("curriculum.addedSubject", { name: created.name }));
                    await refreshTree(token, selectedCurriculumId);
                  });
                  return;
                }
                if (addKind === "unit") {
                  if (!unitSubjectId) {
                    setError(t("curriculum.addSubjectFirst"));
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
                    setMessage(t("curriculum.addedUnit", { name: created.name }));
                    await refreshTree(token, selectedCurriculumId);
                  });
                  return;
                }
                if (addKind === "topic") {
                  if (!selectedUnit) {
                    setError(t("curriculum.addUnitFirst"));
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
                    setMessage(t("curriculum.addedTopic", { name: created.name }));
                    await refreshTree(token, selectedCurriculumId);
                  });
                  return;
                }
                if (addKind === "objective") {
                  if (!selectedObjectiveUnit) {
                    setError(t("curriculum.addUnitFirst"));
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
                    setMessage(t("curriculum.addedObjective", { title: created.title }));
                    await refreshTree(token, selectedCurriculumId);
                  });
                  return;
                }
                if (!selectedObjective) {
                  setError(t("curriculum.addObjectiveFirst"));
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
                  setMessage(
                    t("curriculum.addedMicroSkill", { name: created.name, id: created.id })
                  );
                  await refreshTree(token, selectedCurriculumId);
                });
              }}
            >
              <label className="block space-y-1 text-sm">
                <span>{t("curriculum.label.kind")}</span>
                <select
                  className="w-full rounded-lg border border-black/10 bg-white px-3 py-2"
                  value={addKind}
                  onChange={(event) => setAddKind(event.target.value)}
                >
                  <option value="curriculum">{t("curriculum.kind.curriculum")}</option>
                  <option value="subject">{t("curriculum.kind.subject")}</option>
                  <option value="unit">{t("curriculum.kind.unit")}</option>
                  <option value="topic">{t("curriculum.kind.topic")}</option>
                  <option value="objective">{t("curriculum.kind.objective")}</option>
                  <option value="skill">{t("curriculum.kind.skill")}</option>
                </select>
              </label>
              {addKind === "curriculum" ? (
                <>
                  <Labeled value={curriculumName} onChange={setCurriculumName} label={t("curriculum.label.name")} />
                  <Labeled value={curriculumVersion} onChange={setCurriculumVersion} label={t("curriculum.label.version")} />
                  <label className="flex items-center gap-2 text-sm">
                    <input
                      type="checkbox"
                      checked={createAsRegional}
                      onChange={(event) => setCreateAsRegional(event.target.checked)}
                    />
                    {t("curriculum.defineRegional")}
                  </label>
                  {createAsRegional ? (
                    <Labeled value={regionCode} onChange={setRegionCode} label={t("curriculum.label.region")} />
                  ) : null}
                </>
              ) : null}
              {addKind === "subject" ? (
                <>
                  <Labeled value={subjectName} onChange={setSubjectName} label={t("curriculum.label.subject")} />
                  <Labeled value={subjectCode} onChange={setSubjectCode} label={t("curriculum.label.code")} />
                </>
              ) : null}
              {addKind === "unit" ? (
                <>
                  <NodeSelect
                    label={t("curriculum.label.subject")}
                    value={unitSubjectId}
                    onChange={setUnitSubjectId}
                    options={(tree?.subjects ?? []).map((subject) => ({
                      id: subject.id,
                      label: subject.name,
                    }))}
                  />
                  <Labeled value={unitName} onChange={setUnitName} label={t("curriculum.label.unit")} />
                </>
              ) : null}
              {addKind === "topic" ? (
                <>
                  <NodeSelect
                    label={t("curriculum.label.unit")}
                    value={topicUnitId}
                    onChange={setTopicUnitId}
                    options={(tree?.subjects ?? []).flatMap((subject) =>
                      subject.units.map((unit) => ({
                        id: unit.id,
                        label: `${subject.name} / ${unit.name}`,
                      }))
                    )}
                  />
                  <Labeled value={topicName} onChange={setTopicName} label={t("curriculum.label.topic")} />
                </>
              ) : null}
              {addKind === "objective" ? (
                <>
                  <NodeSelect
                    label={t("curriculum.label.unit")}
                    value={objectiveUnitId}
                    onChange={setObjectiveUnitId}
                    options={(tree?.subjects ?? []).flatMap((subject) =>
                      subject.units.map((unit) => ({
                        id: unit.id,
                        label: `${subject.name} / ${unit.name}`,
                      }))
                    )}
                  />
                  <Labeled
                    value={objectiveTitle}
                    onChange={setObjectiveTitle}
                    label={t("curriculum.label.objective")}
                  />
                </>
              ) : null}
              {addKind === "skill" ? (
                <>
                  <NodeSelect
                    label={t("curriculum.label.objective")}
                    value={microSkillObjectiveId}
                    onChange={setMicroSkillObjectiveId}
                    options={(tree?.subjects ?? []).flatMap((subject) =>
                      subject.units.flatMap((unit) =>
                        unit.learningObjectives.map((objective) => ({
                          id: objective.id,
                          label: objective.title,
                        }))
                      )
                    )}
                  />
                  <Labeled value={microSkillName} onChange={setMicroSkillName} label={t("curriculum.label.skill")} />
                </>
              ) : null}
              <PrimaryButton type="submit" disabled={busy}>
                {t("curriculum.tab.add")}
              </PrimaryButton>
            </form>
          </FocusCard>
        ) : null}

        {tab === "inherit" ? (
          <FocusCard>
            <form
              className="space-y-3"
              onSubmit={(event) => {
                event.preventDefault();
                if (!organisationId || !parentCurriculumId.trim()) {
                  setError(t("curriculum.inheritRequired"));
                  return;
                }
                void run(async () => {
                  const inherited = await inheritCurriculum(
                    token,
                    parentCurriculumId.trim(),
                    organisationId
                  );
                  setMessage(
                    t("curriculum.inherited", {
                      name: inherited.name,
                      region: inherited.regionCode ?? "",
                    })
                  );
                  await loadCurricula(token, organisationId);
                  setSelectedCurriculumId(inherited.id);
                  await refreshTree(token, inherited.id);
                });
              }}
            >
              <label className="block space-y-1 text-sm">
                <span>{t("curriculum.label.parent")}</span>
                <select
                  className="w-full rounded-lg border border-black/10 bg-white px-3 py-2"
                  value={parentCurriculumId}
                  onChange={(event) => setParentCurriculumId(event.target.value)}
                  required
                >
                  <option value="">{t("curriculum.label.parent")}</option>
                  {curricula.map((item) => (
                    <option key={item.id} value={item.id}>
                      <DataText>{item.name}</DataText>
                    </option>
                  ))}
                </select>
              </label>
              <PrimaryButton type="submit" disabled={busy || !organisationId}>
                {t("curriculum.inheritButton")}
              </PrimaryButton>
            </form>
          </FocusCard>
        ) : null}

        {tab === "override" && tree?.parentCurriculumId ? (
          <FocusCard>
            <form
              className="space-y-3"
              onSubmit={(event) => {
                event.preventDefault();
                if (overrideKind === "unit") {
                  if (!selectedCurriculumId || !selectedUnit || !overrideUnitName.trim()) {
                    setError(t("curriculum.overrideUnitNameRequired"));
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
                    setMessage(t("curriculum.overrodeUnit", { id: selectedUnit.id }));
                    await refreshTree(token, selectedCurriculumId);
                  });
                  return;
                }
                if (!selectedCurriculumId || !selectedObjective || !overrideObjectiveTitle.trim()) {
                  setError(t("curriculum.overrideObjectiveRequired"));
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
                  setMessage(t("curriculum.overrodeObjective", { id: selectedObjective.id }));
                  await refreshTree(token, selectedCurriculumId);
                });
              }}
            >
              <label className="block space-y-1 text-sm">
                <span>{t("curriculum.override.kind")}</span>
                <select
                  className="w-full rounded-lg border border-black/10 bg-white px-3 py-2"
                  value={overrideKind}
                  onChange={(event) => setOverrideKind(event.target.value)}
                >
                  <option value="unit">{t("curriculum.override.unit")}</option>
                  <option value="objective">{t("curriculum.override.objective")}</option>
                </select>
              </label>
              {overrideKind === "unit" ? (
                <Labeled
                  value={overrideUnitName}
                  onChange={setOverrideUnitName}
                  label={t("curriculum.label.unit")}
                />
              ) : (
                <Labeled
                  value={overrideObjectiveTitle}
                  onChange={setOverrideObjectiveTitle}
                  label={t("curriculum.label.objective")}
                />
              )}
              <PrimaryButton type="submit" disabled={busy}>
                {t("curriculum.tab.override")}
              </PrimaryButton>
            </form>
          </FocusCard>
        ) : null}
      </div>
    </LearningFrame>
  );
}

function Labeled({
  label,
  value,
  onChange,
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
}) {
  return (
    <label className="block space-y-1 text-sm">
      <span>{label}</span>
      <input
        className="w-full rounded-lg border border-black/10 bg-white px-3 py-2"
        value={value}
        onChange={(event) => onChange(event.target.value)}
        required
      />
    </label>
  );
}

function NodeSelect({
  label,
  value,
  onChange,
  options,
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  options: { id: string; label: string }[];
}) {
  return (
    <label className="block space-y-1 text-sm">
      <span>{label}</span>
      <select
        className="w-full rounded-lg border border-black/10 bg-white px-3 py-2"
        value={value}
        onChange={(event) => onChange(event.target.value)}
      >
        {options.map((option) => (
          <option key={option.id} value={option.id}>
            {option.label}
          </option>
        ))}
      </select>
    </label>
  );
}

