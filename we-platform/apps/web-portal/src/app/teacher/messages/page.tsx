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

function isTeacher(profile: UserProfile): boolean {
  return profile.roles.includes("Teacher");
}

export default function TeacherMessagesPage() {
  const router = useRouter();
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [threads, setThreads] = useState<MessageInboxThread[]>([]);
  const [selectedThread, setSelectedThread] = useState<MessageInboxThread | null>(null);
  const [conversation, setConversation] = useState<Conversation | null>(null);
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
        if (!isTeacher(loaded)) {
          router.replace("/dashboard");
          return;
        }
        setProfile(loaded);
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
        thread.parentUserId
      );
      setConversation(loaded);
    } catch {
      setError("Unable to load this conversation.");
    }
  }

  async function handleReply(event: React.FormEvent) {
    event.preventDefault();
    const token = localStorage.getItem("we_access_token");
    if (!token || !selectedThread || !draft.trim()) {
      return;
    }

    try {
      await sendMessage(
        token,
        selectedThread.studentUserId,
        selectedThread.parentUserId,
        draft.trim()
      );
      setDraft("");
      const inbox = await fetchMessageInbox(token);
      setThreads(inbox.threads);
      const refreshed =
        inbox.threads.find(
          (item) =>
            item.studentUserId === selectedThread.studentUserId &&
            item.parentUserId === selectedThread.parentUserId
        ) ?? selectedThread;
      await openThread(refreshed);
    } catch {
      setError("Unable to send reply.");
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
            <h1 className="text-2xl font-semibold">Parent messages</h1>
            <p className="text-sm text-black/60 mt-1">
              Messages from parents of students in your classes.
            </p>
          </div>
          <Link href="/teacher" className="text-sm underline">
            Teacher workspace
          </Link>
        </div>

        <div className="rounded-lg border border-black/10 p-6 space-y-3">
          <h2 className="font-medium">Inbox</h2>
          {threads.length === 0 ? (
            <p className="text-sm text-black/60">No parent messages yet.</p>
          ) : (
            <ul className="space-y-2">
              {threads.map((thread) => (
                <li key={`${thread.studentUserId}-${thread.parentUserId}`}>
                  <button
                    type="button"
                    onClick={() => openThread(thread)}
                    className={`w-full text-left rounded border px-3 py-2 text-sm ${
                      selectedThread?.parentUserId === thread.parentUserId &&
                      selectedThread?.studentUserId === thread.studentUserId
                        ? "border-black bg-black text-white"
                        : "border-black/20"
                    }`}
                  >
                    <span className="font-medium">{thread.latestMessage.body}</span>
                    <span className="block text-xs opacity-70 mt-1">
                      Student {thread.studentUserId.slice(0, 8)} — Parent{" "}
                      {thread.parentUserId.slice(0, 8)} — {thread.messageCount} message
                      {thread.messageCount === 1 ? "" : "s"}
                    </span>
                  </button>
                </li>
              ))}
            </ul>
          )}
        </div>

        {conversation ? (
          <>
            <div className="rounded-lg border border-black/10 p-6 space-y-3">
              <h2 className="font-medium">Conversation</h2>
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

            <form onSubmit={handleReply} className="rounded-lg border border-black/10 p-6 space-y-3">
              <h2 className="font-medium">Reply</h2>
              <textarea
                value={draft}
                onChange={(event) => setDraft(event.target.value)}
                className="block w-full rounded border border-black/20 px-3 py-2 min-h-24 text-sm"
                required
              />
              <button
                type="submit"
                className="rounded bg-black text-white px-4 py-2 text-sm"
              >
                Send reply
              </button>
            </form>
          </>
        ) : null}
      </div>
    </div>
  );
}
