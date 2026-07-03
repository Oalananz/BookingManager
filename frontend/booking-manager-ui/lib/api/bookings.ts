import { apiClient } from "@/lib/api/client";
import { ApiEnvelope, Paged } from "@/lib/types/common";
import { AuditLog, Booking, CreateBookingRequest } from "@/lib/types/booking";

export interface BookingFilters {
  resourceId?: string;
  from?: string;
  to?: string;
  status?: string;
  page?: number;
  pageSize?: number;
}

function toParams(filters: BookingFilters): string {
  const params = new URLSearchParams();
  if (filters.resourceId) params.set("resourceId", filters.resourceId);
  if (filters.from) params.set("from", filters.from);
  if (filters.to) params.set("to", filters.to);
  if (filters.status) params.set("status", filters.status);
  if (filters.page) params.set("page", String(filters.page));
  if (filters.pageSize) params.set("pageSize", String(filters.pageSize));
  return params.toString();
}

export const bookingsApi = {
  getMine: (filters: BookingFilters = {}) =>
    apiClient.get<Paged<Booking>>(`/api/bookings?${toParams(filters)}`),
  getById: (id: string) => apiClient.get<ApiEnvelope<Booking>>(`/api/bookings/${id}`),
  create: (request: CreateBookingRequest) =>
    apiClient.post<ApiEnvelope<Booking>>("/api/bookings", request),
  cancel: (id: string) => apiClient.post<ApiEnvelope<Booking>>(`/api/bookings/${id}/cancel`),
};

export const adminApi = {
  getBookings: (filters: BookingFilters & { userId?: string } = {}) => {
    const params = toParams(filters);
    const userId = filters.userId ? `&userId=${filters.userId}` : "";
    return apiClient.get<Paged<Booking>>(`/api/admin/bookings?${params}${userId}`);
  },
  cancelBooking: (id: string) =>
    apiClient.post<ApiEnvelope<Booking>>(`/api/bookings/${id}/cancel`),
  getAuditLogs: (page = 1, pageSize = 20) =>
    apiClient.get<Paged<AuditLog>>(`/api/admin/audit-logs?page=${page}&pageSize=${pageSize}`),
};
