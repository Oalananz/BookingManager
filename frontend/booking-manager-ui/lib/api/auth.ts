import { apiClient } from "@/lib/api/client";
import { ApiEnvelope } from "@/lib/types/common";
import { AuthResult } from "@/lib/types/auth";

export const authApi = {
  register: (fullName: string, email: string, password: string) =>
    apiClient.post<ApiEnvelope<AuthResult>>("/api/auth/register", { fullName, email, password }),
  login: (email: string, password: string) =>
    apiClient.post<ApiEnvelope<AuthResult>>("/api/auth/login", { email, password }),
  logout: () => apiClient.post<void>("/api/auth/logout"),
};
