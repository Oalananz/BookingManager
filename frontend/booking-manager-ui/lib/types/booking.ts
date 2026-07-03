export type BookingStatus = "Confirmed" | "Cancelled";

export interface Booking {
  id: string;
  resourceId: string;
  userId: string;
  startDateTime: string;
  endDateTime: string;
  status: BookingStatus;
  createdAt: string;
  cancelledAt: string | null;
}

export interface CreateBookingRequest {
  resourceId: string;
  userId: string;
  startDateTime: string;
  endDateTime: string;
}
