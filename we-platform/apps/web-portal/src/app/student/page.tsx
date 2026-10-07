"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { fetchProfile, type UserProfile } from "@/lib/auth";
import { roleHome } from "@/lib/role-home";
import { ApiError } from "@/lib/api-error";
import { DataText } from "@/components/data-text";
import {
  FocusCard,
  LearningFrame,
  PrimaryLink,
  type LearningTab,
} from "@/components/learning-frame";
import {
  growthPoints,
  latestNote,
  latestSkillReads,
  levelFromMark,
  nextAssessment,
  shakySkill,
  taskHref,
  todayHeadline,
  upcomingDue,
  type LatestNote,
  type SkillLevel,
  type SkillRead,
} from "@/lib/student-focus";
import {
  fetchStudentWorkspace,
  type StudentWorkspace,
  type StudentWorkspaceAssessment,
} from "@/lib/student-workspace";
import { useI18n } from "@/i18n/I18nProvider";

type ScreenId = "today" | "skills" | "next" | "notes" | "growth";

function isStudent(profile: UserProfile): boolean {
  return profile.roles.includes("Student");
}

function firstName(name: string): string {
  const [given] = name.trim().split(/\s+/);
  return given || name;
}

function isLocalPreview(): boolean {
  if (typeof window === "undefined") {
    return false;
  }
  const host = window.location.hostname;
  const local = host === "localhost" || host === "127.0.0.1";
  return local && new URLSearchParams(window.location.search).get("preview") === "1";
}

const previewProfile: UserProfile = {
  id: "preview-student",
  email: "student@school.local",
  name: "Demo Student",
  roles: ["Student"],
};

const previewWorkspace: StudentWorkspace = {
  studentUserId: "preview-student",
  assessments: [
    {
      id: "algebra-check",
      organisationId: "org",
      classId: "class",
      className: "Year 11 Mathematics",
      title: "Algebra check",
      dueAt: "2026-10-09T09:00:00Z",
      learningObjectiveIds: [],
      hasSubmitted: false,
      submittedAt: null,
    },
  ],
  feedback: [
    {
      evidenceId: "sheet",
      assessmentId: "sheet-1",
      title: "Algebra sheet",
      approvedAt: "2026-09-28T00:00:00Z",
      microSkillMarks: [
        { microSkillId: "read", mark: 5, feedback: "Read an equation" },
        { microSkillId: "substitute", mark: 4, feedback: "Substitute a value" },
        { microSkillId: "inverse", mark: 3, feedback: "Use inverse operations" },
        {
          microSkillId: "isolate",
          mark: 2,
          feedback: "Getting the letter alone is the next piece.",
        },
      ],
    },
  ],
  timeline: [
    {
      id: "timeline-1",
      title: "Algebra sheet approved",
      recordedAt: "2026-09-28T00:00:00Z",
    },
  ],
};

export default function StudentWorkspacePage() {
  const router = useRouter();
  const { t } = useI18n();
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [workspace, setWorkspace] = useState<StudentWorkspace | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [screen, setScreen] = useState<ScreenId>("today");

  useEffect(() => {
    if (isLocalPreview()) {
      setProfile(previewProfile);
      setWorkspace(previewWorkspace);
      return;
    }

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
        if (!isStudent(loaded)) {
          router.replace(roleHome(loaded.roles));
          return;
        }
        setProfile(loaded);
        const data = await fetchStudentWorkspace(token, loaded.id);
        if (!cancelled) {
          setWorkspace(data);
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
      });

    return () => {
      cancelled = true;
    };
  }, [router, t]);

  function handleSignOut() {
    localStorage.removeItem("we_access_token");
    router.push("/login");
  }

  if (error) {
    return (
      <div className="we-learning flex items-center justify-center p-6">
        <div className="space-y-4 text-center">
          <p className="text-red-700">{error}</p>
          <Link href="/login" className="underline">
            {t("common.backToLogin")}
          </Link>
        </div>
      </div>
    );
  }

  if (!profile || !workspace) {
    return (
      <div className="we-learning flex items-center justify-center p-6">
        <p>{t("student.home.loading")}</p>
      </div>
    );
  }

  const tabs: LearningTab[] = [
    { id: "today", label: t("student.focus.today") },
    { id: "skills", label: t("student.focus.skills") },
    { id: "next", label: t("student.focus.next") },
    { id: "notes", label: t("student.focus.notes") },
    { id: "growth", label: t("student.focus.growth") },
  ];
  const reads = latestSkillReads(workspace.feedback);
  const focus = shakySkill(reads);
  const task = nextAssessment(workspace.assessments);
  const note = latestNote(workspace.feedback);
  const levelLabel = (level: SkillLevel) => t(`student.focus.level.${level}`);

  return (
    <LearningFrame
      eyebrow="WE"
      title={firstName(profile.name)}
      onSignOut={handleSignOut}
      signOutLabel={t("common.signOut")}
      tabs={tabs}
      activeTab={screen}
      onTabChange={(id) => setScreen(id as ScreenId)}
    >
      {screen === "today" ? (
        <Today
          workspace={workspace}
          task={task}
          focus={focus}
          note={note}
          levelLabel={levelLabel}
        />
      ) : null}
      {screen === "skills" ? (
        <Skills reads={reads} levelLabel={levelLabel} onOpenNext={() => setScreen("next")} />
      ) : null}
      {screen === "next" ? <Next focus={focus} task={task} /> : null}
      {screen === "notes" ? <Notes note={note} levelLabel={levelLabel} /> : null}
      {screen === "growth" ? <Growth workspace={workspace} levelLabel={levelLabel} /> : null}
    </LearningFrame>
  );
}

