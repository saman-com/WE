const curriculumApiUrl =
  process.env.NEXT_PUBLIC_CURRICULUM_API_URL ?? "http://localhost:8083";

export type Curriculum = {
  id: string;
  organisationId: string;
  name: string;
  version: string;
  status: string;
  regionCode: string | null;
  scope: string;
  parentCurriculumId: string | null;
};

export type Subject = {
  id: string;
  curriculumId: string;
  name: string;
  code: string;
  sortOrder: number;
  sourceNodeId: string | null;
  isOverridden: boolean;
};

export type Unit = {
  id: string;
  subjectId: string;
  curriculumId: string;
  name: string;
  sortOrder: number;
  sourceNodeId: string | null;
  isOverridden: boolean;
};

export type Topic = {
  id: string;
  unitId: string;
  subjectId: string;
  curriculumId: string;
  name: string;
  sortOrder: number;
  sourceNodeId: string | null;
  isOverridden: boolean;
};

export type MicroSkill = {
  id: string;
  learningObjectiveId: string;
  unitId: string;
  subjectId: string;
  curriculumId: string;
  name: string;
  sortOrder: number;
  sourceNodeId: string | null;
  isOverridden: boolean;
};

export type MicroSkillTree = {
  id: string;
  name: string;
  sortOrder: number;
  sourceNodeId: string | null;
  isOverridden: boolean;
};

export type LearningObjective = {
  id: string;
  unitId: string;
  subjectId: string;
  curriculumId: string;
  title: string;
  sortOrder: number;
  sourceNodeId: string | null;
  isOverridden: boolean;
};

export type LearningObjectiveTree = {
  id: string;
  title: string;
  sortOrder: number;
  microSkills: MicroSkillTree[];
  sourceNodeId: string | null;
  isOverridden: boolean;
};

export type UnitTree = {
  id: string;
  name: string;
  sortOrder: number;
  topics: Topic[];
  learningObjectives: LearningObjectiveTree[];
  sourceNodeId: string | null;
  isOverridden: boolean;
};

export type SubjectTree = {
  id: string;
  name: string;
  code: string;
  sortOrder: number;
  units: UnitTree[];
  sourceNodeId: string | null;
  isOverridden: boolean;
};

export type CurriculumTree = {
  id: string;
  organisationId: string;
  name: string;
  version: string;
  status: string;
  subjects: SubjectTree[];
  regionCode: string | null;
  scope: string;
  parentCurriculumId: string | null;
};

export function collectMicroSkillNames(tree: CurriculumTree): Record<string, string> {
  const names: Record<string, string> = {};
  for (const subject of tree.subjects ?? []) {
    for (const unit of subject.units ?? []) {
      for (const objective of unit.learningObjectives ?? []) {
        for (const skill of objective.microSkills ?? []) {
          names[skill.id] = skill.name;
        }
      }
    }
  }
  return names;
}

async function curriculumRequest<T>(
  token: string,
  path: string,
  init?: RequestInit
): Promise<T> {
  const response = await fetch(`${curriculumApiUrl}${path}`, {
    ...init,
    headers: {
      Authorization: `Bearer ${token}`,
      "Content-Type": "application/json",
      ...(init?.headers ?? {}),
    },
  });

  if (response.status === 204) {
    return undefined as T;
  }

  if (!response.ok) {
    throw new Error(`Curriculum request failed (${response.status}).`);
  }

  return response.json() as Promise<T>;
}

export function listCurricula(
  token: string,
  organisationId: string,
  regionCode?: string
): Promise<Curriculum[]> {
  const params = new URLSearchParams({ organisationId });
  if (regionCode) {
    params.set("regionCode", regionCode);
  }
  return curriculumRequest<Curriculum[]>(
    token,
    `/api/v1/curriculum?${params.toString()}`
  );
}

