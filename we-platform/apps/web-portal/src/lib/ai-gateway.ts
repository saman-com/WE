const aiGatewayApiUrl =
  process.env.NEXT_PUBLIC_AI_GATEWAY_API_URL ?? "http://localhost:8093";

export type AiAuditLogEntry = {
  id: string;
  callerUserId: string;
  promptId: string;
  promptVersion: string;
  contextScope: string;
  outcome: string;
  blockReason?: string | null;
  providerName: string;
  createdAt: string;
};

export type AiAuditLogSearch = {
  promptId?: string;
  outcome?: string;
  callerUserId?: string;
  search?: string;
};

async function aiGatewayRequest<T>(
  token: string,
  path: string,
  init?: RequestInit
): Promise<T> {
  const response = await fetch(`${aiGatewayApiUrl}${path}`, {
    ...init,
    headers: {
      Authorization: `Bearer ${token}`,
      "Content-Type": "application/json",
      ...(init?.headers ?? {}),
    },
  });

  if (!response.ok) {
    throw new Error(`AI Gateway request failed (${response.status}).`);
  }

  return response.json() as Promise<T>;
}

export function searchAiAuditLogs(
  token: string,
  filters: AiAuditLogSearch = {}
): Promise<AiAuditLogEntry[]> {
  const params = new URLSearchParams();
  if (filters.promptId) {
    params.set("promptId", filters.promptId);
  }
  if (filters.outcome) {
    params.set("outcome", filters.outcome);
  }
  if (filters.callerUserId) {
    params.set("callerUserId", filters.callerUserId);
  }
  if (filters.search) {
    params.set("search", filters.search);
  }

  const query = params.toString();
  return aiGatewayRequest<AiAuditLogEntry[]>(
    token,
    `/api/v1/ai/audit-logs${query ? `?${query}` : ""}`
  );
}
