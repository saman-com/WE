"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { fetchProfile, type UserProfile } from "@/lib/auth";
import { ApiError } from "@/lib/api-error";
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
import { useI18n } from "@/i18n/I18nProvider";

function isFederationAdmin(profile: UserProfile): boolean {
  return profile.roles.includes("FederationAdmin");
}

export default function FederationAdminPage() {
  const router = useRouter();
  const { t } = useI18n();
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

    let cancelled = false;
    fetchProfile(token)
      .then(async (loaded) => {
        if (cancelled) {
          return;
        }
        if (!isFederationAdmin(loaded)) {
          router.replace("/dashboard");
          return;
        }

        setProfile(loaded);
        try {
          await reload(token);
        } catch {
          if (!cancelled) {
            setError(t("common.requestFailed"));
          }
        }
      })
      .catch((err: unknown) => {
        if (cancelled) {
          return;
        }
        if (err instanceof ApiError && err.status === 401) {
          localStorage.removeItem("we_access_token");
          setError(t("common.sessionExpired"));
          return;
        }
        setError(t("common.requestFailed"));
      })
      .finally(() => {
        if (!cancelled) {
          setLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [router, t]);

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
      setSuccess(t("admin.federation.provisioned"));
    } catch {
      setError(t("admin.federation.provisionFailed"));
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
      setSuccess(t("admin.federation.adminAssigned"));
    } catch {
      setError(t("admin.federation.assignFailed"));
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
      setSuccess(t("admin.federation.policyUpdated"));
    } catch {
      setError(t("admin.federation.policyUpdateFailed"));
    }
  }

  if (loading) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6">
        <p>{t("admin.federation.loading")}</p>
      </div>
    );
  }

  if (error && !profile) {
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

  return (
    <div className="min-h-screen p-8">
      <div className="max-w-3xl mx-auto space-y-8">
        <div className="flex items-center justify-between">
          <h1 className="text-2xl font-semibold">{t("dashboard.nav.federationAdmin")}</h1>
          <Link href="/dashboard" className="text-sm underline">
            {t("common.backToDashboard")}
          </Link>
        </div>

        {error ? <p className="text-red-600">{error}</p> : null}
        {success ? <p className="text-green-700">{success}</p> : null}

        <section className="rounded-lg border border-black/10 p-6 space-y-4">
          <h2 className="font-medium">{t("admin.federation.metricsTitle")}</h2>
          {metrics ? (
            <div className="grid grid-cols-3 gap-4 text-sm">
              <div>
                <p className="text-black/60">{t("admin.federation.schools")}</p>
                <p className="text-lg font-medium">{metrics.totalSchools}</p>
              </div>
              <div>
                <p className="text-black/60">{t("admin.federation.totalEnrollment")}</p>
                <p className="text-lg font-medium">{metrics.totalEnrollment}</p>
              </div>
              <div>
                <p className="text-black/60">{t("admin.federation.averageProgress")}</p>
                <p className="text-lg font-medium">{metrics.averageProgressPercent}%</p>
              </div>
            </div>
          ) : (
            <p className="text-sm text-black/60">{t("admin.federation.noMetrics")}</p>
          )}
        </section>

        <section className="rounded-lg border border-black/10 p-6 space-y-4">
          <h2 className="font-medium">{t("admin.federation.provisionTitle")}</h2>
          <form onSubmit={handleCreateSchool} className="space-y-3">
            <input
              className="w-full border border-black/20 rounded px-3 py-2"
              placeholder={t("organisation.schoolNamePlaceholder")}
              value={schoolName}
              onChange={(event) => setSchoolName(event.target.value)}
              required
            />
            <input
              className="w-full border border-black/20 rounded px-3 py-2"
              placeholder={t("organisation.schoolCodePlaceholder")}
              value={schoolCode}
              onChange={(event) => setSchoolCode(event.target.value)}
              required
            />
            <button type="submit" className="text-sm underline">
              {t("organisation.createSchool")}
            </button>
          </form>
        </section>

        <section className="rounded-lg border border-black/10 p-6 space-y-4">
          <h2 className="font-medium">{t("admin.federation.schoolsTitle")}</h2>
          {schools.length === 0 ? (
            <p className="text-sm text-black/60">{t("admin.federation.noSchools")}</p>
          ) : (
            <ul className="space-y-2 text-sm">
              {schools.map((school) => (
                <li key={school.tenantId}>
                  {t("admin.federation.schoolLine", {
                    name: school.name,
                    code: school.code,
                    tenantId: school.tenantId,
                  })}
                </li>
              ))}
            </ul>
          )}
        </section>

        <section className="rounded-lg border border-black/10 p-6 space-y-4">
          <h2 className="font-medium">{t("admin.federation.assignTitle")}</h2>
          <form onSubmit={handleAssignAdmin} className="space-y-3">
            <select
              className="w-full border border-black/20 rounded px-3 py-2"
              value={selectedSchoolId}
              onChange={(event) => setSelectedSchoolId(event.target.value)}
              required
            >
              <option value="">{t("admin.federation.selectSchool")}</option>
              {schools.map((school) => (
                <option key={school.tenantId} value={school.tenantId}>
                  {school.name}
                </option>
              ))}
            </select>
            <input
              className="w-full border border-black/20 rounded px-3 py-2"
              placeholder={t("admin.federation.adminUserIdPlaceholder")}
              value={adminUserId}
              onChange={(event) => setAdminUserId(event.target.value)}
              required
            />
            <button type="submit" className="text-sm underline">
              {t("admin.federation.assignAdmin")}
            </button>
          </form>
        </section>

        <section className="rounded-lg border border-black/10 p-6 space-y-4">
          <h2 className="font-medium">{t("admin.federation.policiesTitle")}</h2>
          {policies.length > 0 ? (
            <ul className="text-sm space-y-1">
              {policies.map((policy) => (
                <li key={policy.policyKey}>
                  {policy.policyKey}: {policy.policyValue}
                </li>
              ))}
            </ul>
          ) : (
            <p className="text-sm text-black/60">{t("admin.federation.noPolicies")}</p>
          )}
          <form onSubmit={handleUpdatePolicy} className="space-y-3">
            <input
              className="w-full border border-black/20 rounded px-3 py-2"
              placeholder={t("admin.federation.policyKeyPlaceholder")}
              value={policyKey}
              onChange={(event) => setPolicyKey(event.target.value)}
              required
            />
            <input
              className="w-full border border-black/20 rounded px-3 py-2"
              placeholder={t("admin.federation.policyValuePlaceholder")}
              value={policyValue}
              onChange={(event) => setPolicyValue(event.target.value)}
              required
            />
            <button type="submit" className="text-sm underline">
              {t("admin.federation.savePolicy")}
            </button>
          </form>
        </section>
      </div>
    </div>
  );
}