export function createCurriculum(
  token: string,
  organisationId: string,
  name: string,
  version: string,
  options?: { regionCode?: string; scope?: "Regional" | "School" }
): Promise<Curriculum> {
  return curriculumRequest<Curriculum>(token, "/api/v1/curriculum", {
    method: "POST",
    body: JSON.stringify({
      organisationId,
      name,
      version,
      status: "Draft",
      regionCode: options?.regionCode ?? null,
      scope: options?.scope ?? "School",
    }),
  });
}

export function inheritCurriculum(
  token: string,
  parentCurriculumId: string,
  organisationId: string
): Promise<Curriculum> {
  return curriculumRequest<Curriculum>(
    token,
    `/api/v1/curriculum/${parentCurriculumId}/inherit`,
    {
      method: "POST",
      body: JSON.stringify({ organisationId }),
    }
  );
}

export function getCurriculumTree(
  token: string,
  curriculumId: string
): Promise<CurriculumTree> {
  return curriculumRequest<CurriculumTree>(
    token,
    `/api/v1/curriculum/${curriculumId}/tree`
  );
}

export function createSubject(
  token: string,
  curriculumId: string,
  name: string,
  code: string,
  sortOrder: number
): Promise<Subject> {
  return curriculumRequest<Subject>(
    token,
    `/api/v1/curriculum/${curriculumId}/subjects`,
    {
      method: "POST",
      body: JSON.stringify({ name, code, sortOrder }),
    }
  );
}

export function createUnit(
  token: string,
  curriculumId: string,
  subjectId: string,
  name: string,
  sortOrder: number
): Promise<Unit> {
  return curriculumRequest<Unit>(
    token,
    `/api/v1/curriculum/${curriculumId}/subjects/${subjectId}/units`,
    {
      method: "POST",
      body: JSON.stringify({ name, sortOrder }),
    }
  );
}

export function updateUnit(
  token: string,
  curriculumId: string,
  subjectId: string,
  unitId: string,
  name: string,
  sortOrder: number
): Promise<Unit> {
  return curriculumRequest<Unit>(
    token,
    `/api/v1/curriculum/${curriculumId}/subjects/${subjectId}/units/${unitId}`,
    {
      method: "PUT",
      body: JSON.stringify({ name, sortOrder }),
    }
  );
}

export function createTopic(
  token: string,
  curriculumId: string,
  subjectId: string,
  unitId: string,
  name: string,
  sortOrder: number
): Promise<Topic> {
  return curriculumRequest<Topic>(
    token,
    `/api/v1/curriculum/${curriculumId}/subjects/${subjectId}/units/${unitId}/topics`,
    {
      method: "POST",
      body: JSON.stringify({ name, sortOrder }),
    }
  );
}

export function createLearningObjective(
  token: string,
  curriculumId: string,
  subjectId: string,
  unitId: string,
  title: string,
  sortOrder: number
): Promise<LearningObjective> {
  return curriculumRequest<LearningObjective>(
    token,
    `/api/v1/curriculum/${curriculumId}/subjects/${subjectId}/units/${unitId}/learning-objectives`,
    {
      method: "POST",
      body: JSON.stringify({ title, sortOrder }),
    }
  );
}

export function updateLearningObjective(
  token: string,
  curriculumId: string,
  subjectId: string,
  unitId: string,
  learningObjectiveId: string,
  title: string,
  sortOrder: number
): Promise<LearningObjective> {
  return curriculumRequest<LearningObjective>(
    token,
    `/api/v1/curriculum/${curriculumId}/subjects/${subjectId}/units/${unitId}/learning-objectives/${learningObjectiveId}`,
    {
      method: "PUT",
      body: JSON.stringify({ title, sortOrder }),
    }
  );
}

export function createMicroSkill(
  token: string,
  curriculumId: string,
  subjectId: string,
  unitId: string,
  learningObjectiveId: string,
  name: string,
  sortOrder: number
): Promise<MicroSkill> {
  return curriculumRequest<MicroSkill>(
    token,
    `/api/v1/curriculum/${curriculumId}/subjects/${subjectId}/units/${unitId}/learning-objectives/${learningObjectiveId}/micro-skills`,
    {
      method: "POST",
      body: JSON.stringify({ name, sortOrder }),
    }
  );
}
