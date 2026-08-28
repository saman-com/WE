const curriculumApiUrl =
  process.env.NEXT_PUBLIC_CURRICULUM_API_URL ?? "http://localhost:8083";

export type Curriculum = {
  id: string;
  organisationId: string;
  name: string;
  version: string;
  status: string;
};

export type Subject = {
  id: string;
  curriculumId: string;
  name: string;
  code: string;
  sortOrder: number;
};

export type Unit = {
  id: string;
  subjectId: string;
  curriculumId: string;
  name: string;
  sortOrder: number;
};

export type Topic = {
  id: string;
  unitId: string;
  subjectId: string;
  curriculumId: string;
  name: string;
  sortOrder: number;
};

export type MicroSkill = {
  id: string;
  learningObjectiveId: string;
  unitId: string;
  subjectId: string;
  curriculumId: string;
  name: string;
  sortOrder: number;
};

export type MicroSkillTree = {
  id: string;
  name: string;
  sortOrder: number;
};

export type LearningObjective = {
  id: string;
  unitId: string;
  subjectId: string;
  curriculumId: string;
  title: string;
  sortOrder: number;
};

export type LearningObjectiveTree = {
  id: string;
  title: string;
  sortOrder: number;
  microSkills: MicroSkillTree[];
};

export type UnitTree = {
  id: string;
  name: string;
  sortOrder: number;
  topics: Topic[];
  learningObjectives: LearningObjectiveTree[];
};

export type SubjectTree = {
  id: string;
  name: string;
  code: string;
  sortOrder: number;
  units: UnitTree[];
};

export type CurriculumTree = {
  id: string;
  organisationId: string;
  name: string;
  version: string;
  status: string;
  subjects: SubjectTree[];
};

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
  organisationId: string
): Promise<Curriculum[]> {
  return curriculumRequest<Curriculum[]>(
    token,
    `/api/v1/curriculum?organisationId=${organisationId}`
  );
}

export function createCurriculum(
  token: string,
  organisationId: string,
  name: string,
  version: string
): Promise<Curriculum> {
  return curriculumRequest<Curriculum>(token, "/api/v1/curriculum", {
    method: "POST",
    body: JSON.stringify({
      organisationId,
      name,
      version,
      status: "Draft",
    }),
  });
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
