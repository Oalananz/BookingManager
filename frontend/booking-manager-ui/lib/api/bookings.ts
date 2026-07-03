import { apiClient } from "@/lib/api/client";
import { Booking, CreateBookingRequest } from "@/lib/types/booking";

export const bookingsApi = {
  getForResource: (resourceId: string, from?: string, to?: string) => {
    const params = new URLSearchParams({ resourceId });
    if (from) params.set("from", from);
    if (to) params.set("to", to);
    return apiClient.get<Booking[]>(`/api/bookings?${params.toString()}`);
  },
  create: (request: CreateBookingRequest) => apiClient.post<Booking>("/api/bookings", request),
  cancel: (id: string) => apiClient.post<Booking>(`/api/bookings/${id}/cancel`),
};
