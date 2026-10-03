import { ApiError, readApiError } from "@/lib/api-error";

const identityApiUrl =
  process.env.NEXT_PUBLIC_IDENTITY_API_URL ?? "http://localhost:8081";

export type LoginResponse = {
  accessToken: string;
  expiresInSeconds: number;
};

export type UserProfile = {
  id: string;
  email: string;
  name: string;
  roles: string[];
};

export async function login(
  email: string,
  password: string
): Promise<LoginResponse> {
  const response = await fetch(`${identityApiUrl}/api/v1/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, password }),
  });

  if (!response.ok) {
    throw await readApiError(response, "auth.invalid_credentials");
  }

  return response.json();
}

export async function fetchProfile(token: string): Promise<UserProfile> {
  const response = await fetch(`${identityApiUrl}/api/v1/auth/me`, {
    headers: { Authorization: `Bearer ${token}` },
  });

  if (!response.ok) {
    throw new ApiError("auth.unauthorized", response.status);
  }

  return response.json();
}
