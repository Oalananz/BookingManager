import { authStorage } from "@/lib/auth/storage";

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5168";

export class ApiError extends Error {
  status: number;
  code: string;

  constructor(status: number, code: string, message: string) {
    super(message);
    this.status = status;
    this.code = code;
  }
}

interface ErrorBody {
  code?: string;
  message?: string;
  errors?: Record<string, string[]>;
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const token = authStorage.getToken();

  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...init,
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...init?.headers,
    },
  });

  if (!response.ok) {
    const body = (await response.json().catch(() => null)) as ErrorBody | null;

    // Expired/invalid session: clear it and send the user back to login.
    if (response.status === 401 && typeof window !== "undefined" && token) {
      authStorage.clear();
      window.location.href = "/login";
    }

    const fieldErrors = body?.errors
      ? " " + Object.values(body.errors).flat().join(" ")
      : "";
    throw new ApiError(
      response.status,
      body?.code ?? "UNKNOWN_ERROR",
      (body?.message ?? "Request failed.") + fieldErrors
    );
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export const apiClient = {
  get: <T>(path: string) => request<T>(path),
  post: <T>(path: string, body?: unknown) =>
    request<T>(path, { method: "POST", body: body ? JSON.stringify(body) : undefined }),
  put: <T>(path: string, body?: unknown) =>
    request<T>(path, { method: "PUT", body: body ? JSON.stringify(body) : undefined }),
  delete: <T>(path: string) => request<T>(path, { method: "DELETE" }),
};
