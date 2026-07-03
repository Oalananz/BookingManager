import { apiClient } from "@/lib/api/client";
import { Resource } from "@/lib/types/resource";

export const resourcesApi = {
  getAll: () => apiClient.get<Resource[]>("/api/resources"),
};
