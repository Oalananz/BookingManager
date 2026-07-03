export type ResourceStatus = "Available" | "Maintenance" | "Disabled";

export interface Resource {
  id: string;
  name: string;
  type: string;
  capacity: number | null;
  status: ResourceStatus;
  description?: string | null;
  createdAt?: string;
  updatedAt?: string;
}

export interface ResourceRequest {
  name: string;
  type: string;
  description?: string | null;
  capacity?: number | null;
  status: ResourceStatus;
}
