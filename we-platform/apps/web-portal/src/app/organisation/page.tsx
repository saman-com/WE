"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { LearningFrame } from "@/components/learning-frame";
import { fetchProfile, type UserProfile } from "@/lib/auth";
import {
  assignTeacher,
  createClass,
  createOrganisation,
  createYearLevel,
  enrollStudent,
  listOrganisations,
  type Organisation,
  type SchoolClass,
  type YearLevel,
} from "@/lib/organisation";
import { useI18n } from "@/i18n/I18nProvider";

const STUDENT_SEED_ID = "22222222-2222-2222-2222-222222222222";

export default function OrganisationSetupPage() {
  const router = useRouter();
  const { t } = useI18n();
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [token, setToken] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const [schoolName, setSchoolName] = useState("Westfield Primary");
  const [schoolCode, setSchoolCode] = useState("WFP");
  const [yearName, setYearName] = useState("Year 7");
  const [yearOrder, setYearOrder] = useState(7);
  const [className, setClassName] = useState("7A");
  const [classCode, setClassCode] = useState("7A");
  const [teacherUserId, setTeacherUserId] = useState("");
  const [studentUserId, setStudentUserId] = useState(STUDENT_SEED_ID);

  const [organisation, setOrganisation] = useState<Organisation | null>(null);
  const [schools, setSchools] = useState<Organisation[]>([]);
  const [yearLevel, setYearLevel] = useState<YearLevel | null>(null);
  const [schoolClass, setSchoolClass] = useState<SchoolClass | null>(null);

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
        try {
          setSchools(await listOrganisations(stored));
        } catch {
          setSchools([]);
        }
      })
      .catch(() => {
        localStorage.removeItem("we_access_token");
        router.replace("/login");
      });
  }, [router, t]);

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
    >
      <div className="mx-auto max-w-xl space-y-6">
        <h1 className="text-2xl font-semibold">{t("dashboard.nav.organisationSetup")}</h1>

        <p className="text-sm text-black/60">
          {t("organisation.intro")}
        </p>

        {schools.length > 0 ? (
          <ul className="text-sm">
            {schools.map((school) => (
              <li key={school.id}>{school.name}</li>
            ))}
          </ul>
        ) : null}

        {error ? <p className="text-sm text-red-600">{error}</p> : null}
        {message ? <p className="text-sm text-green-700">{message}</p> : null}

        <form
          className="rounded-lg border border-black/10 p-6 space-y-3"
          onSubmit={(event) => {
            event.preventDefault();
            void run(async () => {
              const created = await createOrganisation(token, schoolName, schoolCode);
              setOrganisation(created);
              setMessage(t("organisation.createdSchool", { name: created.name }));
            });
          }}
        >
          <h2 className="font-medium">{t("organisation.step1Title")}</h2>
          <input
            className="w-full rounded border border-black/20 px-3 py-2"
            value={schoolName}
            onChange={(event) => setSchoolName(event.target.value)}
            placeholder={t("organisation.schoolNamePlaceholder")}
            required
          />
          <input
            className="w-full rounded border border-black/20 px-3 py-2"
            value={schoolCode}
            onChange={(event) => setSchoolCode(event.target.value)}
            placeholder={t("organisation.schoolCodePlaceholder")}
            required
          />
          <button
            type="submit"
            disabled={busy}
            className="rounded bg-black text-white px-4 py-2 text-sm disabled:opacity-60"
          >
            {t("organisation.createSchool")}
          </button>
          {organisation ? (
            <p className="text-sm">{t("organisation.schoolId", { id: organisation.id })}</p>
          ) : null}
        </form>

        <form
          className="rounded-lg border border-black/10 p-6 space-y-3"
          onSubmit={(event) => {
            event.preventDefault();
            if (!organisation) {
              setError(t("organisation.createSchoolFirst"));
              return;
            }
            void run(async () => {
              const created = await createYearLevel(
                token,
                organisation.id,
                yearName,
                yearOrder
              );
              setYearLevel(created);
              setMessage(t("organisation.createdYearLevel", { name: created.name }));
            });
          }}
        >
          <h2 className="font-medium">{t("organisation.step2Title")}</h2>
          <input
            className="w-full rounded border border-black/20 px-3 py-2"
            value={yearName}
            onChange={(event) => setYearName(event.target.value)}
            placeholder={t("organisation.yearLevelNamePlaceholder")}
            required
          />
          <input
            type="number"
            className="w-full rounded border border-black/20 px-3 py-2"
            value={yearOrder}
            onChange={(event) => setYearOrder(Number(event.target.value))}
            required
          />
          <button
            type="submit"
            disabled={busy || !organisation}
            className="rounded bg-black text-white px-4 py-2 text-sm disabled:opacity-60"
          >
            {t("organisation.createYearLevel")}
          </button>
        </form>

        <form
          className="rounded-lg border border-black/10 p-6 space-y-3"
          onSubmit={(event) => {
            event.preventDefault();
            if (!organisation || !yearLevel) {
              setError(t("organisation.createSchoolAndYearFirst"));
              return;
            }
            void run(async () => {
              const created = await createClass(
                token,
                organisation.id,
                yearLevel.id,
                className,
                classCode
              );
              setSchoolClass(created);
              setMessage(t("organisation.createdClass", { name: created.name }));
            });
          }}
        >
          <h2 className="font-medium">{t("organisation.step3Title")}</h2>
          <input
            className="w-full rounded border border-black/20 px-3 py-2"
            value={className}
            onChange={(event) => setClassName(event.target.value)}
            placeholder={t("organisation.classNamePlaceholder")}
            required
          />
          <input
            className="w-full rounded border border-black/20 px-3 py-2"
            value={classCode}
            onChange={(event) => setClassCode(event.target.value)}
            placeholder={t("organisation.classCodePlaceholder")}
            required
          />
          <button
            type="submit"
            disabled={busy || !yearLevel}
            className="rounded bg-black text-white px-4 py-2 text-sm disabled:opacity-60"
          >
            {t("organisation.createClass")}
          </button>
        </form>

        <form
          className="rounded-lg border border-black/10 p-6 space-y-3"
          onSubmit={(event) => {
            event.preventDefault();
            if (!organisation || !schoolClass) {
              setError(t("organisation.createClassFirst"));
              return;
            }
            void run(async () => {
              await assignTeacher(
                token,
                organisation.id,
                schoolClass.id,
                teacherUserId
              );
              setMessage(t("organisation.teacherAssigned"));
            });
          }}
        >
          <h2 className="font-medium">{t("organisation.step4Title")}</h2>
          <input
            className="w-full rounded border border-black/20 px-3 py-2"
            value={teacherUserId}
            onChange={(event) => setTeacherUserId(event.target.value)}
            placeholder={t("organisation.teacherIdPlaceholder")}
            required
          />
          <button
            type="submit"
            disabled={busy || !schoolClass}
            className="rounded bg-black text-white px-4 py-2 text-sm disabled:opacity-60"
          >
            {t("organisation.assignTeacher")}
          </button>
        </form>

        <form
          className="rounded-lg border border-black/10 p-6 space-y-3"
          onSubmit={(event) => {
            event.preventDefault();
            if (!organisation || !schoolClass) {
              setError(t("organisation.createClassFirst"));
              return;
            }
            void run(async () => {
              await enrollStudent(
                token,
                organisation.id,
                schoolClass.id,
                studentUserId
              );
              setMessage(t("organisation.studentEnrolled"));
            });
          }}
        >
          <h2 className="font-medium">{t("organisation.step5Title")}</h2>
          <input
            className="w-full rounded border border-black/20 px-3 py-2"
            value={studentUserId}
            onChange={(event) => setStudentUserId(event.target.value)}
            placeholder={t("organisation.studentIdPlaceholder")}
            required
          />
          <button
            type="submit"
            disabled={busy || !schoolClass}
            className="rounded bg-black text-white px-4 py-2 text-sm disabled:opacity-60"
          >
            {t("organisation.enrollStudent")}
          </button>
        </form>
      </div>
    </LearningFrame>
  );
}
