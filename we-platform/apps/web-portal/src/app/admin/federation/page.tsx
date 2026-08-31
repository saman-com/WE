"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { fetchProfile, type UserProfile } from "@/lib/auth";
import {
  assignSchoolAdmin,
  createFederationSchool,
  fetchFederationMetrics,
  fetchFederationPolicies,
  listFederationSchools,
  updateFederationPolicies,
  type FederationMetrics,
  type FederationPolicy,
  type FederationSchool,
} from "@/lib/federation";

function isFederationAdmin(profile: UserProfile): boolean {
  return profile.roles.includes("FederationAdmin");
}

export default function FederationAdminPage() {
  const router = useRouter();
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [schools, setSchools] = useState<FederationSchool[]>([]);
  const [metrics, setMetrics] = useState<FederationMetrics | null>(null);
  const [policies, setPolicies] = useState<FederationPolicy[]>([]);
  const [schoolName, setSchoolName] = useState("");
  const [schoolCode, setSchoolCode] = useState("");
  const [adminUserId, setAdminUserId] = useState("");
  const [selectedSchoolId, setSelectedSchoolId] = useState("");
  const [policyKey, setPolicyKey] = useState("shared-curriculum");
  const [policyValue, setPolicyValue] = useState("enabled");
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  async function reload(token: string) {
    const [loadedSchools, loadedMetrics, loadedPolicies] = await Promise.all([
      listFederationSchools(token),
      fetchFederationMetrics(token),
      fetchFederationPolicies(token),
    ]);
    setSchools(loadedSchools);
    setMetrics(loadedMetrics);
    setPolicies(loadedPolicies);
  }

  useEffect(() => {
    const token = localStorage.getItem("we_access_token");
    if (!token) {
      router.replace("/login");
      return;
    }

    fetchProfile(token)
      .then(async (loaded) => {
        if (!isFederationAdmin(loaded)) {
          router.replace("/dashboard");
          return;
        }

        setProfile(loaded);
        await reload(token);
      })
      .catch(() => {
        localStorage.removeItem("we_access_token");
        setError("Session expired. Please sign in again.");
      })
      .finally(() => setLoading(false));
  }, [router]);

  async function handleCreateSchool(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const token = localStorage.getItem("we_access_token");
    if (!token) {
      return;
    }

    setError(null);
    setSuccess(null);
    try {
      await createFederationSchool(token, { name: schoolName, code: schoolCode });
      setSchoolName("");
      setSchoolCode("");
      await reload(token);
      setSuccess("School tenant provisioned with default configuration.");
    } catch {
      setError("Failed to provision school tenant.");
    }
  }

  async function handleAssignAdmin(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const token = localStorage.getItem("we_access_token");
    if (!token || !selectedSchoolId) {
      return;
    }

    setError(null);
    setSuccess(null);
    try {
      await assignSchoolAdmin(token, selectedSchoolId, { userId: adminUserId });
      setAdminUserId("");
      setSuccess("School admin assigned.");
    } catch {
      setError("Failed to assign school admin.");
    }
  }

  async function handleUpdatePolicy(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const token = localStorage.getItem("we_access_token");
    if (!token) {
      return;
    }

    setError(null);
    setSuccess(null);
    try {
      const updated = await updateFederationPolicies(token, [
        ...policies.filter((item) => item.policyKey !== policyKey),
        { policyKey, policyValue },
      ]);
      setPolicies(updated);
      setSuccess("Federation policy updated.");
    } catch {
      setError("Failed to update federation policy.");
    }
  }

  if (loading) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6">
        <p>Loading federation admin portal...</p>
      </div>
    );
  }

  if (error && !profile) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6">
        <div className="space-y-4 text-center">
          <p className="text-red-600">{error}</p>
          <Link href="/login" className="underline">
            Back to login
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen p-8">
      <div className="max-w-3xl mx-auto space-y-8">
        <div className="flex items-center justify-between">
          <h1 className="text-2xl font-semibold">Federation Administration</h1>
          <Link href="/dashboard" className="text-sm underline">
            Back to dashboard
          </Link>
        </div>

        {error ? <p className="text-red-600">{error}</p> : null}
        {success ? <p className="text-green-700">{success}</p> : null}

        <section className="rounded-lg border border-black/10 p-6 space-y-4">
          <h2 className="font-medium">Cross-school metrics</h2>
          {metrics ? (
            <div className="grid grid-cols-3 gap-4 text-sm">
              <div>
                <p className="text-black/60">Schools</p>
                <p className="text-lg font-medium">{metrics.totalSchools}</p>
              </div>
              <div>
                <p className="text-black/60">Total enrollment</p>
                <p className="text-lg font-medium">{metrics.totalEnrollment}</p>
              </div>
              <div>
                <p className="text-black/60">Average progress</p>
                <p className="text-lg font-medium">{metrics.averageProgressPercent}%</p>
              </div>
            </div>
          ) : (
            <p className="text-sm text-black/60">No metrics available.</p>
          )}
        </section>

        <section className="rounded-lg border border-black/10 p-6 space-y-4">
          <h2 className="font-medium">Provision school tenant</h2>
          <form onSubmit={handleCreateSchool} className="space-y-3">
            <input
              className="w-full border border-black/20 rounded px-3 py-2"
              placeholder="School name"
              value={schoolName}
              onChange={(event) => setSchoolName(event.target.value)}
              required
            />
            <input
              className="w-full border border-black/20 rounded px-3 py-2"
              placeholder="School code"
              value={schoolCode}
              onChange={(event) => setSchoolCode(event.target.value)}
              required
            />
            <button type="submit" className="text-sm underline">
              Create school
            </button>
          </form>
        </section>

        <section className="rounded-lg border border-black/10 p-6 space-y-4">
          <h2 className="font-medium">Schools in federation</h2>
          {schools.length === 0 ? (
            <p className="text-sm text-black/60">No schools provisioned yet.</p>
          ) : (
            <ul className="space-y-2 text-sm">
              {schools.map((school) => (
                <li key={school.tenantId}>
                  {school.name} ({school.code}) — tenant {school.tenantId}
                </li>
              ))}
            </ul>
          )}
        </section>

        <section className="rounded-lg border border-black/10 p-6 space-y-4">
          <h2 className="font-medium">Assign school admin</h2>
          <form onSubmit={handleAssignAdmin} className="space-y-3">
            <select
              className="w-full border border-black/20 rounded px-3 py-2"
              value={selectedSchoolId}
              onChange={(event) => setSelectedSchoolId(event.target.value)}
              required
            >
              <option value="">Select school</option>
              {schools.map((school) => (
                <option key={school.tenantId} value={school.tenantId}>
                  {school.name}
                </option>
              ))}
            </select>
            <input
              className="w-full border border-black/20 rounded px-3 py-2"
              placeholder="Admin user id"
              value={adminUserId}
              onChange={(event) => setAdminUserId(event.target.value)}
              required
            />
            <button type="submit" className="text-sm underline">
              Assign admin
            </button>
          </form>
        </section>

        <section className="rounded-lg border border-black/10 p-6 space-y-4">
          <h2 className="font-medium">Federation policies</h2>
          {policies.length > 0 ? (
            <ul className="text-sm space-y-1">
              {policies.map((policy) => (
                <li key={policy.policyKey}>
                  {policy.policyKey}: {policy.policyValue}
                </li>
              ))}
            </ul>
          ) : (
            <p className="text-sm text-black/60">No policies configured.</p>
          )}
          <form onSubmit={handleUpdatePolicy} className="space-y-3">
            <input
              className="w-full border border-black/20 rounded px-3 py-2"
              placeholder="Policy key"
              value={policyKey}
              onChange={(event) => setPolicyKey(event.target.value)}
              required
            />
            <input
              className="w-full border border-black/20 rounded px-3 py-2"
              placeholder="Policy value"
              value={policyValue}
              onChange={(event) => setPolicyValue(event.target.value)}
              required
            />
            <button type="submit" className="text-sm underline">
              Save policy
            </button>
          </form>
        </section>
      </div>
    </div>
  );
}
