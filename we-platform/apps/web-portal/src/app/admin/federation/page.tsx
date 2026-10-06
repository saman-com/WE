"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { DataText } from "@/components/data-text";
import { FocusCard, LearningFrame, PrimaryButton } from "@/components/learning-frame";
import { fetchProfile, listDirectoryUsers, type DirectoryUser, type UserProfile } from "@/lib/auth";
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
  const { t, translateError } = useI18n();
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
  const [tab, setTab] = useState("overview");
  const [people, setPeople] = useState<DirectoryUser[]>([]);

  function namedCode(prefix: string, code: string) {
    const key = `${prefix}.${code}`;
    const label = t(key);
    return label === key ? code : label;
  }

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
        listDirectoryUsers(token).then(setPeople).catch(() => setPeople([]));
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
    } catch (err) {
      if (err instanceof ApiError && err.code === "federation.duplicate_school_code") {
        setError(translateError(err.code));
        return;
      }
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

  if (!profile) {
    return null;
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
      tabs={[
        { id: "overview", label: t("admin.federation.tab.overview") },
        { id: "add", label: t("admin.federation.tab.addSchool") },
        { id: "assign", label: t("admin.federation.tab.assign") },
        { id: "policy", label: t("admin.federation.tab.policy") },
      ]}
      activeTab={tab}
      onTabChange={setTab}
    >
      <div className="space-y-6">
        <h1 className="text-2xl font-semibold">{t("dashboard.nav.federationAdmin")}</h1>
        {error ? (
          <p role="status" className="text-sm text-red-700">
            {error}
          </p>
        ) : null}
        {success ? (
          <p role="status" className="text-sm text-green-800">
            {success}
          </p>
        ) : null}

        {tab === "overview" ? (
          <>
            <FocusCard>
              <h2 className="font-medium">{t("admin.federation.metricsTitle")}</h2>
              {metrics ? (
                <div className="mt-4 grid grid-cols-1 gap-4 text-sm sm:grid-cols-3">
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
                <p className="mt-3 text-sm text-black/60">{t("admin.federation.noMetrics")}</p>
              )}
            </FocusCard>
            <FocusCard>
              <h2 className="font-medium">{t("admin.federation.schoolsTitle")}</h2>
              {schools.length === 0 ? (
                <p className="mt-3 text-sm text-black/60">{t("admin.federation.noSchools")}</p>
              ) : (
                <ul className="mt-3 space-y-2 text-sm">
                  {schools.map((school) => (
                    <li key={school.tenantId}>
                      <DataText>{`${school.name} (${school.code})`}</DataText>
                    </li>
                  ))}
                </ul>
              )}
            </FocusCard>
            <FocusCard>
              <h2 className="font-medium">{t("admin.federation.policiesTitle")}</h2>
              {policies.length > 0 ? (
                <ul className="mt-3 space-y-1 text-sm">
                  {policies.map((policy) => (
                    <li key={policy.policyKey}>
                      {`${namedCode("admin.federation.policyKey", policy.policyKey)}: ${namedCode("admin.federation.policyValue", policy.policyValue)}`}
                    </li>
                  ))}
                </ul>
              ) : (
                <p className="mt-3 text-sm text-black/60">{t("admin.federation.noPolicies")}</p>
              )}
            </FocusCard>
          </>
        ) : null}

        {tab === "add" ? (
          <FocusCard>
            <h2 className="font-medium">{t("admin.federation.provisionTitle")}</h2>
            <form onSubmit={handleCreateSchool} className="mt-4 space-y-3">
              <label className="block space-y-1 text-sm">
                <span>{t("admin.federation.schoolName")}</span>
                <input
                  className="w-full rounded-lg border border-black/10 bg-white px-3 py-2"
                  value={schoolName}
                  onChange={(event) => setSchoolName(event.target.value)}
                  required
                />
              </label>
              <label className="block space-y-1 text-sm">
                <span>{t("admin.federation.schoolCode")}</span>
                <input
                  className="w-full rounded-lg border border-black/10 bg-white px-3 py-2"
                  value={schoolCode}
                  onChange={(event) => setSchoolCode(event.target.value)}
                  required
                />
              </label>
              <PrimaryButton type="submit">{t("organisation.createSchool")}</PrimaryButton>
            </form>
          </FocusCard>
        ) : null}

        {tab === "assign" ? (
          <FocusCard>
            <h2 className="font-medium">{t("admin.federation.assignTitle")}</h2>
            <form onSubmit={handleAssignAdmin} className="mt-4 space-y-3">
              <label className="block space-y-1 text-sm">
                <span>{t("admin.federation.selectSchool")}</span>
                <select
                  className="w-full rounded-lg border border-black/10 bg-white px-3 py-2"
                  value={selectedSchoolId}
                  onChange={(event) => setSelectedSchoolId(event.target.value)}
                  required
                >
                  <option value="">{t("admin.federation.selectSchool")}</option>
                  {schools.map((school) => (
                    <option key={school.tenantId} value={school.tenantId} dir="auto">
                      {school.name}
                    </option>
                  ))}
                </select>
              </label>
              <label className="block space-y-1 text-sm">
                <span>{t("admin.federation.adminUserId")}</span>
                <select
                  className="w-full rounded-lg border border-black/10 bg-white px-3 py-2"
                  value={adminUserId}
                  onChange={(event) => setAdminUserId(event.target.value)}
                  required
                >
                  <option value="">{t("admin.federation.adminUserId")}</option>
                  {people.map((person) => (
                    <option key={person.id} value={person.id} dir="auto">
                      {person.name}
                    </option>
                  ))}
                </select>
              </label>
              <PrimaryButton type="submit">{t("admin.federation.assignAdmin")}</PrimaryButton>
            </form>
          </FocusCard>
        ) : null}

        {tab === "policy" ? (
          <FocusCard>
            <h2 className="font-medium">{t("admin.federation.policiesTitle")}</h2>
            <form onSubmit={handleUpdatePolicy} className="mt-4 space-y-3">
              <label className="block space-y-1 text-sm">
                <span>{t("admin.federation.policyKey")}</span>
                <input
                  className="w-full rounded-lg border border-black/10 bg-white px-3 py-2"
                  value={policyKey}
                  onChange={(event) => setPolicyKey(event.target.value)}
                  required
                />
              </label>
              <label className="block space-y-1 text-sm">
                <span>{t("admin.federation.policyValue")}</span>
                <input
                  className="w-full rounded-lg border border-black/10 bg-white px-3 py-2"
                  value={policyValue}
                  onChange={(event) => setPolicyValue(event.target.value)}
                  required
                />
              </label>
              <PrimaryButton type="submit">{t("admin.federation.savePolicy")}</PrimaryButton>
            </form>
          </FocusCard>
        ) : null}
      </div>
    </LearningFrame>
  );
}
