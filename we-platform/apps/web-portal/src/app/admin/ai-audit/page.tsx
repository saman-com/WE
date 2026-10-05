"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { FocusCard, LearningFrame, PrimaryButton } from "@/components/learning-frame";
import { fetchProfile, listDirectoryUsers, personName, type DirectoryUser, type UserProfile } from "@/lib/auth";
import { searchAiAuditLogs, type AiAuditLogEntry } from "@/lib/ai-gateway";
import { useI18n } from "@/i18n/I18nProvider";

export default function AiAuditAdminPage() {
  const router = useRouter();
  const { t } = useI18n();
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [directory, setDirectory] = useState<DirectoryUser[]>([]);
  const [logs, setLogs] = useState<AiAuditLogEntry[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [noticeError, setNoticeError] = useState(false);
  const [loading, setLoading] = useState(true);
  const [promptId, setPromptId] = useState("");
  const [outcome, setOutcome] = useState("");
  const [search, setSearch] = useState("");

  useEffect(() => {
    const token = localStorage.getItem("we_access_token");
    if (!token) {
      router.replace("/login");
      return;
    }

    fetchProfile(token)
      .then(async (loaded) => {
        if (!loaded.roles.includes("SystemAdministrator")) {
          router.replace("/dashboard");
          return;
        }

        setProfile(loaded);
        listDirectoryUsers(token).then(setDirectory).catch(() => setDirectory([]));
        try {
          const entries = await searchAiAuditLogs(token);
          setLogs(entries);
          setNotice(t("admin.aiAudit.shown", { count: String(entries.length) }));
        } catch {
          setNoticeError(true);
          setNotice(t("admin.aiAudit.loadError"));
        }
      })
      .catch(() => {
        localStorage.removeItem("we_access_token");
        setError(t("common.sessionExpired"));
      })
      .finally(() => setLoading(false));
  }, [router, t]);

  async function handleSearch(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const token = localStorage.getItem("we_access_token");
    if (!token) {
      router.replace("/login");
      return;
    }

    setLoading(true);
    setNotice(null);
    setNoticeError(false);

    try {
      const entries = await searchAiAuditLogs(token, {
        promptId: promptId.trim() || undefined,
        outcome: outcome.trim() || undefined,
        search: search.trim() || undefined,
      });
      setLogs(entries);
      setNotice(t("admin.aiAudit.shown", { count: String(entries.length) }));
    } catch {
      setNoticeError(true);
      setNotice(t("admin.aiAudit.loadError"));
    } finally {
      setLoading(false);
    }
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

  if (loading && !profile) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6">
        <p>{t("admin.aiAudit.loading")}</p>
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
    >
      <div className="space-y-6">
        <h1 className="text-2xl font-semibold">{t("dashboard.nav.aiAuditLogs")}</h1>
        {notice ? (
          <p role="status" className={`text-sm ${noticeError ? "text-red-700" : "text-green-800"}`}>
            {notice}
          </p>
        ) : null}

        <FocusCard>
          <form onSubmit={handleSearch} className="space-y-3">
            <label className="block space-y-1 text-sm">
              <span>{t("admin.aiAudit.promptId")}</span>
              <input
                className="w-full rounded-lg border border-black/10 bg-white px-3 py-2"
                value={promptId}
                onChange={(event) => setPromptId(event.target.value)}
              />
            </label>
            <label className="block space-y-1 text-sm">
              <span>{t("admin.aiAudit.outcome")}</span>
              <select
                className="w-full rounded-lg border border-black/10 bg-white px-3 py-2"
                value={outcome}
                onChange={(event) => setOutcome(event.target.value)}
              >
                <option value="">{t("admin.aiAudit.all")}</option>
                <option value="success">success</option>
                <option value="blocked">blocked</option>
                <option value="validation_failed">validation_failed</option>
              </select>
            </label>
            <label className="block space-y-1 text-sm">
              <span>{t("admin.aiAudit.search")}</span>
              <input
                className="w-full rounded-lg border border-black/10 bg-white px-3 py-2"
                value={search}
                onChange={(event) => setSearch(event.target.value)}
              />
            </label>
            <PrimaryButton type="submit" disabled={loading}>
              {t("admin.aiAudit.applyFilters")}
            </PrimaryButton>
          </form>
        </FocusCard>

        {logs.length === 0 ? (
          <p className="text-sm text-black/60">{t("admin.aiAudit.noEntries")}</p>
        ) : (
          <>
            <ul className="space-y-3 md:hidden">
              {logs.map((entry) => (
                <li key={entry.id}>
                  <FocusCard>
                    <p className="font-medium">{entry.promptId}</p>
                    <p className="text-sm">{personName(directory, entry.callerUserId, t("admin.aiAudit.unknownCaller"))}</p>
                    <p className="text-sm">{entry.outcome}</p>
                    <p className="text-sm text-black/60">{entry.providerName}</p>
                    <p className="text-sm text-black/60">
                      {new Date(entry.createdAt).toLocaleString()}
                    </p>
                  </FocusCard>
                </li>
              ))}
            </ul>
            <div className="hidden overflow-x-auto rounded-xl bg-black/[0.04] md:block">
              <table className="w-full text-sm">
                <thead className="text-start">
                  <tr>
                    <th className="p-3 text-start">{t("admin.aiAudit.col.timestamp")}</th>
                    <th className="p-3 text-start">{t("admin.aiAudit.col.caller")}</th>
                    <th className="p-3 text-start">{t("admin.aiAudit.col.prompt")}</th>
                    <th className="p-3 text-start">{t("admin.aiAudit.outcome")}</th>
                    <th className="p-3 text-start">{t("admin.aiAudit.col.provider")}</th>
                    <th className="p-3 text-start">{t("admin.aiAudit.col.blockReason")}</th>
                  </tr>
                </thead>
                <tbody>
                  {logs.map((entry) => (
                    <tr key={entry.id} className="border-t border-black/10 align-top">
                      <td className="p-3 whitespace-nowrap">
                        {new Date(entry.createdAt).toLocaleString()}
                      </td>
                      <td className="p-3">
                        {personName(directory, entry.callerUserId, t("admin.aiAudit.unknownCaller"))}
                      </td>
                      <td className="p-3">{entry.promptId}</td>
                      <td className="p-3">{entry.outcome}</td>
                      <td className="p-3">{entry.providerName}</td>
                      <td className="p-3 text-black/70">{entry.blockReason ?? "—"}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </>
        )}
      </div>
    </LearningFrame>
  );
}