function Today({
  workspace,
  task,
  focus,
  note,
  levelLabel,
}: {
  workspace: StudentWorkspace;
  task: StudentWorkspaceAssessment | null;
  focus: SkillRead | null;
  note: LatestNote | null;
  levelLabel: (level: SkillLevel) => string;
}) {
  const { t, locale } = useI18n();
  const due = upcomingDue(workspace.assessments);
  const lead = todayHeadline(task, focus, note?.sentence ?? null);
  const headline =
    lead.kind === "twoThings" ? (
      t("student.focus.twoThings", {
        day: new Date(lead.dueAt).toLocaleDateString(locale, { weekday: "long" }),
      })
    ) : lead.kind === "oneDue" ? (
      <>
        <DataText>{lead.title}</DataText> {t("student.focus.oneDue")}
      </>
    ) : lead.kind === "note" ? (
      <DataText>{lead.sentence}</DataText>
    ) : (
      t("student.focus.quietDay")
    );

  const taskCard = task ? (
    <FocusCard>
      <div className="space-y-2">
        <p className="text-sm text-black/60">
          <DataText>{task.className}</DataText>
        </p>
        {task.dueAt ? (
          <p className="text-sm text-black/60">
            {t("student.focus.dueLine", {
              date: new Date(task.dueAt).toLocaleDateString(locale, {
                weekday: "long",
                day: "numeric",
                month: "long",
              }),
            })}
          </p>
        ) : null}
        <p className="text-lg font-semibold">
          <DataText>{task.title}</DataText>
        </p>
      </div>
    </FocusCard>
  ) : null;

  const aside =
    task ? (
      <div className="space-y-4">
        {focus ? (
          <div className="space-y-1">
            <p className="text-sm font-semibold">
              {t("student.focus.stillShaky")}{" "}
              <span className="font-normal text-black/60">{levelLabel(focus.level)}</span>
            </p>
            <p className="text-sm">
              <DataText>{focus.label}</DataText>
            </p>
          </div>
        ) : null}
        <PrimaryLink href={taskHref(task)}>{t("student.focus.openTask")}</PrimaryLink>
        {note && note.sentence !== focus?.label ? (
          <p className="text-sm text-black/60">
            <DataText>{note.sentence}</DataText>
          </p>
        ) : null}
      </div>
    ) : null;

  return (
    <div className="space-y-6">
      <h1 className="max-w-xl text-2xl font-semibold leading-snug">{headline}</h1>
      {taskCard ? (
        <div className="grid gap-6 md:grid-cols-[minmax(0,1.2fr)_minmax(0,0.8fr)]">
          {taskCard}
          {aside}
        </div>
      ) : null}
      {due.length > 0 ? (
        <div className="flex gap-6">
          {due.map((item) => (
            <div key={item.id}>
              <p className="text-sm font-semibold">
                {item.dueAt
                  ? new Date(item.dueAt).toLocaleDateString(locale, { weekday: "short" })
                  : ""}
              </p>
              <p className="text-sm text-black/60">
                <DataText>{item.title}</DataText>
              </p>
            </div>
          ))}
        </div>
      ) : null}
    </div>
  );
}

function Skills({
  reads,
  levelLabel,
  onOpenNext,
}: {
  reads: SkillRead[];
  levelLabel: (level: SkillLevel) => string;
  onOpenNext: () => void;
}) {
  const { t } = useI18n();
  const [selectedId, setSelectedId] = useState(reads[0]?.id ?? "");
  const selected = reads.find((read) => read.id === selectedId) ?? reads[0];

  if (reads.length === 0) {
    return <p className="text-sm text-black/60">{t("student.focus.skillsEmpty")}</p>;
  }

  const list = (
    <ul className="space-y-2">
      {reads.map((read) => {
        const selectedRow = read.id === selected?.id;
        return (
          <li key={read.id}>
            <button
              type="button"
              onClick={() => setSelectedId(read.id)}
              className={
                selectedRow
                  ? "w-full rounded-lg border border-[#1c1917] bg-black/[0.04] px-3 py-2 text-left"
                  : "w-full rounded-lg border border-black/10 px-3 py-2 text-left"
              }
            >
              <span className="flex items-baseline justify-between gap-3">
                <span className="text-sm">
                  <DataText>{read.label}</DataText>
                </span>
                <span className="shrink-0 text-sm text-black/60">{levelLabel(read.level)}</span>
              </span>
              <span className="mt-2 block h-1 rounded-full bg-black/10">
                <span
                  className="block h-1 rounded-full bg-[#1c1917]"
                  style={{ width: `${Math.max(0, Math.min(100, (read.mark / 5) * 100))}%` }}
                />
              </span>
            </button>
          </li>
        );
      })}
    </ul>
  );

  const detail = selected ? (
    <div className="space-y-3">
      <p className="text-sm font-semibold">
        <DataText>{selected.sourceTitle}</DataText>
      </p>
      <p className="text-sm">
        <DataText>{selected.feedback || selected.label}</DataText>
      </p>
      <p className="text-sm text-black/60">{levelLabel(selected.level)}</p>
      {selected.level === "gettingThere" || selected.level === "notYet" ? (
        <button
          type="button"
          onClick={onOpenNext}
          className="inline-flex rounded-lg bg-[#1c1917] px-4 py-2.5 text-sm font-semibold text-white"
        >
          {t("student.focus.next")}
        </button>
      ) : null}
    </div>
  ) : null;

  return (
    <div className="space-y-4">
      <p className="text-sm text-black/60">{t("student.focus.skillsIntro")}</p>
      <div className="grid gap-6 md:grid-cols-[minmax(0,1.2fr)_minmax(0,0.8fr)]">
        {list}
        {detail}
      </div>
    </div>
  );
}

