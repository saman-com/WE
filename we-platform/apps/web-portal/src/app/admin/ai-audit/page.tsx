"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { fetchProfile, type UserProfile } from "@/lib/auth";
import { searchAiAuditLogs, type AiAuditLogEntry } from "@/lib/ai-gateway";

export default function AiAuditAdminPage() {
  const router = useRouter();
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [logs, setLogs] = useState<AiAuditLogEntry[]>([]);
  const [error, setError] = useState<string | null>(null);
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
        const entries = await searchAiAuditLogs(token);
        setLogs(entries);
      })
      .catch(() => {
        localStorage.removeItem("we_access_token");
        setError("Session expired. Please sign in again.");
      })
      .finally(() => setLoading(false));
  }, [router]);

  async function handleSearch(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const token = localStorage.getItem("we_access_token");
    if (!token) {
      router.replace("/login");
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const entries = await searchAiAuditLogs(token, {
        promptId: promptId.trim() || undefined,
        outcome: outcome.trim() || undefined,
        search: search.trim() || undefined,
      });
      setLogs(entries);
    } catch {
      setError("Unable to load AI audit logs.");
    } finally {
      setLoading(false);
    }
  }

  if (error) {
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

  if (loading && !profile) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6">
        <p>Loading AI audit logs...</p>
      </div>
    );
  }

  return (
    <div className="min-h-screen p-8">
      <div className="max-w-5xl mx-auto space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-semibold">AI Audit Logs</h1>
            <p className="text-sm text-black/70">
              Review AI gateway interactions, outcomes, and safety blocks.
            </p>
          </div>
          <Link href="/dashboard" className="text-sm underline">
            Back to dashboard
          </Link>
        </div>

        <form onSubmit={handleSearch} className="rounded-lg border border-black/10 p-4 grid gap-3 md:grid-cols-4">
          <label className="text-sm space-y-1">
            <span>Prompt id</span>
            <input
              className="w-full border border-black/20 rounded px-2 py-1"
              value={promptId}
              onChange={(event) => setPromptId(event.target.value)}
              placeholder="assessment-feedback"
            />
          </label>
          <label className="text-sm space-y-1">
            <span>Outcome</span>
            <select
              className="w-full border border-black/20 rounded px-2 py-1"
              value={outcome}
              onChange={(event) => setOutcome(event.target.value)}
            >
              <option value="">All</option>
              <option value="success">success</option>
              <option value="blocked">blocked</option>
              <option value="validation_failed">validation_failed</option>
            </select>
          </label>
          <label className="text-sm space-y-1 md:col-span-2">
            <span>Search</span>
            <input
              className="w-full border border-black/20 rounded px-2 py-1"
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder="Prompt, scope, or block reason"
            />
          </label>
          <button
            type="submit"
            className="md:col-span-4 justify-self-start rounded border border-black/20 px-3 py-1 text-sm"
          >
            Apply filters
          </button>
        </form>

        <div className="rounded-lg border border-black/10 overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-black/5 text-left">
              <tr>
                <th className="p-3">Timestamp</th>
                <th className="p-3">Caller</th>
                <th className="p-3">Prompt</th>
                <th className="p-3">Outcome</th>
                <th className="p-3">Provider</th>
                <th className="p-3">Block reason</th>
              </tr>
            </thead>
            <tbody>
              {logs.length === 0 ? (
                <tr>
                  <td className="p-3 text-black/60" colSpan={6}>
                    No audit log entries found.
                  </td>
                </tr>
              ) : (
                logs.map((entry) => (
                  <tr key={entry.id} className="border-t border-black/10 align-top">
                    <td className="p-3 whitespace-nowrap">
                      {new Date(entry.createdAt).toLocaleString()}
                    </td>
                    <td className="p-3 font-mono text-xs">{entry.callerUserId}</td>
                    <td className="p-3">
                      <div>{entry.promptId}</div>
                      <div className="text-xs text-black/60">v{entry.promptVersion}</div>
                    </td>
                    <td className="p-3">{entry.outcome}</td>
                    <td className="p-3">{entry.providerName}</td>
                    <td className="p-3 text-black/70">{entry.blockReason ?? "—"}</td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
