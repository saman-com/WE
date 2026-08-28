"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { fetchProfile, type UserProfile } from "@/lib/auth";
import {
  assignTeacher,
  createClass,
  createOrganisation,
  createYearLevel,
  enrollStudent,
  type Organisation,
  type SchoolClass,
  type YearLevel,
} from "@/lib/organisation";

const STUDENT_SEED_ID = "22222222-2222-2222-2222-222222222222";

export default function OrganisationSetupPage() {
  const router = useRouter();
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
      .then((loaded) => {
        if (!loaded.roles.includes("SystemAdministrator")) {
          setError("Only system administrators can set up organisations.");
          return;
        }
        setProfile(loaded);
      })
      .catch(() => {
        localStorage.removeItem("we_access_token");
        router.replace("/login");
      });
  }, [router]);

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
        <p>Loading organisation setup...</p>
      </div>
    );
  }

  return (
    <div className="min-h-screen p-8">
      <div className="max-w-xl mx-auto space-y-6">
        <div className="flex items-center justify-between">
          <h1 className="text-2xl font-semibold">Organisation setup</h1>
          <Link href="/dashboard" className="text-sm underline">
            Dashboard
          </Link>
        </div>

        <p className="text-sm text-black/60">
          Create a school, add a year level and class, assign a teacher, and
          enroll a student. Seed student id: {STUDENT_SEED_ID}
        </p>

        {error ? <p className="text-sm text-red-600">{error}</p> : null}
        {message ? <p className="text-sm text-green-700">{message}</p> : null}

        <form
          className="rounded-lg border border-black/10 p-6 space-y-3"
          onSubmit={(event) => {
            event.preventDefault();
            void run(async () => {
              const created = await createOrganisation(token, schoolName, schoolCode);
              setOrganisation(created);
              setMessage(`Created school ${created.name}.`);
            });
          }}
        >
          <h2 className="font-medium">1. Create school</h2>
          <input
            className="w-full rounded border border-black/20 px-3 py-2"
            value={schoolName}
            onChange={(event) => setSchoolName(event.target.value)}
            placeholder="School name"
            required
          />
          <input
            className="w-full rounded border border-black/20 px-3 py-2"
            value={schoolCode}
            onChange={(event) => setSchoolCode(event.target.value)}
            placeholder="School code"
            required
          />
          <button
            type="submit"
            disabled={busy}
            className="rounded bg-black text-white px-4 py-2 text-sm disabled:opacity-60"
          >
            Create school
          </button>
          {organisation ? (
            <p className="text-sm">School id: {organisation.id}</p>
          ) : null}
        </form>

        <form
          className="rounded-lg border border-black/10 p-6 space-y-3"
          onSubmit={(event) => {
            event.preventDefault();
            if (!organisation) {
              setError("Create a school first.");
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
              setMessage(`Created ${created.name}.`);
            });
          }}
        >
          <h2 className="font-medium">2. Add year level</h2>
          <input
            className="w-full rounded border border-black/20 px-3 py-2"
            value={yearName}
            onChange={(event) => setYearName(event.target.value)}
            placeholder="Year level name"
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
            Create year level
          </button>
        </form>

        <form
          className="rounded-lg border border-black/10 p-6 space-y-3"
          onSubmit={(event) => {
            event.preventDefault();
            if (!organisation || !yearLevel) {
              setError("Create a school and year level first.");
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
              setMessage(`Created class ${created.name}.`);
            });
          }}
        >
          <h2 className="font-medium">3. Create class</h2>
          <input
            className="w-full rounded border border-black/20 px-3 py-2"
            value={className}
            onChange={(event) => setClassName(event.target.value)}
            placeholder="Class name"
            required
          />
          <input
            className="w-full rounded border border-black/20 px-3 py-2"
            value={classCode}
            onChange={(event) => setClassCode(event.target.value)}
            placeholder="Class code"
            required
          />
          <button
            type="submit"
            disabled={busy || !yearLevel}
            className="rounded bg-black text-white px-4 py-2 text-sm disabled:opacity-60"
          >
            Create class
          </button>
        </form>

        <form
          className="rounded-lg border border-black/10 p-6 space-y-3"
          onSubmit={(event) => {
            event.preventDefault();
            if (!organisation || !schoolClass) {
              setError("Create a class first.");
              return;
            }
            void run(async () => {
              await assignTeacher(
                token,
                organisation.id,
                schoolClass.id,
                teacherUserId
              );
              setMessage("Teacher assigned.");
            });
          }}
        >
          <h2 className="font-medium">4. Assign teacher</h2>
          <input
            className="w-full rounded border border-black/20 px-3 py-2"
            value={teacherUserId}
            onChange={(event) => setTeacherUserId(event.target.value)}
            placeholder="Teacher user id from dashboard"
            required
          />
          <button
            type="submit"
            disabled={busy || !schoolClass}
            className="rounded bg-black text-white px-4 py-2 text-sm disabled:opacity-60"
          >
            Assign teacher
          </button>
        </form>

        <form
          className="rounded-lg border border-black/10 p-6 space-y-3"
          onSubmit={(event) => {
            event.preventDefault();
            if (!organisation || !schoolClass) {
              setError("Create a class first.");
              return;
            }
            void run(async () => {
              await enrollStudent(
                token,
                organisation.id,
                schoolClass.id,
                studentUserId
              );
              setMessage("Student enrolled.");
            });
          }}
        >
          <h2 className="font-medium">5. Enroll student</h2>
          <input
            className="w-full rounded border border-black/20 px-3 py-2"
            value={studentUserId}
            onChange={(event) => setStudentUserId(event.target.value)}
            placeholder="Student user id"
            required
          />
          <button
            type="submit"
            disabled={busy || !schoolClass}
            className="rounded bg-black text-white px-4 py-2 text-sm disabled:opacity-60"
          >
            Enroll student
          </button>
        </form>
      </div>
    </div>
  );
}
