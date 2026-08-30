"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { fetchProfile, type UserProfile } from "@/lib/auth";
import {
  fetchConversation,
  fetchMessageInbox,
  sendMessage,
  type Conversation,
  type MessageInboxThread,
} from "@/lib/messaging";
import { fetchLinkedChildren, type ParentChildLink } from "@/lib/parent-workspace";

function isParent(profile: UserProfile): boolean {
  return profile.roles.includes("Parent");
}

export default function ParentMessagesPage() {
  const router = useRouter();
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [children, setChildren] = useState<ParentChildLink[]>([]);
  const [threads, setThreads] = useState<MessageInboxThread[]>([]);
  const [selectedThread, setSelectedThread] = useState<MessageInboxThread | null>(null);
  const [conversation, setConversation] = useState<Conversation | null>(null);
  const [newStudentId, setNewStudentId] = useState("");
  const [newTeacherId, setNewTeacherId] = useState("");
  const [draft, setDraft] = useState("");
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const token = localStorage.getItem("we_access_token");
    if (!token) {
      router.replace("/login");
      return;
    }

    fetchProfile(token)
      .then(async (loaded) => {
        if (!isParent(loaded)) {
          router.replace("/dashboard");
          return;
        }
        setProfile(loaded);
        const linked = await fetchLinkedChildren(token);
        setChildren(linked);
        if (linked.length > 0) {
          setNewStudentId(linked[0].studentUserId);
        }
        const inbox = await fetchMessageInbox(token);
        setThreads(inbox.threads);
      })
      .catch(() => {
        localStorage.removeItem("we_access_token");
        setError("Session expired. Please sign in again.");
      });
  }, [router]);

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
      setError("Unable to load this conversation.");
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
      setError("Select a child and enter the teacher user ID.");
      return;
    }

    try {
      await sendMessage(token, studentUserId, recipientUserId, draft.trim());
      setDraft("");
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
    } catch {
      setError("Unable to send message.");
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

  if (!profile) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6">
        <p>Loading messages...</p>
      </div>
    );
  }

  return (
    <div className="min-h-screen p-8">
      <div className="max-w-3xl mx-auto space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-semibold">School messages</h1>
            <p className="text-sm text-black/60 mt-1">
              Communicate with your child&apos;s teachers in a student-specific context.
            </p>
          </div>
          <Link href="/parent" className="text-sm underline">
            Parent workspace
          </Link>
        </div>

        <div className="rounded-lg border border-black/10 p-6 space-y-3">
          <h2 className="font-medium">Conversations</h2>
          {threads.length === 0 ? (
            <p className="text-sm text-black/60">No messages yet.</p>
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
                    Child {thread.studentUserId.slice(0, 8)} — Teacher{" "}
                    {thread.teacherUserId.slice(0, 8)}
                  </button>
                </li>
              ))}
            </ul>
          )}
        </div>

        {conversation ? (
          <div className="rounded-lg border border-black/10 p-6 space-y-3">
            <h2 className="font-medium">Message history</h2>
            <ul className="space-y-3">
              {conversation.messages.map((message) => (
                <li key={message.id} className="border border-black/10 rounded p-3">
                  <p className="text-xs text-black/60">
                    {message.senderRole} — {new Date(message.createdAt).toLocaleString()}
                  </p>
                  <p className="text-sm mt-1">{message.body}</p>
                </li>
              ))}
            </ul>
          </div>
        ) : null}

        <form onSubmit={handleSend} className="rounded-lg border border-black/10 p-6 space-y-3">
          <h2 className="font-medium">
            {selectedThread ? "Reply" : "New message to teacher"}
          </h2>
          {!selectedThread ? (
            <>
              <label className="block text-sm">
                Child
                <select
                  value={newStudentId}
                  onChange={(event) => setNewStudentId(event.target.value)}
                  className="mt-1 block w-full rounded border border-black/20 px-3 py-2"
                >
                  {children.map((child) => (
                    <option key={child.studentUserId} value={child.studentUserId}>
                      Child {child.studentUserId.slice(0, 8)}
                    </option>
                  ))}
                </select>
              </label>
              <label className="block text-sm">
                Teacher user ID
                <input
                  value={newTeacherId}
                  onChange={(event) => setNewTeacherId(event.target.value)}
                  className="mt-1 block w-full rounded border border-black/20 px-3 py-2"
                  placeholder="Teacher user ID from your school"
                />
              </label>
            </>
          ) : null}
          <label className="block text-sm">
            Message
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
            Send message
          </button>
        </form>
      </div>
    </div>
  );
}
