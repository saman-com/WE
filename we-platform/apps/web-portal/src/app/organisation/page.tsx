"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { FocusCard, LearningFrame, PrimaryButton } from "@/components/learning-frame";
import { fetchProfile, listDirectoryUsers, personName, type DirectoryUser, type UserProfile } from "@/lib/auth";
import {
  assignTeacher,
  createClass,
  createYearLevel,
  deleteClass,
  deleteYearLevel,
  enrollStudent,
  linkParentChild,
  listClasses,
  listEnrollments,
  listOrganisations,
  listParentChildren,
  listTeachers,
  listYearLevels,
  unassignTeacher,
  unenrollStudent,
  unlinkParentChild,
  updateClass,
  updateYearLevel,
  type ClassMember,
  type Organisation,
  type ParentChildLink,
  type SchoolClass,
  type YearLevel,
} from "@/lib/organisation";
import { useI18n } from "@/i18n/I18nProvider";

type Tab = "years" | "classes" | "staff" | "students" | "families";

export default function OrganisationSetupPage() {
  const router = useRouter();
  const { t } = useI18n();
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [token, setToken] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [status, setStatus] = useState<string | null>(null);
  const [statusError, setStatusError] = useState(false);
  const [schools, setSchools] = useState<Organisation[]>([]);
  const [directory, setDirectory] = useState<DirectoryUser[]>([]);
  const [organisationId, setOrganisationId] = useState("");
  const [years, setYears] = useState<YearLevel[]>([]);
  const [classes, setClasses] = useState<SchoolClass[]>([]);
  const [tab, setTab] = useState<Tab>("years");
  const [classId, setClassId] = useState("");
  const [people, setPeople] = useState<ClassMember[]>([]);
  const [links, setLinks] = useState<ParentChildLink[]>([]);
  const [busy, setBusy] = useState(false);

  const [yearName, setYearName] = useState("");
  const [yearOrder, setYearOrder] = useState(1);
  const [className, setClassName] = useState("");
  const [classCode, setClassCode] = useState("");
  const [classYearId, setClassYearId] = useState("");
  const [userId, setUserId] = useState("");
  const [parentId, setParentId] = useState("");
  const [studentId, setStudentId] = useState("");
  const [editingYear, setEditingYear] = useState<string | null>(null);
  const [editingClass, setEditingClass] = useState<string | null>(null);

  useEffect(() => {
    const stored = localStorage.getItem("we_access_token");
    if (!stored) {
      router.replace("/login");
      return;
    }
    setToken(stored);
    fetchProfile(stored)
      .then(async (loaded) => {
        if (!loaded.roles.includes("SystemAdministrator")) {
          setError(t("organisation.adminOnly"));
          return;
        }
        setProfile(loaded);
        const existing = await listOrganisations(stored);
        setSchools(existing);
        setDirectory(await listDirectoryUsers(stored).catch(() => []));
        if (existing[0]) {
          setOrganisationId(existing[0].id);
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
    let cancelled = false;
    Promise.all([
      listYearLevels(token, organisationId),
      listClasses(token, organisationId),
    ])
      .then(([loadedYears, loadedClasses]) => {
        if (cancelled) {
          return;
        }
        setYears(loadedYears);
        setClasses(loadedClasses);
        setClassId((current) => current || loadedClasses[0]?.id || "");
        setClassYearId((current) => current || loadedYears[0]?.id || "");
      })
      .catch(() => {
        if (!cancelled) {
          setStatusError(true);
          setStatus(t("organisation.manage.failed"));
        }
      });
    return () => {
      cancelled = true;
    };
  }, [token, organisationId, t]);

  useEffect(() => {
    if (!token || !organisationId || !classId || (tab !== "staff" && tab !== "students")) {
      return;
    }
    const load = tab === "staff" ? listTeachers : listEnrollments;
    load(token, organisationId, classId)
      .then(setPeople)
      .catch(() => {
        setPeople([]);
        setStatusError(true);
        setStatus(t("organisation.manage.failed"));
      });
  }, [token, organisationId, classId, tab, t]);

  async function run(action: () => Promise<void>) {
    if (!token || !organisationId) {
      return;
    }
    setBusy(true);
    setStatus(null);
    setStatusError(false);
    try {
      await action();
      setStatus(t("organisation.manage.saved"));
      const [loadedYears, loadedClasses] = await Promise.all([
        listYearLevels(token, organisationId),
        listClasses(token, organisationId),
      ]);
      setYears(loadedYears);
      setClasses(loadedClasses);
    } catch {
      setStatusError(true);
      setStatus(t("organisation.manage.failed"));
    } finally {
      setBusy(false);
    }
  }

  const firstTime = classes.length === 0;
  const tabs = [
    { id: "years", label: t("organisation.manage.years") },
    { id: "classes", label: t("organisation.manage.classes") },
    { id: "staff", label: t("organisation.manage.staff") },
    { id: "students", label: t("organisation.manage.students") },
    { id: "families", label: t("organisation.manage.families") },
  ];

  if (error && !profile) {
    return (
      <div className="flex min-h-screen items-center justify-center p-6">
        <div className="space-y-4 text-center">
          <p className="text-red-700">{error}</p>
          <Link href="/login" className="underline">
            {t("common.backToLogin")}
          </Link>
        </div>
      </div>
    );
  }

  if (!profile || !token) {
    return (
      <div className="flex min-h-screen items-center justify-center p-6">
        <p>{t("organisation.loading")}</p>
      </div>
    );
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
      tabs={tabs}
      activeTab={tab}
      onTabChange={(id) => setTab(id as Tab)}
    >
      <div className="space-y-6">
        <h1 className="text-2xl font-semibold">{t("dashboard.nav.organisationSetup")}</h1>
        {status ? (
          <p role="status" className={`text-sm ${statusError ? "text-red-700" : "text-green-800"}`}>
            {status}
          </p>
        ) : null}
        <label className="block space-y-1 text-sm">
          <span>{t("organisation.manage.school")}</span>
          <select
            className="w-full rounded-lg border border-black/10 bg-white px-3 py-2"
            value={organisationId}
            onChange={(event) => setOrganisationId(event.target.value)}
          >
            {schools.map((school) => (
              <option key={school.id} value={school.id}>
                {school.name}
              </option>
            ))}
          </select>
        </label>

        {firstTime ? (
          <FocusCard>
            <h2 className="font-medium">{t("organisation.manage.firstTitle")}</h2>
            <form
              className="mt-4 space-y-3"
              onSubmit={(event) => {
                event.preventDefault();
                void run(async () => {
                  const year = await createYearLevel(token, organisationId, yearName, yearOrder);
                  await createClass(token, organisationId, year.id, className, classCode);
                  setYearName("");
                  setClassName("");
                  setClassCode("");
                });
              }}
            >
              <Field label={t("organisation.manage.yearName")} value={yearName} onChange={setYearName} />
              <Field
                label={t("organisation.manage.sortOrder")}
                value={String(yearOrder)}
                onChange={(value) => setYearOrder(Number(value))}
                type="number"
              />
              <Field label={t("organisation.manage.className")} value={className} onChange={setClassName} />
              <Field label={t("organisation.manage.classCode")} value={classCode} onChange={setClassCode} />
              <PrimaryButton type="submit" disabled={busy}>
                {t("organisation.manage.firstAction")}
              </PrimaryButton>
            </form>
          </FocusCard>
        ) : null}

        {tab === "years" ? (
          <FocusCard>
            {years.length === 0 ? (
              <p className="text-sm text-black/60">{t("organisation.manage.noYears")}</p>
            ) : (
              <ul className="space-y-3 text-sm">
                {years.map((year) => (
                  <li key={year.id} className="flex flex-wrap items-center justify-between gap-2">
                    {editingYear === year.id ? (
                      <form
                        className="flex flex-1 flex-wrap gap-2"
                        onSubmit={(event) => {
                          event.preventDefault();
                          void run(async () => {
                            await updateYearLevel(token, organisationId, year.id, yearName, yearOrder);
                            setEditingYear(null);
                          });
                        }}
                      >
                        <Field label={t("organisation.manage.yearName")} value={yearName} onChange={setYearName} />
                        <Field
                          label={t("organisation.manage.sortOrder")}
                          value={String(yearOrder)}
                          onChange={(value) => setYearOrder(Number(value))}
                          type="number"
                        />
                        <TextButton type="submit">{t("organisation.manage.save")}</TextButton>
                      </form>
                    ) : (
                      <span>
                        {year.name}
                      </span>
                    )}
                    <span className="flex gap-3">
                      <TextButton
                        onClick={() => {
                          setEditingYear(year.id);
                          setYearName(year.name);
                          setYearOrder(year.sortOrder);
                        }}
                      >
                        {t("organisation.manage.edit")}
                      </TextButton>
                      <TextButton
                        onClick={() => {
                          void run(() => deleteYearLevel(token, organisationId, year.id));
                        }}
                      >
                        {t("organisation.manage.remove")}
                      </TextButton>
                    </span>
                  </li>
                ))}
              </ul>
            )}
            {!firstTime ? (
              <form
                className="mt-4 space-y-3"
                onSubmit={(event) => {
                  event.preventDefault();
                  void run(async () => {
                    await createYearLevel(token, organisationId, yearName, yearOrder);
                    setYearName("");
                  });
                }}
              >
                <Field label={t("organisation.manage.yearName")} value={yearName} onChange={setYearName} />
                <Field
                  label={t("organisation.manage.sortOrder")}
                  value={String(yearOrder)}
                  onChange={(value) => setYearOrder(Number(value))}
                  type="number"
                />
                <PrimaryButton type="submit" disabled={busy}>
                  {t("organisation.manage.addYear")}
                </PrimaryButton>
              </form>
            ) : null}
          </FocusCard>
        ) : null}

        {tab === "classes" ? (
          <FocusCard>
            {classes.length === 0 ? (
              <p className="text-sm text-black/60">{t("organisation.manage.noClasses")}</p>
            ) : (
              <ul className="space-y-3 text-sm">
                {classes.map((schoolClass) => (
                  <li key={schoolClass.id} className="space-y-2">
                    {editingClass === schoolClass.id ? (
                      <form
                        className="space-y-3"
                        onSubmit={(event) => {
                          event.preventDefault();
                          void run(async () => {
                            await updateClass(
                              token,
                              organisationId,
                              schoolClass.id,
                              className,
                              classCode,
                              classYearId || schoolClass.yearLevelId
                            );
                            setEditingClass(null);
                          });
                        }}
                      >
                        <Field label={t("organisation.manage.className")} value={className} onChange={setClassName} />
                        <Field label={t("organisation.manage.classCode")} value={classCode} onChange={setClassCode} />
                        <TextButton type="submit">{t("organisation.manage.save")}</TextButton>
                      </form>
                    ) : (
                      <div className="flex flex-wrap items-center justify-between gap-2">
                        <span>
                          {schoolClass.name} ({schoolClass.code})
                        </span>
                        <span className="flex gap-3">
                          <TextButton
                            onClick={() => {
                              setEditingClass(schoolClass.id);
                              setClassName(schoolClass.name);
                              setClassCode(schoolClass.code);
                              setClassYearId(schoolClass.yearLevelId);
                            }}
                          >
                            {t("organisation.manage.edit")}
                          </TextButton>
                          <TextButton
                            onClick={() => {
                              void run(() => deleteClass(token, organisationId, schoolClass.id));
                            }}
                          >
                            {t("organisation.manage.remove")}
                          </TextButton>
                        </span>
                      </div>
                    )}
                  </li>
                ))}
              </ul>
            )}
            {!firstTime ? (
              <form
                className="mt-4 space-y-3"
                onSubmit={(event) => {
                  event.preventDefault();
                  void run(async () => {
                    await createClass(token, organisationId, classYearId, className, classCode);
                    setClassName("");
                    setClassCode("");
                  });
                }}
              >
                <label className="block space-y-1 text-sm">
                  <span>{t("organisation.manage.yearName")}</span>
                  <select
                    className="w-full rounded-lg border border-black/10 bg-white px-3 py-2"
                    value={classYearId}
                    onChange={(event) => setClassYearId(event.target.value)}
                  >
                    {years.map((year) => (
                      <option key={year.id} value={year.id}>
                        {year.name}
                      </option>
                    ))}
                  </select>
                </label>
                <Field label={t("organisation.manage.className")} value={className} onChange={setClassName} />
                <Field label={t("organisation.manage.classCode")} value={classCode} onChange={setClassCode} />
                <PrimaryButton type="submit" disabled={busy}>
                  {t("organisation.manage.addClass")}
                </PrimaryButton>
              </form>
            ) : null}
          </FocusCard>
        ) : null}

        {tab === "staff" || tab === "students" ? (
          <FocusCard>
            <ClassPicker
              label={t("organisation.manage.classLabel")}
              classes={classes}
              classId={classId}
              onChange={setClassId}
            />
            {people.length === 0 ? (
              <p className="mt-3 text-sm text-black/60">{t("organisation.manage.noPeople")}</p>
            ) : (
              <ul className="mt-3 space-y-2 text-sm">
                {people.map((person) => (
                  <li key={person.userId} className="flex items-center justify-between gap-2">
                    <span>{personName(directory, person.userId, t("organisation.manage.unknownPerson"))}</span>
                    <TextButton
                      onClick={() => {
                        void run(async () => {
                          if (tab === "staff") {
                            await unassignTeacher(token, organisationId, classId, person.userId);
                          } else {
                            await unenrollStudent(token, organisationId, classId, person.userId);
                          }
                          const load = tab === "staff" ? listTeachers : listEnrollments;
                          setPeople(await load(token, organisationId, classId));
                        });
                      }}
                    >
                      {t("organisation.manage.remove")}
                    </TextButton>
                  </li>
                ))}
              </ul>
            )}
            <form
              className="mt-4 space-y-3"
              onSubmit={(event) => {
                event.preventDefault();
                void run(async () => {
                  if (tab === "staff") {
                    await assignTeacher(token, organisationId, classId, userId);
                    setPeople(await listTeachers(token, organisationId, classId));
                  } else {
                    await enrollStudent(token, organisationId, classId, userId);
                    setPeople(await listEnrollments(token, organisationId, classId));
                  }
                  setUserId("");
                });
              }}
            >
              <PersonSelect
                label={t("organisation.manage.userId")}
                value={userId}
                onChange={setUserId}
                people={directory.filter((person) =>
                  person.roles.includes(tab === "staff" ? "Teacher" : "Student")
                )}
              />
              <PrimaryButton type="submit" disabled={busy || !classId}>
                {t("organisation.manage.addPerson")}
              </PrimaryButton>
            </form>
          </FocusCard>
        ) : null}

        {tab === "families" ? (
          <FocusCard>
            <form
              className="space-y-3"
              onSubmit={(event) => {
                event.preventDefault();
                void run(async () => {
                  await linkParentChild(token, parentId, studentId);
                  setLinks(await listParentChildren(token, parentId));
                  setStudentId("");
                });
              }}
            >
              <PersonSelect
                label={t("organisation.manage.parentId")}
                value={parentId}
                onChange={setParentId}
                people={directory.filter((person) => person.roles.includes("Parent"))}
              />
              <PersonSelect
                label={t("organisation.manage.studentId")}
                value={studentId}
                onChange={setStudentId}
                people={directory.filter((person) => person.roles.includes("Student"))}
              />
              <PrimaryButton type="submit" disabled={busy}>
                {t("organisation.manage.addPerson")}
              </PrimaryButton>
            </form>
            <TextButton
              onClick={() => {
                if (!parentId) {
                  return;
                }
                void listParentChildren(token, parentId)
                  .then(setLinks)
                  .catch(() => {
                    setStatusError(true);
                    setStatus(t("organisation.manage.failed"));
                  });
              }}
            >
              {t("organisation.manage.lookup")}
            </TextButton>
            {links.length === 0 ? (
              <p className="mt-3 text-sm text-black/60">{t("organisation.manage.noLinks")}</p>
            ) : (
              <ul className="mt-3 space-y-2 text-sm">
                {links.map((link) => (
                  <li key={link.studentUserId} className="flex items-center justify-between gap-2">
                    <span>
                      {personName(directory, link.studentUserId, t("organisation.manage.unknownPerson"))}
                    </span>
                    <TextButton
                      onClick={() => {
                        void run(async () => {
                          await unlinkParentChild(token, link.parentUserId, link.studentUserId);
                          setLinks(await listParentChildren(token, link.parentUserId));
                        });
                      }}
                    >
                      {t("organisation.manage.remove")}
                    </TextButton>
                  </li>
                ))}
              </ul>
            )}
          </FocusCard>
        ) : null}
      </div>
    </LearningFrame>
  );
}

function Field({
  label,
  value,
  onChange,
  type = "text",
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  type?: string;
}) {
  return (
    <label className="block min-w-40 flex-1 space-y-1 text-sm">
      <span>{label}</span>
      <input
        type={type}
        className="w-full rounded-lg border border-black/10 bg-white px-3 py-2"
        value={value}
        onChange={(event) => onChange(event.target.value)}
        required
      />
    </label>
  );
}

function TextButton({
  children,
  onClick,
  type = "button",
}: {
  children: React.ReactNode;
  onClick?: () => void;
  type?: "button" | "submit";
}) {
  return (
    <button type={type} onClick={onClick} className="text-sm text-black/60 underline-offset-2 hover:underline">
      {children}
    </button>
  );
}

function PersonSelect({
  label,
  value,
  onChange,
  people,
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  people: DirectoryUser[];
}) {
  return (
    <label className="block space-y-1 text-sm">
      <span>{label}</span>
      <select
        className="w-full rounded-lg border border-black/10 bg-white px-3 py-2"
        value={value}
        onChange={(event) => onChange(event.target.value)}
        required
      >
        <option value="">{label}</option>
        {people.map((person) => (
          <option key={person.id} value={person.id}>
            {person.name}
          </option>
        ))}
      </select>
    </label>
  );
}

function ClassPicker({
  label,
  classes,
  classId,
  onChange,
}: {
  label: string;
  classes: SchoolClass[];
  classId: string;
  onChange: (id: string) => void;
}) {
  return (
    <label className="block space-y-1 text-sm">
      <span>{label}</span>
      <select
        className="w-full rounded-lg border border-black/10 bg-white px-3 py-2"
        value={classId}
        onChange={(event) => onChange(event.target.value)}
      >
        {classes.map((schoolClass) => (
          <option key={schoolClass.id} value={schoolClass.id}>
            {schoolClass.name}
          </option>
        ))}
      </select>
    </label>
  );
}
