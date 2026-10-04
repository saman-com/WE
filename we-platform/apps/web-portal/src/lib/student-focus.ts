import type {
  StudentWorkspaceAssessment,
  StudentWorkspaceFeedback,
} from "@/lib/student-workspace";

export type SkillLevel = "strong" | "solid" | "gettingThere" | "notYet";

export type SkillRead = {
  id: string;
  label: string;
  mark: number;
  level: SkillLevel;
  feedback: string;
  approvedAt: string;
  sourceTitle: string;
};

export type GrowthPoint = {
  id: string;
  title: string;
  approvedAt: string;
  mark: number;
  level: SkillLevel;
};

/** Student-facing words for a mark out of 5. Solid starts at 3. */
export function levelFromMark(mark: number): SkillLevel {
  if (mark >= 5) {
    return "strong";
  }
  if (mark >= 3) {
    return "solid";
  }
  if (mark >= 2) {
    return "gettingThere";
  }
  return "notYet";
}

export function latestSkillReads(feedback: StudentWorkspaceFeedback[]): SkillRead[] {
  const ordered = [...feedback].sort((a, b) => a.approvedAt.localeCompare(b.approvedAt));
  const bySkill = new Map<string, SkillRead>();

  for (const item of ordered) {
    for (const mark of item.microSkillMarks) {
      const sentence = mark.feedback.trim();
      bySkill.set(mark.microSkillId, {
        id: mark.microSkillId,
        label: sentence || item.title,
        mark: mark.mark,
        level: levelFromMark(mark.mark),
        feedback: sentence,
        approvedAt: item.approvedAt,
        sourceTitle: item.title,
      });
    }
  }

  return [...bySkill.values()];
}

export function shakySkill(reads: SkillRead[]): SkillRead | null {
  const open = reads.filter(
    (read) => read.level === "gettingThere" || read.level === "notYet"
  );
  if (open.length === 0) {
    return null;
  }
  return [...open].sort((a, b) => a.mark - b.mark)[0];
}

export function nextAssessment(
  assessments: StudentWorkspaceAssessment[]
): StudentWorkspaceAssessment | null {
  const pending = assessments.filter((item) => !item.hasSubmitted);
  pending.sort((a, b) => {
    if (a.dueAt && b.dueAt) {
      return a.dueAt.localeCompare(b.dueAt);
    }
    if (a.dueAt) {
      return -1;
    }
    if (b.dueAt) {
      return 1;
    }
    return a.title.localeCompare(b.title);
  });
  return pending[0] ?? null;
}

export function taskHref(assessment: StudentWorkspaceAssessment): string {
  const query = new URLSearchParams({
    classId: assessment.classId,
    assessmentId: assessment.id,
  });
  return `/student/assessments?${query.toString()}`;
}

export type TodayHeadline =
  | { kind: "twoThings"; dueAt: string }
  | { kind: "oneDue"; title: string }
  | { kind: "note"; sentence: string }
  | { kind: "empty" };

export function todayHeadline(
  task: StudentWorkspaceAssessment | null,
  focus: SkillRead | null,
  note: string | null
): TodayHeadline {
  if (task && focus && task.dueAt) {
    return { kind: "twoThings", dueAt: task.dueAt };
  }
  if (task) {
    return { kind: "oneDue", title: task.title };
  }
  if (note) {
    return { kind: "note", sentence: note };
  }
  return { kind: "empty" };
}

export type LatestNote = {
  source: StudentWorkspaceFeedback;
  sentence: string;
};

export function latestNote(feedback: StudentWorkspaceFeedback[]): LatestNote | null {
  const source = [...feedback].sort((a, b) => b.approvedAt.localeCompare(a.approvedAt))[0];
  if (!source) {
    return null;
  }
  const shakiest = [...source.microSkillMarks]
    .filter((mark) => mark.feedback.trim())
    .sort((a, b) => a.mark - b.mark)[0];
  return { source, sentence: shakiest?.feedback.trim() || source.title };
}

export function upcomingDue(
  assessments: StudentWorkspaceAssessment[],
  limit = 3
): StudentWorkspaceAssessment[] {
  return assessments
    .filter((item) => !item.hasSubmitted && item.dueAt)
    .sort((a, b) => a.dueAt!.localeCompare(b.dueAt!))
    .slice(0, limit);
}

export function growthPoints(feedback: StudentWorkspaceFeedback[]): GrowthPoint[] {
  return [...feedback]
    .filter((item) => item.microSkillMarks.length > 0)
    .sort((a, b) => a.approvedAt.localeCompare(b.approvedAt))
    .map((item) => {
      const mark =
        item.microSkillMarks.reduce((sum, entry) => sum + entry.mark, 0) /
        item.microSkillMarks.length;
      return {
        id: item.evidenceId,
        title: item.title,
        approvedAt: item.approvedAt,
        mark,
        level: levelFromMark(mark),
      };
    });
}
