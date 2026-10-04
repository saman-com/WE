import { describe, expect, it } from "vitest";
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
} from "@/lib/student-focus";
import type {
  StudentWorkspaceAssessment,
  StudentWorkspaceFeedback,
} from "@/lib/student-workspace";

function assessment(
  partial: Partial<StudentWorkspaceAssessment> & Pick<StudentWorkspaceAssessment, "id" | "title">
): StudentWorkspaceAssessment {
  return {
    organisationId: "org",
    classId: "class",
    className: "Year 11 Mathematics",
    dueAt: null,
    learningObjectiveIds: [],
    hasSubmitted: false,
    submittedAt: null,
    ...partial,
  };
}

const feedback: StudentWorkspaceFeedback[] = [
  {
    evidenceId: "older",
    assessmentId: "a1",
    title: "Algebra sheet",
    approvedAt: "2026-09-01T00:00:00Z",
    microSkillMarks: [
      { microSkillId: "isolate", mark: 2, feedback: "The letter is still on both sides." },
      { microSkillId: "substitute", mark: 4, feedback: "You can substitute a number." },
    ],
  },
  {
    evidenceId: "newer",
    assessmentId: "a2",
    title: "Algebra sheet 2",
    approvedAt: "2026-09-20T00:00:00Z",
    microSkillMarks: [
      { microSkillId: "isolate", mark: 2, feedback: "Getting the letter alone is the next piece." },
    ],
  },
];

describe("student focus", () => {
  it("names marks the way a student reads them", () => {
    expect(levelFromMark(5)).toBe("strong");
    expect(levelFromMark(3)).toBe("solid");
    expect(levelFromMark(2)).toBe("gettingThere");
    expect(levelFromMark(1)).toBe("notYet");
  });

  it("only calls full marks Strong, and Solid starts at 3 out of 5", () => {
    expect(levelFromMark(4.9)).toBe("solid");
    expect(levelFromMark(2.9)).toBe("gettingThere");
    expect(levelFromMark(1.9)).toBe("notYet");
    expect(levelFromMark(0)).toBe("notYet");
  });

  it("keeps the latest approved sentence for each skill", () => {
    const reads = latestSkillReads(feedback);
    const isolate = reads.find((read) => read.id === "isolate");
    expect(isolate?.feedback).toBe("Getting the letter alone is the next piece.");
    expect(isolate?.level).toBe("gettingThere");
  });

  it("lists skills in the order the work presents them, not ranked by mark", () => {
    const reads = latestSkillReads([
      {
        evidenceId: "sheet",
        assessmentId: "a1",
        title: "Algebra sheet",
        approvedAt: "2026-09-28T00:00:00Z",
        microSkillMarks: [
          { microSkillId: "read", mark: 5, feedback: "Read an equation" },
          { microSkillId: "isolate", mark: 2, feedback: "Isolate the variable" },
          { microSkillId: "brackets", mark: 1, feedback: "Solve with brackets" },
        ],
      },
    ]);
    expect(reads.map((read) => read.id)).toEqual(["read", "isolate", "brackets"]);
  });

  it("opens the exact due assessment, not the generic list", () => {
    const href = taskHref(
      assessment({ id: "algebra check", title: "Algebra check", classId: "class-11" })
    );
    expect(href).toBe("/student/assessments?classId=class-11&assessmentId=algebra+check");
  });

  it("chooses the Today headline from the task, the shaky skill, and the last note", () => {
    const task = assessment({ id: "soon", title: "Algebra check", dueAt: "2026-10-09T00:00:00Z" });
    const focus = shakySkill(latestSkillReads(feedback));

    expect(todayHeadline(task, focus, "A note")).toEqual({
      kind: "twoThings",
      dueAt: "2026-10-09T00:00:00Z",
    });
    expect(todayHeadline(task, null, "A note")).toEqual({ kind: "oneDue", title: "Algebra check" });
    expect(todayHeadline(null, focus, "A note")).toEqual({ kind: "note", sentence: "A note" });
    expect(todayHeadline(null, null, null)).toEqual({ kind: "empty" });
  });

  it("leads Notes with the shakiest skill on the latest approved work", () => {
    const note = latestNote([
      ...feedback,
      {
        evidenceId: "latest",
        assessmentId: "a3",
        title: "Algebra quiz",
        approvedAt: "2026-09-25T00:00:00Z",
        microSkillMarks: [
          { microSkillId: "read", mark: 5, feedback: "You read every equation correctly." },
          { microSkillId: "isolate", mark: 2, feedback: "Get the letter alone first." },
        ],
      },
    ]);
    expect(note?.source.evidenceId).toBe("latest");
    expect(note?.sentence).toBe("Get the letter alone first.");
    expect(latestNote([])).toBeNull();
  });

  it("picks the shakiest open skill and the soonest task", () => {
    expect(shakySkill(latestSkillReads(feedback))?.id).toBe("isolate");
    const next = nextAssessment([
      assessment({ id: "later", title: "Later", dueAt: "2026-10-20T00:00:00Z" }),
      assessment({ id: "soon", title: "Algebra check", dueAt: "2026-10-09T00:00:00Z" }),
      assessment({ id: "done", title: "Done", hasSubmitted: true, dueAt: "2026-10-01T00:00:00Z" }),
    ]);
    expect(next?.title).toBe("Algebra check");
    expect(upcomingDue([
      assessment({ id: "a", title: "A", dueAt: "2026-10-12T00:00:00Z" }),
      assessment({ id: "b", title: "B", dueAt: "2026-10-09T00:00:00Z" }),
    ]).map((item) => item.id)).toEqual(["b", "a"]);
  });

  it("builds growth from approved work only", () => {
    const points = growthPoints(feedback);
    expect(points.map((point) => point.id)).toEqual(["older", "newer"]);
    expect(points[0].level).toBe("solid");
    expect(points[1].level).toBe("gettingThere");
  });
});
