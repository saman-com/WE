import { readApiError } from "@/lib/api-error";

const federationApiUrl =
  process.env.NEXT_PUBLIC_FEDERATION_API_URL ?? "http://localhost:8099";

export type FederationSchool = {
  tenantId: string;
  federationId: string;
  name: string;
  code: string;
  hasDefaultConfiguration: boolean;
  createdAt: string;
};

export type SchoolMetricSummary = {
  tenantId: string;
  name: string;
  enrollmentCount: number;
  averageProgressPercent: number;
};

export type FederationMetrics = {
  totalSchools: number;
  totalEnrollment: number;
  averageProgressPercent: number;
  schools: SchoolMetricSummary[];
};

export type FederationPolicy = {
  policyKey: string;
  policyValue: string;
};

export type CreateFederationSchoolRequest = {
  name: string;
  code: string;
};

export type AssignSchoolAdminRequest = {
  userId: string;
};

async function federationRequest<T>(
  token: string,
  path: string,
  init?: RequestInit
): Promise<T> {
  const response = await fetch(`${federationApiUrl}${path}`, {
    ...init,
    headers: {
      Authorization: `Bearer ${token}`,
      "Content-Type": "application/json",
      ...(init?.headers ?? {}),
    },
  });

  if (!response.ok) {
    throw await readApiError(response, "federation.configuration_provision_failed");
  }

  return response.json() as Promise<T>;
}

export function listFederationSchools(token: string): Promise<FederationSchool[]> {
  return federationRequest<FederationSchool[]>(token, "/api/v1/federation/schools");
}

export function createFederationSchool(
  token: string,
  body: CreateFederationSchoolRequest
): Promise<FederationSchool> {
  return federationRequest<FederationSchool>(token, "/api/v1/federation/schools", {
    method: "POST",
    body: JSON.stringify(body),
  });
}

export function assignSchoolAdmin(
  token: string,
  schoolTenantId: string,
  body: AssignSchoolAdminRequest
): Promise<void> {
  return federationRequest<void>(
    token,
    `/api/v1/federation/schools/${schoolTenantId}/admins`,
    {
      method: "POST",
      body: JSON.stringify(body),
    }
  );
}

export function fetchFederationMetrics(token: string): Promise<FederationMetrics> {
  return federationRequest<FederationMetrics>(token, "/api/v1/federation/metrics");
}

export function fetchFederationPolicies(token: string): Promise<FederationPolicy[]> {
  return federationRequest<FederationPolicy[]>(token, "/api/v1/federation/policies");
}

export function updateFederationPolicies(
  token: string,
  policies: FederationPolicy[]
): Promise<FederationPolicy[]> {
  return federationRequest<FederationPolicy[]>(token, "/api/v1/federation/policies", {
    method: "PUT",
    body: JSON.stringify({ policies }),
  });
}
