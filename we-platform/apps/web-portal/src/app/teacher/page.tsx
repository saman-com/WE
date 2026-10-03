"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { fetchProfile, type UserProfile } from "@/lib/auth";
import { listClasses, listOrganisations, type SchoolClass } from "@/lib/organisation";
import { useI18n } from "@/i18n/I18nProvider";

type ClassWithOrganisation = SchoolClass & {
  organisationName: string;
};

function isTeacher(profile: UserProfile): boolean {
  return profile.roles.includes("Teacher");
}

export default function TeacherWorkspacePage() {
  const router = useRouter();
  const { t } = useI18n();
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [classes, setClasses] = useState<ClassWithOrganisation[]>([]);

  useEffect(() => {
    const token = localStorage.getItem("we_access_token");
    if (!token) {
      router.replace("/login");
      return;
    }

    fetchProfile(token)
      .then(async (loaded) => {
        if (!isTeacher(loaded)) {
          router.replace("/dashboard");
          return;
        }
        setProfile(loaded);
        const organisations = await listOrganisations(token);
        const scoped = await Promise.all(
          organisations.map(async (organisation) => {
            const organisationClasses = await listClasses(token, organisation.id);
            return organisationClasses.map((schoolClass) => ({
              ...schoolClass,
              organisationName: organisation.name,
            }));
          })
        );
        setClasses(scoped.flat());
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
        <p>{t("teacher.workspace.loading")}</p>
      </div>
    );
  }

  return (
    <div className="min-h-screen p-8">
      <div className="max-w-3xl mx-auto space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-semibold">{t("teacher.workspace.title")}</h1>
            <p className="text-sm text-black/60 mt-1">
              {t("teacher.workspace.subtitle")}
            </p>
          </div>
          <button
            onClick={handleLogout}
            className="text-sm underline"
            type="button"
          >
            {t("common.signOut")}
          </button>
        </div>

        <div className="flex gap-4 text-sm">
          <Link href="/assessments" className="underline">
            {t("dashboard.nav.assessments")}
          </Link>
          <Link href="/curriculum" className="underline">
            {t("dashboard.nav.curriculum")}
          </Link>
          <Link href="/teacher/interventions" className="underline">
            {t("teacher.workspace.nav.interventions")}
          </Link>
          <Link href="/teacher/messages" className="underline">
            {t("teacher.workspace.nav.parentMessages")}
          </Link>
        </div>

        <div className="rounded-lg border border-black/10 p-6 space-y-3">
          <h2 className="font-medium">{t("teacher.workspace.myClasses")}</h2>
          {classes.length === 0 ? (
            <p className="text-sm text-black/60">{t("teacher.workspace.noClasses")}</p>
          ) : (
            <ul className="space-y-3">
              {classes.map((schoolClass) => (
                <li key={schoolClass.id} className="border border-black/10 rounded p-4">
                  <Link
                    href={`/teacher/classes/${schoolClass.id}?organisationId=${schoolClass.organisationId}`}
                    className="font-medium underline"
                  >
                    {schoolClass.name} ({schoolClass.code})
                  </Link>
                  <p className="text-sm text-black/60 mt-1">
                    {schoolClass.organisationName}
                  </p>
                  <p className="text-sm text-black/60">
                    {t("teacher.workspace.studentsEnrolled", {
                      count: schoolClass.studentUserIds?.length ?? 0,
                    })}
                  </p>
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>
    </div>
  );
}
