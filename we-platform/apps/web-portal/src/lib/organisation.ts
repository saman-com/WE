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
