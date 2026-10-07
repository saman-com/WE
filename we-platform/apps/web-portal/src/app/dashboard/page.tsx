"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { fetchProfile, listDirectoryUsers, personName, type DirectoryUser, type UserProfile } from "@/lib/auth";
import { roleHome } from "@/lib/role-home";
import { listClasses, listOrganisations, type SchoolClass } from "@/lib/organisation";
import { DataText } from "@/components/data-text";
import { useI18n } from "@/i18n/I18nProvider";

export default function DashboardPage() {
  const router = useRouter();
  const { t } = useI18n();
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [classes, setClasses] = useState<SchoolClass[]>([]);
  const [people, setPeople] = useState<DirectoryUser[]>([]);

  useEffect(() => {
    const token = localStorage.getItem("we_access_token");
    if (!token) {
      router.replace("/login");
      return;
    }

    fetchProfile(token)
      .then(async (loaded) => {
        setProfile(loaded);
        const canListDirectory = loaded.roles.some((role) =>
          role === "SystemAdministrator" ||
          role === "FederationAdmin" ||
          role === "Teacher" ||
          role === "Student" ||
          role === "SchoolLeader"
        );
        if (canListDirectory) {
          listDirectoryUsers(token).then(setPeople).catch(() => setPeople([]));
        }
        const canListOrganisations = loaded.roles.some((role) =>
          role === "Teacher" ||
          role === "Student" ||
          role === "SchoolLeader" ||
          role === "SystemAdministrator"
        );
        if (!canListOrganisations) {
          setClasses([]);
          return;
        }
        try {
          const organisations = await listOrganisations(token);
          const scoped = await Promise.all(
            organisations.map((organisation) => listClasses(token, organisation.id))
          );
          setClasses(scoped.flat());
        } catch {
          setClasses([]);
        }
      })
      .catch(() => {
        localStorage.removeItem("we_access_token");
        setError(t("common.sessionExpired"));
      });
  }, [router, t]);

  function handleLogout() {
    localStorage.removeItem("we_access_token");
    router.push("/login");
  }

  if (error) {
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

  if (!profile) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6">
        <p>{t("common.loadingProfile")}</p>
      </div>
    );
  }

  return (
    <div className="min-h-screen p-8">
      <div className="max-w-xl mx-auto space-y-6">
        <div className="flex items-center justify-between">
          <h1 className="text-2xl font-semibold">{t("dashboard.title")}</h1>
          <button
            onClick={handleLogout}
            className="text-sm underline"
            type="button"
          >
            {t("dashboard.signOut")}
          </button>
        </div>

        <div className="rounded-lg border border-black/10 p-6 space-y-2">
          <p className="font-medium">{t("dashboard.profile.nameLabel")}</p>
          <p>
            <DataText>{profile.name}</DataText>
          </p>
          <p className="font-medium">{t("dashboard.profile.emailLabel")}</p>
          <p>
            <DataText>{profile.email}</DataText>
          </p>
        </div>

        <nav className="flex flex-col gap-2">
        <Link href="/notifications" className="text-sm underline block">
          {t("dashboard.nav.notifications")}
        </Link>

        {profile.roles.includes("FederationAdmin") ? (
          <Link href="/admin/federation" className="text-sm underline block">
            {t("dashboard.nav.federationAdmin")}
          </Link>
        ) : null}

        {profile.roles.includes("SystemAdministrator") ? (
          <>
            <Link href="/organisation" className="text-sm underline block">
              {t("dashboard.nav.organisationSetup")}
            </Link>
            <Link href="/admin/ai-audit" className="text-sm underline block">
              {t("dashboard.nav.aiAuditLogs")}
            </Link>
            <Link href="/admin/regional-configuration" className="text-sm underline block">
              {t("dashboard.nav.regionalConfiguration")}
            </Link>
          </>
        ) : null}

        {profile.roles.includes("SystemAdministrator") ||
        profile.roles.includes("Teacher") ? (
          <>
            <Link href="/teacher" className="text-sm underline block">
              {t("dashboard.nav.teacherWorkspace")}
            </Link>
            <Link href="/curriculum" className="text-sm underline block">
              {t("dashboard.nav.curriculum")}
            </Link>
            <Link href="/assessments" className="text-sm underline block">
              {t("dashboard.nav.assessments")}
            </Link>
          </>
        ) : null}

        {profile.roles.includes("Student") ? (
          <>
            <Link href="/student" className="text-sm underline block">
              {t("dashboard.nav.studentWorkspace")}
            </Link>
            <Link href="/student/assessments" className="text-sm underline block">
              {t("dashboard.nav.myAssessments")}
            </Link>
          </>
        ) : null}

        {profile.roles.includes("Parent") ? (
          <>
            <Link href="/parent" className="text-sm underline block">
              {t("dashboard.nav.parentWorkspace")}
            </Link>
            <Link href="/parent/messages" className="text-sm underline block">
              {t("dashboard.nav.schoolMessages")}
            </Link>
          </>
        ) : null}

        {profile.roles.includes("SchoolLeader") ? (
          <>
            <Link href="/leadership" className="text-sm underline block">
              {t("dashboard.nav.leadershipDashboard")}
            </Link>
            <Link href="/admin/regional-configuration" className="text-sm underline block">
              {t("dashboard.nav.regionalConfiguration")}
            </Link>
          </>
        ) : null}

        {profile.roles.includes("EducationAuthorityOfficer") ? (
          <Link href="/authority" className="text-sm underline block">
            {t("dashboard.nav.policyDashboards")}
          </Link>
        ) : null}
        </nav>

        <div className="rounded-lg border border-black/10 p-6 space-y-2">
          <h2 className="font-medium">{t("dashboard.classes.title")}</h2>
          {classes.length === 0 ? (
            <p className="text-sm text-black/60">{t("dashboard.classes.empty")}</p>
          ) : (
            <ul className="space-y-3">
              {classes.map((schoolClass) => (
                <li key={schoolClass.id}>
                  <p className="font-medium">
                    <DataText>{`${schoolClass.name} (${schoolClass.code})`}</DataText>
                  </p>
                  {schoolClass.studentUserIds && schoolClass.studentUserIds.length > 0 ? (
                    <ul className="list-disc pl-5 mt-1 space-y-1">
                      {schoolClass.studentUserIds.map((studentUserId) => (
                        <li key={studentUserId}>
                          <Link
                            href={`/students/${studentUserId}/profile`}
                            className="text-sm underline"
                          >
                            {t("dashboard.classes.viewProfile", {
                              studentUserId: personName(
                                people,
                                studentUserId,
                                t("organisation.manage.unknownPerson")
                              ),
                            })}
                          </Link>
                        </li>
                      ))}
                    </ul>
                  ) : profile.roles.includes("Student") ? (
                    <Link
                      href={`/students/${profile.id}/profile`}
                      className="text-sm underline"
                    >
                      {t("dashboard.classes.viewMyLearningProfile")}
                    </Link>
                  ) : (
                    <p className="text-sm text-black/60">
                      {t("dashboard.classes.noStudentsEnrolled")}
                    </p>
                  )}
                </li>
              ))}
            </ul>
          )}
        </div>

        <Link href={roleHome(profile.roles)} className="text-sm underline">
          {t("dashboard.backToHome")}
        </Link>
      </div>
    </div>
  );
}
