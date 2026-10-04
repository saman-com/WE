"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { fetchProfile, type UserProfile } from "@/lib/auth";
import { listClasses, listOrganisations, type SchoolClass } from "@/lib/organisation";
import { FocusCard, LearningFrame, PrimaryLink } from "@/components/learning-frame";
import { useI18n } from "@/i18n/I18nProvider";

type ClassWithOrganisation = SchoolClass & {
  organisationName: string;
};

function isTeacher(profile: UserProfile): boolean {
  return profile.roles.includes("Teacher");
}

function isLocalTeacherPreview(): boolean {
  if (typeof window === "undefined") {
    return false;
  }
  const host = window.location.hostname;
  const local = host === "localhost" || host === "127.0.0.1";
  return local && new URLSearchParams(window.location.search).get("preview") === "1";
}

export default function TeacherWorkspacePage() {
  const router = useRouter();
  const { t } = useI18n();
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [classes, setClasses] = useState<ClassWithOrganisation[]>([]);

  useEffect(() => {
    if (isLocalTeacherPreview()) {
      setProfile({
        id: "preview-teacher",
        email: "teacher@school.local",
        name: "Demo Teacher",
        roles: ["Teacher"],
      });
      setClasses([
        {
          id: "class-maths",
          organisationId: "org",
          yearLevelId: "year-11",
          name: "Year 11 Mathematics",
          code: "11MAT",
          organisationName: "Demo school",
          studentUserIds: ["s1", "s2", "s3"],
        },
      ]);
      return;
    }

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
        try {
          const organisations = await listOrganisations(token);
          const scoped = await Promise.all(
            organisations.map(async (organisation) => {
              const organisationClasses = await listClasses(token, organisation.id);
              return organisationClasses
                .filter((schoolClass) => schoolClass.teacherUserIds?.includes(loaded.id))
                .map((schoolClass) => ({
                  ...schoolClass,
                  organisationName: organisation.name,
                }));
            })
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
      <div className="we-learning flex items-center justify-center p-6">
        <div className="space-y-4 text-center">
          <p className="text-red-700">{error}</p>
          <Link href="/login" className="underline">
            {t("common.backToLogin")}
          </Link>
        </div>
      </div>
    );
  }

  if (!profile) {
    return (
      <div className="we-learning flex items-center justify-center p-6">
        <p>{t("teacher.workspace.loading")}</p>
      </div>
    );
  }

  const [firstClass, ...otherClasses] = classes;

  const mutedLinks = (
    <div className="flex flex-wrap gap-x-4 gap-y-2 text-sm text-black/60">
      <Link href="/assessments" className="underline-offset-2 hover:underline">
        {t("dashboard.nav.assessments")}
      </Link>
      <Link href="/curriculum" className="underline-offset-2 hover:underline">
        {t("dashboard.nav.curriculum")}
      </Link>
      <Link href="/teacher/interventions" className="underline-offset-2 hover:underline">
        {t("teacher.workspace.nav.interventions")}
      </Link>
      <Link href="/teacher/messages" className="underline-offset-2 hover:underline">
        {t("teacher.workspace.nav.parentMessages")}
      </Link>
    </div>
  );

  return (
    <LearningFrame
      eyebrow="WE"
      title={profile.name}
      onSignOut={handleLogout}
      signOutLabel={t("common.signOut")}
    >
      <div className="space-y-6">
        <h1 className="max-w-xl text-2xl font-semibold leading-snug">
          {t("teacher.home.headline")}
        </h1>

        {firstClass ? (
          <div className="grid gap-6 md:grid-cols-[minmax(0,1.2fr)_minmax(0,0.8fr)]">
            <FocusCard>
              <div className="space-y-2">
                <p className="text-sm text-black/60">{firstClass.organisationName}</p>
                <p className="text-lg font-semibold">
                  {firstClass.name} ({firstClass.code})
                </p>
                <p className="text-sm">
                  {t("teacher.workspace.studentsEnrolled", {
                    count: firstClass.studentUserIds?.length ?? 0,
                  })}
                </p>
              </div>
            </FocusCard>
            <div className="space-y-3">
              <PrimaryLink
                href={`/teacher/classes/${firstClass.id}?organisationId=${firstClass.organisationId}`}
              >
                {t("teacher.home.openClass", { name: firstClass.name })}
              </PrimaryLink>
              {mutedLinks}
            </div>
          </div>
        ) : (
          <div className="space-y-3">
            <p className="text-sm text-black/60">{t("teacher.workspace.noClasses")}</p>
            {mutedLinks}
          </div>
        )}

        {otherClasses.length > 0 ? (
          <ul className="divide-y divide-black/10 border-y border-black/10">
            {otherClasses.map((schoolClass) => (
              <li key={schoolClass.id}>
                <Link
                  href={`/teacher/classes/${schoolClass.id}?organisationId=${schoolClass.organisationId}`}
                  className="group flex items-baseline justify-between gap-3 py-3"
                >
                  <div>
                    <p className="text-sm font-semibold underline-offset-2 group-hover:underline">
                      {schoolClass.name} ({schoolClass.code})
                    </p>
                    <p className="text-sm text-black/60">{schoolClass.organisationName}</p>
                  </div>
                  <p className="text-sm text-black/60">
                    {t("teacher.workspace.studentsEnrolled", {
                      count: schoolClass.studentUserIds?.length ?? 0,
                    })}
                  </p>
                </Link>
              </li>
            ))}
          </ul>
        ) : null}
      </div>
    </LearningFrame>
  );
}
