const organisationApiUrl =
  process.env.NEXT_PUBLIC_ORGANISATION_API_URL ?? "http://localhost:8082";

export type Organisation = {
  id: string;
  name: string;
  code: string;
};

export type YearLevel = {
  id: string;
  organisationId: string;
  name: string;
  sortOrder: number;
};

export type SchoolClass = {
  id: string;
  organisationId: string;
  yearLevelId: string;
  name: string;
  code: string;
  teacherUserIds?: string[] | null;
  studentUserIds?: string[] | null;
};

export type ClassMember = {
  userId: string;
};

async function organisationRequest<T>(
  token: string,
  path: string,
  init?: RequestInit
): Promise<T> {
  const response = await fetch(`${organisationApiUrl}${path}`, {
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
    throw new Error(`Organisation request failed (${response.status}).`);
  }

  return response.json() as Promise<T>;
}

export function listOrganisations(token: string): Promise<Organisation[]> {
  return organisationRequest<Organisation[]>(token, "/api/v1/organisations");
}

export function createOrganisation(
  token: string,
  name: string,
  code: string
): Promise<Organisation> {
  return organisationRequest<Organisation>(token, "/api/v1/organisations", {
    method: "POST",
    body: JSON.stringify({ name, code }),
  });
}

export function createYearLevel(
  token: string,
  organisationId: string,
  name: string,
  sortOrder: number
): Promise<YearLevel> {
  return organisationRequest<YearLevel>(
    token,
    `/api/v1/organisations/${organisationId}/year-levels`,
    {
      method: "POST",
      body: JSON.stringify({ name, sortOrder }),
    }
  );
}

export function createClass(
  token: string,
  organisationId: string,
  yearLevelId: string,
  name: string,
  code: string
): Promise<SchoolClass> {
  return organisationRequest<SchoolClass>(
    token,
    `/api/v1/organisations/${organisationId}/classes`,
    {
      method: "POST",
      body: JSON.stringify({ name, code, yearLevelId }),
    }
  );
}

export function listClasses(
  token: string,
  organisationId: string
): Promise<SchoolClass[]> {
  return organisationRequest<SchoolClass[]>(
    token,
    `/api/v1/organisations/${organisationId}/classes`
  );
}

export function assignTeacher(
  token: string,
  organisationId: string,
  classId: string,
  userId: string
): Promise<ClassMember> {
  return organisationRequest<ClassMember>(
    token,
    `/api/v1/organisations/${organisationId}/classes/${classId}/teachers`,
    {
      method: "POST",
      body: JSON.stringify({ userId }),
    }
  );
}

export type ParentChildLink = {
  parentUserId: string;
  studentUserId: string;
};

export function listYearLevels(token: string, organisationId: string): Promise<YearLevel[]> {
  return organisationRequest<YearLevel[]>(
    token,
    `/api/v1/organisations/${organisationId}/year-levels`
  );
}

export function updateYearLevel(
  token: string,
  organisationId: string,
  yearLevelId: string,
  name: string,
  sortOrder: number
): Promise<YearLevel> {
  return organisationRequest<YearLevel>(
    token,
    `/api/v1/organisations/${organisationId}/year-levels/${yearLevelId}`,
    { method: "PUT", body: JSON.stringify({ name, sortOrder }) }
  );
}

export function deleteYearLevel(
  token: string,
  organisationId: string,
  yearLevelId: string
): Promise<void> {
  return organisationRequest<void>(
    token,
    `/api/v1/organisations/${organisationId}/year-levels/${yearLevelId}`,
    { method: "DELETE" }
  );
}

export function updateClass(
  token: string,
  organisationId: string,
  classId: string,
  name: string,
  code: string,
  yearLevelId: string
): Promise<SchoolClass> {
  return organisationRequest<SchoolClass>(
    token,
    `/api/v1/organisations/${organisationId}/classes/${classId}`,
    { method: "PUT", body: JSON.stringify({ name, code, yearLevelId }) }
  );
}

export function deleteClass(
  token: string,
  organisationId: string,
  classId: string
): Promise<void> {
  return organisationRequest<void>(
    token,
    `/api/v1/organisations/${organisationId}/classes/${classId}`,
    { method: "DELETE" }
  );
}

export function listTeachers(
  token: string,
  organisationId: string,
  classId: string
): Promise<ClassMember[]> {
  return organisationRequest<ClassMember[]>(
    token,
    `/api/v1/organisations/${organisationId}/classes/${classId}/teachers`
  );
}

export function unassignTeacher(
  token: string,
  organisationId: string,
  classId: string,
  userId: string
): Promise<void> {
  return organisationRequest<void>(
    token,
    `/api/v1/organisations/${organisationId}/classes/${classId}/teachers/${userId}`,
    { method: "DELETE" }
  );
}

export function listEnrollments(
  token: string,
  organisationId: string,
  classId: string
): Promise<ClassMember[]> {
  return organisationRequest<ClassMember[]>(
    token,
    `/api/v1/organisations/${organisationId}/classes/${classId}/enrollments`
  );
}

export function unenrollStudent(
  token: string,
  organisationId: string,
  classId: string,
  userId: string
): Promise<void> {
  return organisationRequest<void>(
    token,
    `/api/v1/organisations/${organisationId}/classes/${classId}/enrollments/${userId}`,
    { method: "DELETE" }
  );
}

export function listParentChildren(
  token: string,
  parentUserId: string
): Promise<ParentChildLink[]> {
  return organisationRequest<ParentChildLink[]>(
    token,
    `/api/v1/parents/${parentUserId}/children`
  );
}

export function linkParentChild(
  token: string,
  parentUserId: string,
  studentUserId: string
): Promise<ParentChildLink> {
  return organisationRequest<ParentChildLink>(
    token,
    `/api/v1/parents/${parentUserId}/children`,
    { method: "POST", body: JSON.stringify({ studentUserId }) }
  );
}

export function unlinkParentChild(
  token: string,
  parentUserId: string,
  studentUserId: string
): Promise<void> {
  return organisationRequest<void>(
    token,
    `/api/v1/parents/${parentUserId}/children/${studentUserId}`,
    { method: "DELETE" }
  );
}

export function enrollStudent(
  token: string,
  organisationId: string,
  classId: string,
  userId: string
): Promise<ClassMember> {
  return organisationRequest<ClassMember>(
    token,
    `/api/v1/organisations/${organisationId}/classes/${classId}/enrollments`,
    {
      method: "POST",
      body: JSON.stringify({ userId }),
    }
  );
}
