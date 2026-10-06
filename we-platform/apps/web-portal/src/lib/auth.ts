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

export type DirectoryUser = {
  id: string;
  name: string;
  email: string;
  roles: string[];
  active?: boolean;
};

export type ParentTeacher = {
  id: string;
  name: string;
};

export function personName(
  people: ReadonlyArray<{ id: string; name?: string; email?: string }>,
  userId: string,
  unknownLabel: string
): string {
  const match = people.find((person) => person.id === userId);
  if (match?.name) {
    return match.name;
  }
  if (match?.email) {
    return match.email;
  }
  return unknownLabel;
}

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

export async function createDirectoryUser(
  token: string,
  account: { name: string; email: string; password: string; role: string }
): Promise<DirectoryUser> {
  const response = await fetch(`${identityApiUrl}/api/v1/users`, {
    method: "POST",
    headers: {
      Authorization: `Bearer ${token}`,
      "Content-Type": "application/json",
    },
    body: JSON.stringify(account),
  });

  if (!response.ok) {
    throw await readApiError(response, "users.invalid");
  }

  return response.json();
}

export async function updateDirectoryUser(
  token: string,
  userId: string,
  account: { name: string; role: string }
): Promise<DirectoryUser> {
  const response = await fetch(`${identityApiUrl}/api/v1/users/${userId}`, {
    method: "PUT",
    headers: {
      Authorization: `Bearer ${token}`,
      "Content-Type": "application/json",
    },
    body: JSON.stringify(account),
  });

  if (!response.ok) {
    throw await readApiError(response, "users.invalid");
  }

  return response.json();
}

export async function deactivateDirectoryUser(
  token: string,
  userId: string
): Promise<DirectoryUser> {
  return postAccountAction(token, userId, "deactivate");
}

export async function reactivateDirectoryUser(
  token: string,
  userId: string
): Promise<DirectoryUser> {
  return postAccountAction(token, userId, "reactivate");
}

export async function resetDirectoryUserPassword(
  token: string,
  userId: string,
  password: string
): Promise<DirectoryUser> {
  const response = await fetch(`${identityApiUrl}/api/v1/users/${userId}/password`, {
    method: "POST",
    headers: {
      Authorization: `Bearer ${token}`,
      "Content-Type": "application/json",
    },
    body: JSON.stringify({ password }),
  });

  if (!response.ok) {
    throw await readApiError(response, "users.password_invalid");
  }

  return response.json();
}

async function postAccountAction(
  token: string,
  userId: string,
  action: "deactivate" | "reactivate"
): Promise<DirectoryUser> {
  const response = await fetch(`${identityApiUrl}/api/v1/users/${userId}/${action}`, {
    method: "POST",
    headers: { Authorization: `Bearer ${token}` },
  });

  if (!response.ok) {
    throw await readApiError(response, "users.invalid");
  }

  return response.json();
}

export async function listDirectoryUsers(token: string): Promise<DirectoryUser[]> {
  const response = await fetch(`${identityApiUrl}/api/v1/users`, {
    headers: { Authorization: `Bearer ${token}` },
  });

  if (!response.ok) {
    throw await readApiError(response, "errors.unknown");
  }

  return response.json();
}

export async function listParentTeachers(token: string): Promise<ParentTeacher[]> {
  const response = await fetch(`${identityApiUrl}/api/v1/parents/me/teachers`, {
    headers: { Authorization: `Bearer ${token}` },
  });

  if (!response.ok) {
    throw await readApiError(response, "errors.unknown");
  }

  return response.json();
}

export async function listParentChildren(token: string): Promise<ParentTeacher[]> {
  const response = await fetch(`${identityApiUrl}/api/v1/parents/me/children-names`, {
    headers: { Authorization: `Bearer ${token}` },
  });

  if (!response.ok) {
    throw await readApiError(response, "errors.unknown");
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
