import { listDirectoryUsers, personName, type DirectoryUser } from "@/lib/auth";
import {
  collectMicroSkillNames,
  getCurriculumTree,
  listCurricula,
  type CurriculumTree,
} from "@/lib/curriculum";
import { fetchStudentGaps } from "@/lib/gaps";

export function collectLearningNames(tree: CurriculumTree): Record<string, string> {
  const names = collectMicroSkillNames(tree);
  for (const subject of tree.subjects ?? []) {
    for (const unit of subject.units ?? []) {
      for (const objective of unit.learningObjectives ?? []) {
        if (objective.title) {
          names[objective.id] = objective.title;
        }
      }
    }
  }
  return names;
}

export async function loadPeople(token: string): Promise<DirectoryUser[]> {
  try {
    return await listDirectoryUsers(token);
  } catch {
    return [];
  }
}

export async function loadLearningNames(
  token: string,
  organisationId: string
): Promise<Record<string, string>> {
  try {
    const curricula = await listCurricula(token, organisationId);
    const trees = await Promise.all(
      curricula.map((curriculum) => getCurriculumTree(token, curriculum.id).catch(() => null))
    );
    const names: Record<string, string> = {};
    for (const tree of trees) {
      if (tree) {
        Object.assign(names, collectLearningNames(tree));
      }
    }
    return names;
  } catch {
    return {};
  }
}

export async function loadGapLabels(
  token: string,
  studentUserIds: string[],
  learningNames: Record<string, string>
): Promise<Record<string, string>> {
  const labels: Record<string, string> = {};
  await Promise.all(
    [...new Set(studentUserIds)].map(async (studentUserId) => {
      try {
        const loaded = await fetchStudentGaps(token, studentUserId);
        for (const gap of loaded.gaps) {
          const skill = learningNames[gap.microSkillId];
          const objective = gap.learningObjectiveId
            ? learningNames[gap.learningObjectiveId]
            : undefined;
          const name = skill || objective;
          if (name) {
            labels[gap.id] = name;
          }
        }
      } catch {
        // A missing gap list leaves that student's gaps unlabeled.
      }
    })
  );
  return labels;
}

export function learningLabel(
  names: Record<string, string>,
  id: string,
  unknown: string
): string {
  return names[id] || unknown;
}

const visibleId = /[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/gi;

export function replaceVisibleIds(
  text: string,
  people: ReadonlyArray<{ id: string; name?: string; email?: string }>,
  learningNames: Record<string, string>,
  unknownPerson: string,
  unknownSkill: string
): string {
  return text.replace(visibleId, (id) => {
    const skill = learningNames[id];
    if (skill) {
      return skill;
    }
    const person = personName(people, id, "");
    return person || unknownSkill || unknownPerson;
  });
}
