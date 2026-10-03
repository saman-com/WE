export type ApiErrorBody = {
  code?: string;
  message?: string;
};

export class ApiError extends Error {
  readonly code: string;
  readonly status: number;

  constructor(code: string, status: number, message?: string) {
    super(message ?? code);
    this.name = "ApiError";
    this.code = code;
    this.status = status;
  }
}

export async function readApiError(
  response: Response,
  fallbackCode = "unknown"
): Promise<ApiError> {
  let code = fallbackCode;
  let message: string | undefined;

  try {
    const body = (await response.json()) as ApiErrorBody;
    if (body.code) {
      code = body.code;
    }
    if (body.message) {
      message = body.message;
    }
  } catch {
    // Non-JSON error bodies still map to a translatable fallback code.
  }

  return new ApiError(code, response.status, message);
}
