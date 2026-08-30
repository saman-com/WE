const interventionApiUrl =
  process.env.NEXT_PUBLIC_INTERVENTION_API_URL ?? "http://localhost:8092";

export type InterventionStatus = "Planned" | "Active" | "Completed" | "Closed";

export type Intervention = {
  id: string;
  organisationId: string;
  studentUserId: string;
  learningGapId: string;
  assignedTeacherUserId: string;
  plannedActions: string;
  notes: string;
  outcome: string | null;
  status: InterventionStatus;
  plannedStartAt: string | null;
  plannedEndAt: string | null;
  reviewAt: string | null;
  createdAt: string;
  updatedAt: string;
};

export type StudentInterventions = {
  studentUserId: string;
  interventions: Intervention[];
};

export type CreateInterventionInput = {
  organisationId: string;
  studentUserId: string;
  learningGapId: string;
  plannedActions: string;
  notes?: string;
  plannedStartAt?: string;
  plannedEndAt?: string;
  reviewAt?: string;
};

export type PatchInterventionInput = {
  status?: InterventionStatus;
  notes?: string;
  outcome?: string;
  plannedActions?: string;
};

async function handleResponse<T>(response: Response, action: string): Promise<T> {
  if (!response.ok) {
    throw new Error(`${action} failed (${response.status}).`);
  }
  return response.json() as Promise<T>;
}

export function fetchStudentInterventions(
  token: string,
  studentUserId: string
): Promise<StudentInterventions> {
  return fetch(
    `${interventionApiUrl}/api/v1/interventions?studentUserId=${encodeURIComponent(studentUserId)}`,
    {
      headers: { Authorization: `Bearer ${token}` },
    }
  ).then((response) => handleResponse<StudentInterventions>(response, "Student interventions request"));
}

export function fetchIntervention(token: string, interventionId: string): Promise<Intervention> {
  return fetch(`${interventionApiUrl}/api/v1/interventions/${interventionId}`, {
    headers: { Authorization: `Bearer ${token}` },
  }).then((response) => handleResponse<Intervention>(response, "Intervention request"));
}

export function createIntervention(
  token: string,
  input: CreateInterventionInput
): Promise<Intervention> {
  return fetch(`${interventionApiUrl}/api/v1/interventions`, {
    method: "POST",
    headers: {
      Authorization: `Bearer ${token}`,
      "Content-Type": "application/json",
    },
    body: JSON.stringify(input),
  }).then((response) => handleResponse<Intervention>(response, "Create intervention request"));
}

export function patchIntervention(
  token: string,
  interventionId: string,
  input: PatchInterventionInput
): Promise<Intervention> {
  return fetch(`${interventionApiUrl}/api/v1/interventions/${interventionId}`, {
    method: "PATCH",
    headers: {
      Authorization: `Bearer ${token}`,
      "Content-Type": "application/json",
    },
    body: JSON.stringify(input),
  }).then((response) => handleResponse<Intervention>(response, "Update intervention request"));
}
