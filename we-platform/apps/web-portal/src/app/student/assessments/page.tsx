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
  getMySubmission,
  listAssessments,
  submitAssessment,
  type Assessment,
  type AssessmentSubmission,
} from "@/lib/assessment";

type ClassScope = {
  organisationId: string;
  schoolClass: SchoolClass;
};

function isStudent(profile: UserProfile): boolean {
  return profile.roles.includes("Student");
}

export default function StudentAssessmentsPage() {
  const router = useRouter();
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [token, setToken] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const [classScopes, setClassScopes] = useState<ClassScope[]>([]);
  const [selectedClassId, setSelectedClassId] = useState("");
  const [assessments, setAssessments] = useState<Assessment[]>([]);
  const [selectedAssessmentId, setSelectedAssessmentId] = useState("");
  const [submission, setSubmission] = useState<AssessmentSubmission | null>(null);
  const [responses, setResponses] = useState("");

  const selectedScope = classScopes.find(
    (scope) => scope.schoolClass.id === selectedClassId
  );
  const selectedAssessment = assessments.find(
    (assessment) => assessment.id === selectedAssessmentId
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
        if (!isStudent(loaded)) {
          setError("This page is for students only.");
          return;
        }
        setProfile(loaded);
        try {
          const organisations = await listOrganisations(stored);
          const scoped = await Promise.all(
            organisations.map(async (organisation: Organisation) => {
              const classes = await listClasses(stored, organisation.id);
              return classes.map((schoolClass) => ({
                organisationId: organisation.id,
                schoolClass,
              }));
            })
          );
          const flattened = scoped.flat();
          setClassScopes(flattened);
          if (flattened[0]) {
            setSelectedClassId(flattened[0].schoolClass.id);
          }
        } catch {
          setClassScopes([]);
        }
      })
      .catch(() => {
        localStorage.removeItem("we_access_token");
        router.replace("/login");
      });
  }, [router]);

  useEffect(() => {
    if (!token || !selectedScope) {
      setAssessments([]);
      setSelectedAssessmentId("");
      return;
    }

    listAssessments(token, selectedScope.organisationId, selectedScope.schoolClass.id)
      .then((loaded) => {
        setAssessments(loaded);
        if (loaded[0]) {
          setSelectedAssessmentId(loaded[0].id);
        } else {
          setSelectedAssessmentId("");
        }
      })
      .catch(() => {
        setAssessments([]);
        setSelectedAssessmentId("");
      });
  }, [token, selectedScope, message]);

  useEffect(() => {
    if (!token || !selectedAssessmentId) {
      setSubmission(null);
      setResponses("");
      return;
    }

    getMySubmission(token, selectedAssessmentId)
      .then((loaded) => {
        setSubmission(loaded);
        setResponses(loaded?.responses ?? "");
      })
      .catch(() => {
        setSubmission(null);
        setResponses("");
      });
  }, [token, selectedAssessmentId, message]);

  async function handleSubmit() {
    if (!token || !selectedAssessmentId || submission) {
      return;
    }

    setBusy(true);
    setError(null);
    setMessage(null);
    try {
      const created = await submitAssessment(token, selectedAssessmentId, responses);
      setSubmission(created);
      setMessage(
        created.isLate
          ? "Assessment submitted (marked as late). Your teacher will review it."
          : "Assessment submitted. Your teacher will review it."
      );
    } catch (err) {
      setError(err instanceof Error ? err.message : "Submission failed.");
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
        <p>Loading...</p>
      </div>
    );
  }

  return (
    <div className="min-h-screen p-8">
      <div className="max-w-3xl mx-auto space-y-6">
        <div className="flex items-center justify-between">
          <h1 className="text-2xl font-semibold">My assessments</h1>
          <Link href="/student" className="text-sm underline">
            My learning
          </Link>
        </div>

        {error ? <p className="text-red-600">{error}</p> : null}
        {message ? <p className="text-green-700">{message}</p> : null}

        <div className="rounded-lg border border-black/10 p-6 space-y-4">
          <h2 className="font-medium">Class</h2>
          {classScopes.length === 0 ? (
            <p className="text-sm text-black/60">You are not enrolled in any classes.</p>
          ) : (
            <select
              className="w-full border rounded px-3 py-2"
              value={selectedClassId}
              onChange={(event) => setSelectedClassId(event.target.value)}
            >
              {classScopes.map((scope) => (
                <option key={scope.schoolClass.id} value={scope.schoolClass.id}>
                  {scope.schoolClass.name} ({scope.schoolClass.code})
                </option>
              ))}
            </select>
          )}
        </div>

        <div className="rounded-lg border border-black/10 p-6 space-y-4">
          <h2 className="font-medium">Published assessments</h2>
          {assessments.length === 0 ? (
            <p className="text-sm text-black/60">No published assessments for this class.</p>
          ) : (
            <ul className="space-y-3">
              {assessments.map((assessment) => (
                <li key={assessment.id}>
                  <button
                    type="button"
                    className={`w-full text-left border rounded p-3 ${
                      selectedAssessmentId === assessment.id ? "border-black" : "border-black/10"
                    }`}
                    onClick={() => setSelectedAssessmentId(assessment.id)}
                  >
                    <p className="font-medium">{assessment.title}</p>
                    {assessment.dueAt ? (
                      <p className="text-sm text-black/70">
                        Due: {new Date(assessment.dueAt).toLocaleString()}
                      </p>
                    ) : null}
                  </button>
                </li>
              ))}
            </ul>
          )}
        </div>

        {selectedAssessment ? (
          <div className="rounded-lg border border-black/10 p-6 space-y-4">
            <h2 className="font-medium">{selectedAssessment.title}</h2>
            {selectedAssessment.instructions ? (
              <p className="text-sm text-black/70">{selectedAssessment.instructions}</p>
            ) : null}
            {selectedAssessment.dueAt ? (
              <p className="text-sm">
                Due: {new Date(selectedAssessment.dueAt).toLocaleString()}
              </p>
            ) : null}

            {submission ? (
              <div className="space-y-3">
                <p className="text-sm text-green-700">
                  Submitted on {new Date(submission.submittedAt).toLocaleString()}
                  {submission.isLate ? " (late submission)" : ""}. Status: {submission.status}
                </p>
                <label className="block space-y-1">
                  <span className="text-sm">Your responses</span>
                  <textarea
                    className="w-full border rounded px-3 py-2 bg-black/5"
                    rows={6}
                    value={submission.responses}
                    readOnly
                  />
                </label>
                <p className="text-sm text-black/60">
                  Submissions cannot be edited after submit.
                </p>
              </div>
            ) : (
              <div className="space-y-3">
                <label className="block space-y-1">
                  <span className="text-sm">Your responses</span>
                  <textarea
                    className="w-full border rounded px-3 py-2"
                    rows={6}
                    value={responses}
                    onChange={(event) => setResponses(event.target.value)}
                    placeholder="Enter your answers here..."
                  />
                </label>
                <button
                  type="button"
                  disabled={busy || responses.trim().length === 0}
                  onClick={handleSubmit}
                  className="rounded bg-black text-white px-4 py-2 text-sm disabled:opacity-50"
                >
                  Submit assessment
                </button>
              </div>
            )}
          </div>
        ) : null}
      </div>
    </div>
  );
}
