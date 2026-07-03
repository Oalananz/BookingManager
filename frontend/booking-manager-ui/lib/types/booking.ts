export type BookingStatus = "Active" | "Cancelled" | "Completed";

export interface Booking {
  id: string;
  resourceId: string;
  resourceName: string | null;
  userId: string;
  startDateTime: string;
  endDateTime: string;
  status: BookingStatus;
  createdAt?: string;
  updatedAt?: string;
  cancelledAt?: string | null;
  cancelledBy?: string | null;
}

export interface CreateBookingRequest {
  resourceId: string;
  startDateTime: string;
  endDateTime: string;
}

export interface AvailabilitySlot {
  start: string;
  end: string;
}

export interface Availability {
  resourceId: string;
  from: string;
  to: string;
  durationMinutes: number;
  slots: AvailabilitySlot[];
}

export interface AuditLog {
  id: number;
  actorUserId: string | null;
  action: string;
  entityType: string;
  entityId: string | null;
  oldValue: unknown;
  newValue: unknown;
  ipAddress: string | null;
  createdAt: string;
}