function Next({
  focus,
  task,
}: {
  focus: SkillRead | null;
  task: StudentWorkspaceAssessment | null;
}) {
  const { t } = useI18n();
  if (!focus) {
    return <p className="text-sm text-black/60">{t("student.focus.nextEmpty")}</p>;
  }

  return (
    <div className="grid gap-6 md:grid-cols-[minmax(0,1.2fr)_minmax(0,0.8fr)]">
      <div className="space-y-4">
        <h1 className="text-2xl font-semibold leading-snug">
          <DataText>{focus.label}</DataText>
        </h1>
        {task ? (
          <PrimaryLink href={taskHref(task)}>{t("student.focus.openTask")}</PrimaryLink>
        ) : null}
      </div>
    </div>
  );
}

function Notes({
  note,
  levelLabel,
}: {
  note: LatestNote | null;
  levelLabel: (level: SkillLevel) => string;
}) {
  const { t } = useI18n();
  if (!note) {
    return <p className="text-sm text-black/60">{t("student.focus.notesEmpty")}</p>;
  }
  const latest = note.source;

  return (
    <div className="grid gap-6 md:grid-cols-[minmax(0,1.2fr)_minmax(0,0.8fr)]">
      <div className="space-y-4">
        <h1 className="text-2xl font-semibold leading-snug">
          <DataText>{note.sentence}</DataText>
        </h1>
        <p className="text-sm text-black/60">{t("student.focus.reflectionPrompt")}</p>
        <Link href="/student/feedback" className="text-sm text-black/60 underline-offset-2 hover:underline">
          {t("student.focus.allNotes")}
        </Link>
      </div>
      <ul className="divide-y divide-black/10 border-y border-black/10">
        {latest.microSkillMarks.map((mark) => (
          <li key={mark.microSkillId} className="flex items-baseline justify-between gap-3 py-3">
            <span className="text-sm">
              <DataText>{mark.feedback.trim() || latest.title}</DataText>
            </span>
            <span className="shrink-0 text-sm text-black/60">
              {levelLabel(levelFromMark(mark.mark))}
            </span>
          </li>
        ))}
      </ul>
    </div>
  );
}

function Growth({
  workspace,
  levelLabel,
}: {
  workspace: StudentWorkspace;
  levelLabel: (level: SkillLevel) => string;
}) {
  const { t } = useI18n();
  const points = growthPoints(workspace.feedback);

  return (
    <div className="space-y-4">
      {points.length === 0 ? (
        <p className="text-sm text-black/60">{t("student.focus.growthEmpty")}</p>
      ) : (
        <>
          <p className="text-sm text-black/60">{t("student.focus.growthLead")}</p>
          <ul className="space-y-4">
            {points.map((point) => (
              <li key={point.id} className="space-y-1">
                <div className="flex items-baseline justify-between gap-3">
                  <p className="text-sm font-semibold">
                    <DataText>{point.title}</DataText>
                  </p>
                  <p className="text-sm text-black/60">
                    {new Date(point.approvedAt).toLocaleDateString()} · {levelLabel(point.level)}
                  </p>
                </div>
                <div className="h-1 rounded-full bg-black/10">
                  <div
                    className="h-1 rounded-full bg-[#1c1917]"
                    style={{ width: `${Math.max(0, Math.min(100, (point.mark / 5) * 100))}%` }}
                  />
                </div>
                <p className="text-sm text-black/60">
                  {t("student.focus.markOfFive", { mark: point.mark.toFixed(1) })}
                </p>
              </li>
            ))}
          </ul>
        </>
      )}
      {workspace.timeline.length > 0 ? (
        <Link href="/student/progress" className="inline-block text-sm text-black/60 underline-offset-2 hover:underline">
          {t("student.focus.fullTimeline")}
        </Link>
      ) : null}
    </div>
  );
}
