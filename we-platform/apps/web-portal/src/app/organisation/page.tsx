"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { DataText } from "@/components/data-text";
import { FocusCard, LearningFrame, PrimaryButton } from "@/components/learning-frame";
import { ApiError } from "@/lib/api-error";
import {
  createDirectoryUser,
  deactivateDirectoryUser,
  fetchProfile,
  listDirectoryUsers,
  personName,
  reactivateDirectoryUser,
  resetDirectoryUserPassword,
  updateDirectoryUser,
  type DirectoryUser,
  type UserProfile,
} from "@/lib/auth";
import { roleHome } from "@/lib/role-home";
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

const accountRoles = [
  "Student",
  "Parent",
  "Teacher",
  "SchoolLeader",
  "EducationAuthorityOfficer",
  "FederationAdmin",
  "SystemAdministrator",
] as const;

export default function OrganisationSetupPage() {
  const router = useRouter();
  const { t, translateError } = useI18n();
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [token, setToken] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [homeHref, setHomeHref] = useState<string | null>(null);
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
  const [yearOrderOverride, setYearOrderOverride] = useState<number | null>(null);
  const yearOrder =
    yearOrderOverride ??
    years.reduce((highest, year) => Math.max(highest, year.sortOrder), 0) + 1;
  const [className, setClassName] = useState("");
  const [classCode, setClassCode] = useState("");
  const [classYearId, setClassYearId] = useState("");
  const [userId, setUserId] = useState("");
  const [parentId, setParentId] = useState("");
  const [studentId, setStudentId] = useState("");
  const [editingYear, setEditingYear] = useState<string | null>(null);
  const [editYearName, setEditYearName] = useState("");
  const [editYearOrder, setEditYearOrder] = useState(1);
  const [editingClass, setEditingClass] = useState<string | null>(null);
  const [editClassName, setEditClassName] = useState("");
  const [editClassCode, setEditClassCode] = useState("");
  const [editingPerson, setEditingPerson] = useState<string | null>(null);
  const [editPersonId, setEditPersonId] = useState("");
  const [editingLink, setEditingLink] = useState<string | null>(null);
  const [editLinkStudentId, setEditLinkStudentId] = useState("");
  const [accountName, setAccountName] = useState("");
  const [accountEmail, setAccountEmail] = useState("");
  const [accountPassword, setAccountPassword] = useState("");
  const [accountRole, setAccountRole] = useState<(typeof accountRoles)[number]>("Student");
  const [editingAccount, setEditingAccount] = useState<string | null>(null);
  const [editAccountName, setEditAccountName] = useState("");
  const [editAccountRole, setEditAccountRole] = useState<(typeof accountRoles)[number]>("Student");
  const [resettingAccount, setResettingAccount] = useState<string | null>(null);
  const [resetPassword, setResetPassword] = useState("");

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
          setHomeHref(roleHome(loaded.roles));
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
    } catch (err) {
      setStatusError(true);
      setStatus(
        err instanceof ApiError ? translateError(err.code, "organisation.manage.failed") : t("organisation.manage.failed")
      );
    } finally {
      setBusy(false);
    }
  }

  async function saveAccount(action: () => Promise<unknown>) {
    if (!token) {
      return;
    }
    setBusy(true);
    setStatus(null);
    setStatusError(false);
    try {
      await action();
      setDirectory(await listDirectoryUsers(token));
      setStatus(t("organisation.manage.saved"));
    } catch (err) {
      setStatusError(true);
      setStatus(
        err instanceof ApiError ? translateError(err.code, "organisation.manage.failed") : t("organisation.manage.failed")
      );
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
          <Link href={homeHref ?? "/login"} className="underline">
            {homeHref ? t("common.backToHome") : t("common.backToLogin")}
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
        <FocusCard>
          <h2 className="font-medium">{t("organisation.accounts.title")}</h2>
          {directory.length === 0 ? (
            <p className="text-sm text-black/60">{t("organisation.accounts.empty")}</p>
          ) : (
            <ul className="space-y-3 text-sm">
              {directory.map((person) => {
                const isSelf = person.id === profile.id;
                const currentRole =
                  accountRoles.find((role) => person.roles.includes(role)) ?? "Student";
                const roleChoices =
                  isSelf && person.roles.includes("SystemAdministrator")
                    ? (["SystemAdministrator"] as const)
                    : accountRoles;
                return (
                  <li key={person.id} className="space-y-2">
                    {editingAccount === person.id ? (
                      <form
                        className="space-y-3"
                        aria-label={t("organisation.accounts.editAccount", { name: person.name })}
                        onSubmit={(event) => {
                          event.preventDefault();
                          void saveAccount(async () => {
                            await updateDirectoryUser(token, person.id, {
                              name: editAccountName,
                              role: editAccountRole,
                            });
                            setEditingAccount(null);
                          });
                        }}
                      >
                        <Field
                          label={t("organisation.accounts.name")}
                          value={editAccountName}
                          onChange={setEditAccountName}
                        />
                        <label className="block space-y-1 text-sm">
                          <span>{t("organisation.accounts.role")}</span>
                          <select
                            className="w-full rounded-lg border border-black/10 bg-white px-3 py-2"
                            value={editAccountRole}
                            onChange={(event) =>
                              setEditAccountRole(event.target.value as (typeof accountRoles)[number])
                            }
                          >
                            {roleChoices.map((role) => (
                              <option key={role} value={role}>
                                {t(`organisation.role.${role}`)}
                              </option>
                            ))}
                          </select>
                        </label>
                        <TextButton type="submit">{t("organisation.manage.save")}</TextButton>
                      </form>
                    ) : (
                      <div className="space-y-2">
                        <div className="flex flex-wrap items-center justify-between gap-2">
                          <span>
                            <DataText>{`${person.name} (${person.email})`}</DataText>
                            {" · "}
                            {person.roles.map((role) => t(`organisation.role.${role}`)).join(", ")}
                            {person.active === false ? (
                              <>
                                {" · "}
                                <span>{t("organisation.accounts.inactive")}</span>
                              </>
                            ) : null}
                          </span>
                          <span className="flex flex-wrap gap-3">
                            <TextButton
                              label={t("organisation.accounts.editAccount", { name: person.name })}
                              onClick={() => {
                                setEditingAccount(person.id);
                                setEditAccountName(person.name);
                                setEditAccountRole(currentRole);
                                setResettingAccount(null);
                              }}
                            >
                              {t("organisation.manage.edit")}
                            </TextButton>
                            {isSelf ? null : person.active === false ? (
                              <TextButton
                                label={t("organisation.accounts.reactivateAccount", { name: person.name })}
                                onClick={() => {
                                  void saveAccount(() => reactivateDirectoryUser(token, person.id));
                                }}
                              >
                                {t("organisation.accounts.reactivate")}
                              </TextButton>
                            ) : (
                              <TextButton
                                label={t("organisation.accounts.deactivateAccount", { name: person.name })}
                                onClick={() => {
                                  void saveAccount(() => deactivateDirectoryUser(token, person.id));
                                }}
                              >
                                {t("organisation.accounts.deactivate")}
                              </TextButton>
                            )}
                            <TextButton
                              label={t("organisation.accounts.resetPasswordAccount", { name: person.name })}
                              onClick={() => {
                                setResettingAccount(person.id);
                                setResetPassword("");
                                setEditingAccount(null);
                              }}
                            >
                              {t("organisation.accounts.resetPassword")}
                            </TextButton>
                          </span>
                        </div>
                        {resettingAccount === person.id ? (
                          <form
                            className="space-y-3"
                            aria-label={t("organisation.accounts.resetPasswordAccount", { name: person.name })}
                            onSubmit={(event) => {
                              event.preventDefault();
                              void saveAccount(async () => {
                                await resetDirectoryUserPassword(token, person.id, resetPassword);
                                setResettingAccount(null);
                                setResetPassword("");
                              });
                            }}
                          >
                            <Field
                              label={t("organisation.accounts.newPassword")}
                              value={resetPassword}
                              onChange={setResetPassword}
                              type="password"
                            />
                            <TextButton type="submit">{t("organisation.manage.save")}</TextButton>
                          </form>
                        ) : null}
                      </div>
                    )}
                  </li>
                );
              })}
            </ul>
          )}
        </FocusCard>
        <FocusCard>
          <h2 className="font-medium">{t("organisation.accounts.addTitle")}</h2>
          <form
            className="space-y-3"
            onSubmit={(event) => {
              event.preventDefault();
              if (!token) {
                return;
              }
              setBusy(true);
              setStatus(null);
              setStatusError(false);
              void createDirectoryUser(token, {
                name: accountName,
                email: accountEmail,
                password: accountPassword,
                role: accountRole,
              })
                .then(() => listDirectoryUsers(token))
                .then((people) => {
                  setDirectory(people);
                  setAccountName("");
                  setAccountEmail("");
                  setAccountPassword("");
                  setStatus(t("organisation.manage.saved"));
                })
                .catch((err: unknown) => {
                  setStatusError(true);
                  setStatus(
                    err instanceof ApiError ? translateError(err.code) : t("organisation.manage.failed")
                  );
                })
                .finally(() => setBusy(false));
            }}
          >
            <Field label={t("organisation.accounts.name")} value={accountName} onChange={setAccountName} />
            <Field
              label={t("organisation.accounts.email")}
              value={accountEmail}
              onChange={setAccountEmail}
              type="email"
            />
            <Field
              label={t("organisation.accounts.password")}
              value={accountPassword}
              onChange={setAccountPassword}
              type="password"
            />
            <p className="text-sm text-black/60">{t("organisation.accounts.passwordHint")}</p>
            <label className="block space-y-1 text-sm">
              <span>{t("organisation.accounts.role")}</span>
              <select
                className="w-full rounded-lg border border-black/10 bg-white px-3 py-2"
                value={accountRole}
                onChange={(event) => setAccountRole(event.target.value as (typeof accountRoles)[number])}
              >
                {accountRoles.map((role) => (
                  <option key={role} value={role}>
                    {t(`organisation.role.${role}`)}
                  </option>
                ))}
              </select>
            </label>
            <PrimaryButton type="submit" disabled={busy}>
              {t("organisation.accounts.add")}
            </PrimaryButton>
          </form>
        </FocusCard>
        <label className="block space-y-1 text-sm">
          <span>{t("organisation.manage.school")}</span>
          <select
            className="w-full rounded-lg border border-black/10 bg-white px-3 py-2"
            value={organisationId}
            onChange={(event) => setOrganisationId(event.target.value)}
          >
            {schools.map((school) => (
              <option key={school.id} value={school.id} dir="auto">
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
                  setYearOrderOverride(null);
                  setClassName("");
                  setClassCode("");
                });
              }}
            >
              <Field label={t("organisation.manage.yearName")} value={yearName} onChange={setYearName} />
              <Field
                label={t("organisation.manage.sortOrder")}
                value={String(yearOrder)}
                onChange={(value) => setYearOrderOverride(Number(value))}
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
          <>
            <FocusCard>
              <h2 className="font-medium">{t("organisation.manage.years")}</h2>
              {years.length === 0 ? (
                <p className="text-sm text-black/60">{t("organisation.manage.noYears")}</p>
              ) : (
                <ul className="space-y-3 text-sm">
                  {years.map((year) => (
                    <li key={year.id} className="space-y-2">
                      {editingYear === year.id ? (
                        <form
                          className="space-y-3"
                          onSubmit={(event) => {
                            event.preventDefault();
                            void run(async () => {
                              await updateYearLevel(
                                token,
                                organisationId,
                                year.id,
                                editYearName,
                                editYearOrder
                              );
                              setEditingYear(null);
                            });
                          }}
                        >
                          <Field
                            label={t("organisation.manage.yearName")}
                            value={editYearName}
                            onChange={setEditYearName}
                          />
                          <Field
                            label={t("organisation.manage.sortOrder")}
                            value={String(editYearOrder)}
                            onChange={(value) => setEditYearOrder(Number(value))}
                            type="number"
                          />
                          <TextButton type="submit">{t("organisation.manage.save")}</TextButton>
                        </form>
                      ) : (
                        <div className="flex flex-wrap items-center justify-between gap-2">
                          <span>
                            <DataText>{year.name}</DataText>
                          </span>
                          <span className="flex gap-3">
                            <TextButton
                              onClick={() => {
                                setEditingYear(year.id);
                                setEditYearName(year.name);
                                setEditYearOrder(year.sortOrder);
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
                        </div>
                      )}
                    </li>
                  ))}
                </ul>
              )}
            </FocusCard>
            {!firstTime ? (
              <FocusCard>
                <h2 className="font-medium">{t("organisation.manage.addYearTitle")}</h2>
                <form
                  className="space-y-3"
                  onSubmit={(event) => {
                    event.preventDefault();
                    void run(async () => {
                      await createYearLevel(token, organisationId, yearName, yearOrder);
                      setYearName("");
                      setYearOrderOverride(null);
                    });
                  }}
                >
                  <Field label={t("organisation.manage.yearName")} value={yearName} onChange={setYearName} />
                  <Field
                    label={t("organisation.manage.sortOrder")}
                    value={String(yearOrder)}
                    onChange={(value) => setYearOrderOverride(Number(value))}
                    type="number"
                  />
                  <PrimaryButton type="submit" disabled={busy}>
                    {t("organisation.manage.addYear")}
                  </PrimaryButton>
                </form>
              </FocusCard>
            ) : null}
          </>
        ) : null}

        {tab === "classes" ? (
          <>
            <FocusCard>
              <h2 className="font-medium">{t("organisation.manage.classes")}</h2>
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
                                editClassName,
                                editClassCode,
                                classYearId || schoolClass.yearLevelId
                              );
                              setEditingClass(null);
                            });
                          }}
                        >
                          <Field
                            label={t("organisation.manage.className")}
                            value={editClassName}
                            onChange={setEditClassName}
                          />
                          <Field
                            label={t("organisation.manage.classCode")}
                            value={editClassCode}
                            onChange={setEditClassCode}
                          />
                          <TextButton type="submit">{t("organisation.manage.save")}</TextButton>
                        </form>
                      ) : (
                        <div className="flex flex-wrap items-center justify-between gap-2">
                          <span>
                            <DataText>{`${schoolClass.name} (${schoolClass.code})`}</DataText>
                          </span>
                          <span className="flex gap-3">
                            <TextButton
                              onClick={() => {
                                setEditingClass(schoolClass.id);
                                setEditClassName(schoolClass.name);
                                setEditClassCode(schoolClass.code);
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
            </FocusCard>
            {!firstTime ? (
              <FocusCard>
                <h2 className="font-medium">{t("organisation.manage.addClassTitle")}</h2>
                <form
                  className="space-y-3"
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
                        <option key={year.id} value={year.id} dir="auto">
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
              </FocusCard>
            ) : null}
          </>
        ) : null}

        {tab === "staff" || tab === "students" ? (
          <>
            <FocusCard>
              <h2 className="font-medium">
                {tab === "staff" ? t("organisation.manage.staff") : t("organisation.manage.students")}
              </h2>
              <ClassPicker
                label={t("organisation.manage.classLabel")}
                classes={classes}
                classId={classId}
                onChange={setClassId}
              />
              {people.length === 0 ? (
                <p className="text-sm text-black/60">{t("organisation.manage.noPeople")}</p>
              ) : (
                <ul className="space-y-3 text-sm">
                  {people.map((person) => (
                    <li key={person.userId} className="space-y-2">
                      {editingPerson === person.userId ? (
                        <form
                          className="space-y-3"
                          onSubmit={(event) => {
                            event.preventDefault();
                            void run(async () => {
                              if (tab === "staff") {
                                await unassignTeacher(token, organisationId, classId, person.userId);
                                await assignTeacher(token, organisationId, classId, editPersonId);
                                setPeople(await listTeachers(token, organisationId, classId));
                              } else {
                                await unenrollStudent(token, organisationId, classId, person.userId);
                                await enrollStudent(token, organisationId, classId, editPersonId);
                                setPeople(await listEnrollments(token, organisationId, classId));
                              }
                              setEditingPerson(null);
                            });
                          }}
                        >
                          <PersonSelect
                            label={t("organisation.manage.userId")}
                            value={editPersonId}
                            onChange={setEditPersonId}
                            people={directory.filter((entry) =>
                              entry.roles.includes(tab === "staff" ? "Teacher" : "Student")
                            )}
                          />
                          <TextButton type="submit">{t("organisation.manage.save")}</TextButton>
                        </form>
                      ) : (
                        <div className="flex flex-wrap items-center justify-between gap-2">
                          <span>
                            <DataText>
                              {personName(directory, person.userId, t("organisation.manage.unknownPerson"))}
                            </DataText>
                          </span>
                          <span className="flex gap-3">
                            <TextButton
                              onClick={() => {
                                setEditingPerson(person.userId);
                                setEditPersonId(person.userId);
                              }}
                            >
                              {t("organisation.manage.edit")}
                            </TextButton>
                            <TextButton
                              onClick={() => {
                                void run(async () => {
                                  if (tab === "staff") {
                                    await unassignTeacher(token, organisationId, classId, person.userId);
                                    setPeople(await listTeachers(token, organisationId, classId));
                                  } else {
                                    await unenrollStudent(token, organisationId, classId, person.userId);
                                    setPeople(await listEnrollments(token, organisationId, classId));
                                  }
                                });
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
            </FocusCard>
            <FocusCard>
              <h2 className="font-medium">
                {tab === "staff"
                  ? t("organisation.manage.addStaffTitle")
                  : t("organisation.manage.addStudentTitle")}
              </h2>
              <form
                className="space-y-3"
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
          </>
        ) : null}

        {tab === "families" ? (
          <>
            <FocusCard>
              <h2 className="font-medium">{t("organisation.manage.families")}</h2>
              <PersonSelect
                label={t("organisation.manage.parentId")}
                value={parentId}
                onChange={(value) => {
                  setParentId(value);
                  if (!value) {
                    setLinks([]);
                    return;
                  }
                  void listParentChildren(token, value)
                    .then(setLinks)
                    .catch(() => {
                      setStatusError(true);
                      setStatus(t("organisation.manage.failed"));
                    });
                }}
                people={directory.filter((person) => person.roles.includes("Parent"))}
              />
              {links.length === 0 ? (
                <p className="text-sm text-black/60">{t("organisation.manage.noLinks")}</p>
              ) : (
                <ul className="space-y-3 text-sm">
                  {links.map((link) => (
                    <li key={link.studentUserId} className="space-y-2">
                      {editingLink === link.studentUserId ? (
                        <form
                          className="space-y-3"
                          onSubmit={(event) => {
                            event.preventDefault();
                            void run(async () => {
                              await unlinkParentChild(token, link.parentUserId, link.studentUserId);
                              await linkParentChild(token, link.parentUserId, editLinkStudentId);
                              setLinks(await listParentChildren(token, link.parentUserId));
                              setEditingLink(null);
                            });
                          }}
                        >
                          <PersonSelect
                            label={t("organisation.manage.studentId")}
                            value={editLinkStudentId}
                            onChange={setEditLinkStudentId}
                            people={directory.filter((person) => person.roles.includes("Student"))}
                          />
                          <TextButton type="submit">{t("organisation.manage.save")}</TextButton>
                        </form>
                      ) : (
                        <div className="flex flex-wrap items-center justify-between gap-2">
                          <span>
                            <DataText>
                              {personName(directory, link.studentUserId, t("organisation.manage.unknownPerson"))}
                            </DataText>
                          </span>
                          <span className="flex gap-3">
                            <TextButton
                              onClick={() => {
                                setEditingLink(link.studentUserId);
                                setEditLinkStudentId(link.studentUserId);
                              }}
                            >
                              {t("organisation.manage.edit")}
                            </TextButton>
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
                          </span>
                        </div>
                      )}
                    </li>
                  ))}
                </ul>
              )}
            </FocusCard>
            <FocusCard>
              <h2 className="font-medium">{t("organisation.manage.addFamilyTitle")}</h2>
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
                  label={t("organisation.manage.studentId")}
                  value={studentId}
                  onChange={setStudentId}
                  people={directory.filter((person) => person.roles.includes("Student"))}
                />
                <PrimaryButton type="submit" disabled={busy || !parentId}>
                  {t("organisation.manage.addPerson")}
                </PrimaryButton>
              </form>
            </FocusCard>
          </>
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
  label,
}: {
  children: React.ReactNode;
  onClick?: () => void;
  type?: "button" | "submit";
  label?: string;
}) {
  return (
    <button
      type={type}
      onClick={onClick}
      aria-label={label}
      className="text-sm text-black/60 underline-offset-2 hover:underline"
    >
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
          <option key={person.id} value={person.id} dir="auto">
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
          <option key={schoolClass.id} value={schoolClass.id} dir="auto">
            {schoolClass.name}
          </option>
        ))}
      </select>
    </label>
  );
}
