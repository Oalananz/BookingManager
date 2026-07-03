import { apiClient } from "@/lib/api/client";
import { ApiEnvelope } from "@/lib/types/common";
import { Availability } from "@/lib/types/booking";
import { Resource, ResourceRequest } from "@/lib/types/resource";

export const resourcesApi = {
  getAll: () => apiClient.get<ApiEnvelope<Resource[]>>("/api/resources"),
  getById: (id: string) => apiClient.get<ApiEnvelope<Resource>>(`/api/resources/${id}`),
  getAvailability: (id: string, from: string, to: string, durationMinutes: number) =>
    apiClient.get<ApiEnvelope<Availability>>(
      `/api/resources/${id}/availability?from=${encodeURIComponent(from)}&to=${encodeURIComponent(to)}&durationMinutes=${durationMinutes}`
    ),
  create: (request: ResourceRequest) =>
    apiClient.post<ApiEnvelope<Resource>>("/api/resources", request),
  update: (id: string, request: ResourceRequest) =>
    apiClient.put<ApiEnvelope<Resource>>(`/api/resources/${id}`, request),
  remove: (id: string) => apiClient.delete<void>(`/api/resources/${id}`),
};
