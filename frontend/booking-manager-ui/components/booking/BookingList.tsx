"use client";

import { useState } from "react";
import { Booking } from "@/lib/types/booking";
import { formatDateTime } from "@/lib/utils/date";
import { Button } from "@/components/ui/Button";
import { Alert } from "@/components/ui/Alert";

export function BookingList({
  bookings,
  onCancel,
}: {
  bookings: Booking[];
  onCancel: (id: string) => Promise<void>;
}) {
  const [cancellingId, setCancellingId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function handleCancel(id: string) {
    setError(null);
    setCancellingId(id);
    try {
      await onCancel(id);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to cancel booking.");
    } finally {
      setCancellingId(null);
    }
  }

  if (bookings.length === 0) {
    return <p className="text-sm text-zinc-500">No bookings found for this resource and date range.</p>;
  }

  return (
    <div className="space-y-3">
      {error && <Alert variant="error">{error}</Alert>}
      <div className="overflow-x-auto rounded-lg border border-zinc-200 dark:border-zinc-800">
        <table className="w-full text-sm">
          <thead className="bg-zinc-50 text-left dark:bg-zinc-900">
            <tr>
              <th className="px-4 py-2 font-medium">User</th>
              <th className="px-4 py-2 font-medium">Start</th>
              <th className="px-4 py-2 font-medium">End</th>
              <th className="px-4 py-2 font-medium">Status</th>
              <th className="px-4 py-2" />
            </tr>
          </thead>
          <tbody>
            {bookings.map((booking) => (
              <tr key={booking.id} className="border-t border-zinc-100 dark:border-zinc-800">
                <td className="px-4 py-2">{booking.userId}</td>
                <td className="px-4 py-2">{formatDateTime(booking.startDateTime)}</td>
                <td className="px-4 py-2">{formatDateTime(booking.endDateTime)}</td>
                <td className="px-4 py-2">
                  <span
                    className={
                      booking.status === "Confirmed"
                        ? "rounded-full bg-green-100 px-2 py-0.5 text-xs text-green-800 dark:bg-green-950 dark:text-green-200"
                        : "rounded-full bg-zinc-100 px-2 py-0.5 text-xs text-zinc-600 dark:bg-zinc-800 dark:text-zinc-400"
                    }
                  >
                    {booking.status}
                  </span>
                </td>
                <td className="px-4 py-2 text-right">
                  {booking.status === "Confirmed" && (
                    <Button
                      variant="danger"
                      disabled={cancellingId === booking.id}
                      onClick={() => handleCancel(booking.id)}
                    >
                      {cancellingId === booking.id ? "Cancelling..." : "Cancel"}
                    </Button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
