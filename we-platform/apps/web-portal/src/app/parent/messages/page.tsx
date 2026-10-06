"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { fetchProfile, listDirectoryUsers, personName, type DirectoryUser, type UserProfile } from "@/lib/auth";
import { roleHome } from "@/lib/role-home";
import { ApiError } from "@/lib/api-error";
import {
  fetchConversation,
  fetchMessageInbox,
  sendMessage,
  type Conversation,
  type MessageInboxThread,
} from "@/lib/messaging";
import { fetchLinkedChildren, type ParentChildLink } from "@/lib/parent-workspace";
import { DataText } from "@/components/data-text";
import { useI18n } from "@/i18n/I18nProvider";

function isParent(profile: UserProfile): boolean {
  return profile.roles.includes("Parent");
}

export default function ParentMessagesPage() {
  const router = useRouter();
  const { t, translateError } = useI18n();
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [children, setChildren] = useState<ParentChildLink[]>([]);
  const [people, setPeople] = useState<DirectoryUser[]>([]);
  const [threads, setThreads] = useState<MessageInboxThread[]>([]);
  const [selectedThread, setSelectedThread] = useState<MessageInboxThread | null>(null);
  const [conversation, setConversation] = useState<Conversation | null>(null);
  const [newStudentId, setNewStudentId] = useState("");
  const [newTeacherId, setNewTeacherId] = useState("");
  const [draft, setDraft] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [formError, setFormError] = useState<string | null>(null);

  useEffect(() => {
    const token = localStorage.getItem("we_access_token");
    if (!token) {
      router.replace("/login");
      return;
    }

    fetchProfile(token)
      .then(async (loaded) => {
        if (!isParent(loaded)) {
          router.replace(roleHome(loaded.roles));
          return;
        }
        setProfile(loaded);
        const [linked, directory] = await Promise.all([
          fetchLinkedChildren(token),
          listDirectoryUsers(token).catch(() => [] as DirectoryUser[]),
        ]);
        setPeople(directory);
        setChildren(linked);
        if (linked.length > 0) {
          setNewStudentId(linked[0].studentUserId);
        }
        const inbox = await fetchMessageInbox(token);
        setThreads(inbox.threads);
      })
      .catch(() => {
        localStorage.removeItem("we_access_token");
        setError(t("common.sessionExpired"));
      });
  }, [router, t]);

  function displayName(userId: string): string {
    return personName(people, userId, t("organisation.manage.unknownPerson"));
  }

  async function openThread(thread: MessageInboxThread) {
    const token = localStorage.getItem("we_access_token");
    if (!token) {
      return;
    }

    setSelectedThread(thread);
    setConversation(null);
    try {
      const loaded = await fetchConversation(
        token,
        thread.studentUserId,
        thread.teacherUserId
      );
      setConversation(loaded);
    } catch {
      setError(t("parent.messages.loadConversationError"));
    }
  }

  async function handleSend(event: React.FormEvent) {
    event.preventDefault();
    const token = localStorage.getItem("we_access_token");
    if (!token || !draft.trim()) {
      return;
    }

    const studentUserId = selectedThread?.studentUserId ?? newStudentId;
    const recipientUserId = selectedThread?.teacherUserId ?? newTeacherId;
    if (!studentUserId || !recipientUserId) {
      setError(t("parent.messages.selectChildError"));
      return;
    }

    try {
      await sendMessage(token, studentUserId, recipientUserId, draft.trim());
      setDraft("");
      setFormError(null);
      const inbox = await fetchMessageInbox(token);
      setThreads(inbox.threads);
      const thread =
        inbox.threads.find(
          (item) =>
            item.studentUserId === studentUserId && item.teacherUserId === recipientUserId
        ) ?? null;
      if (thread) {
        await openThread(thread);
      }
    } catch (err) {
      if (err instanceof ApiError && err.code === "messages.invalid_teacher") {
        setFormError(translateError(err.code));
        return;
      }
      setFormError(t("parent.messages.sendError"));
    }
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
        <p>{t("parent.messages.loading")}</p>
      </div>
    );
  }

  return (
    <div className="min-h-screen p-8">
      <div className="max-w-3xl mx-auto space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-semibold">{t("dashboard.nav.schoolMessages")}</h1>
            <p className="text-sm text-black/60 mt-1">
              {t("parent.messages.subtitle")}
            </p>
          </div>
          <Link href="/parent" className="text-sm underline">
            {t("parent.home.title")}
          </Link>
        </div>

        <div className="rounded-lg border border-black/10 p-6 space-y-3">
          <h2 className="font-medium">{t("parent.messages.conversations")}</h2>
          {threads.length === 0 ? (
            <p className="text-sm text-black/60">{t("parent.messages.empty")}</p>
          ) : (
            <ul className="space-y-2">
              {threads.map((thread) => (
                <li key={`${thread.studentUserId}-${thread.teacherUserId}`}>
                  <button
                    type="button"
                    onClick={() => openThread(thread)}
                    className={`w-full text-left rounded border px-3 py-2 text-sm ${
                      selectedThread?.teacherUserId === thread.teacherUserId &&
                      selectedThread?.studentUserId === thread.studentUserId
                        ? "border-black bg-black text-white"
                        : "border-black/20"
                    }`}
                  >
                    {t("parent.messages.threadLabel", {
                      child: displayName(thread.studentUserId),
                      teacher: displayName(thread.teacherUserId),
                    })}
                  </button>
                </li>
              ))}
            </ul>
          )}
        </div>

        {conversation ? (
          <div className="rounded-lg border border-black/10 p-6 space-y-3">
            <h2 className="font-medium">{t("parent.messages.history")}</h2>
            <ul className="space-y-3">
              {conversation.messages.map((message) => (
                <li key={message.id} className="border border-black/10 rounded p-3">
                  <p className="text-xs text-black/60">
                    {message.senderRole} — {new Date(message.createdAt).toLocaleString()}
                  </p>
                  <p className="text-sm mt-1">
                    <DataText>{message.body}</DataText>
                  </p>
                </li>
              ))}
            </ul>
          </div>
        ) : null}

        <form onSubmit={handleSend} className="rounded-lg border border-black/10 p-6 space-y-3">
          <h2 className="font-medium">
            {selectedThread
              ? t("parent.messages.reply")
              : t("parent.messages.newMessage")}
          </h2>
          {formError ? <p className="text-sm text-red-700">{formError}</p> : null}
          {!selectedThread ? (
            <>
              <label className="block text-sm">
                {t("parent.messages.childLabel")}
                <select
                  value={newStudentId}
                  onChange={(event) => setNewStudentId(event.target.value)}
                  className="mt-1 block w-full rounded border border-black/20 px-3 py-2"
                >
                  {children.map((child) => (
                    <option key={child.studentUserId} value={child.studentUserId}>
                      {displayName(child.studentUserId)}
                    </option>
                  ))}
                </select>
              </label>
              <label className="block text-sm">
                {t("parent.messages.teacherIdLabel")}
                <select
                  value={newTeacherId}
                  onChange={(event) => setNewTeacherId(event.target.value)}
                  className="mt-1 block w-full rounded border border-black/20 px-3 py-2"
                  required
                >
                  <option value="">{t("parent.messages.teacherIdLabel")}</option>
                  {people
                    .filter((person) => person.roles.includes("Teacher"))
                    .map((person) => (
                      <option key={person.id} value={person.id}>
                        {person.name}
                      </option>
                    ))}
                </select>
              </label>
            </>
          ) : null}
          <label className="block text-sm">
            {t("parent.messages.messageLabel")}
            <textarea
              value={draft}
              onChange={(event) => setDraft(event.target.value)}
              className="mt-1 block w-full rounded border border-black/20 px-3 py-2 min-h-24"
              required
            />
          </label>
          <button
            type="submit"
            className="rounded bg-black text-white px-4 py-2 text-sm"
          >
            {t("parent.messages.send")}
          </button>
        </form>
      </div>
    </div>
  );
}
