"use client";

import { FormEvent, useState } from "react";
import { Resource } from "@/lib/types/resource";
import { Booking, AvailabilitySlot } from "@/lib/types/booking";
import { resourcesApi } from "@/lib/api/resources";
import { localInputToUtcIso, formatDateTime } from "@/lib/utils/date";
import { Button } from "@/components/ui/Button";
import { Alert } from "@/components/ui/Alert";

const inputClass =
  "w-full rounded-md border border-zinc-300 px-3 py-2 text-sm dark:border-zinc-700 dark:bg-zinc-900";

export function BookingForm({
  resources,
  onSubmit,
}: {
  resources: Resource[];
  onSubmit: (request: {
    resourceId: string;
    startDateTime: string;
    endDateTime: string;
  }) => Promise<Booking>;
}) {
  const bookable = resources.filter((r) => r.status === "Available");
  const [resourceId, setResourceId] = useState(bookable[0]?.id ?? "");
  const [start, setStart] = useState("");
  const [end, setEnd] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<Booking | null>(null);
  const [slots, setSlots] = useState<AvailabilitySlot[] | null>(null);
  const [checkingSlots, setCheckingSlots] = useState(false);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);
    setSuccess(null);

    if (!resourceId || !start || !end) {
      setError("Please fill in all fields.");
      return;
    }

    if (new Date(end) <= new Date(start)) {
      setError("End time must be after start time.");
      return;
    }

    setSubmitting(true);
    try {
      const booking = await onSubmit({
        resourceId,
        startDateTime: localInputToUtcIso(start),
        endDateTime: localInputToUtcIso(end),
      });
      setSuccess(booking);
      setStart("");
      setEnd("");
      setSlots(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to create booking.");
    } finally {
      setSubmitting(false);
    }
  }

  async function checkAvailability() {
    if (!resourceId || !start) {
      setError("Pick a resource and a start time first — availability is shown for that day.");
      return;
    }
    setError(null);
    setCheckingSlots(true);
    try {
      const day = new Date(start);
      const from = new Date(day.getFullYear(), day.getMonth(), day.getDate(), 0, 0);
      const to = new Date(day.getFullYear(), day.getMonth(), day.getDate() + 1, 0, 0);
      const { data } = await resourcesApi.getAvailability(
        resourceId,
        from.toISOString(),
        to.toISOString(),
        60
      );
      setSlots(data.slots);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load availability.");
    } finally {
      setCheckingSlots(false);
    }
  }

  if (bookable.length === 0) {
    return <p className="text-sm text-zinc-500">No available resources to book.</p>;
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      {error && <Alert variant="error">{error}</Alert>}
      {success && (
        <Alert variant="success">
          Booking created: {success.resourceName ?? "resource"} from{" "}
          {formatDateTime(success.startDateTime)} to {formatDateTime(success.endDateTime)}.
        </Alert>
      )}

      <div>
        <label htmlFor="booking-resource" className="mb-1 block text-sm font-medium">Resource</label>
        <select
          id="booking-resource"
          value={resourceId}
          onChange={(e) => {
            setResourceId(e.target.value);
            setSlots(null);
          }}
          className={inputClass}
        >
          {bookable.map((resource) => (
            <option key={resource.id} value={resource.id}>
              {resource.name}
            </option>
          ))}
        </select>
      </div>

      <div className="grid gap-4 sm:grid-cols-2">
        <div>
          <label htmlFor="booking-start" className="mb-1 block text-sm font-medium">Start</label>
          <input
            id="booking-start"
            type="datetime-local"
            value={start}
            onChange={(e) => setStart(e.target.value)}
            className={inputClass}
          />
        </div>
        <div>
          <label htmlFor="booking-end" className="mb-1 block text-sm font-medium">End</label>
          <input
            id="booking-end"
            type="datetime-local"
            value={end}
            onChange={(e) => setEnd(e.target.value)}
            className={inputClass}
          />
        </div>
      </div>

      <div className="flex gap-3">
        <Button type="submit" disabled={submitting}>
          {submitting ? "Creating..." : "Create Booking"}
        </Button>
        <Button type="button" variant="secondary" disabled={checkingSlots} onClick={checkAvailability}>
          {checkingSlots ? "Checking..." : "Check availability"}
        </Button>
      </div>

      {slots !== null && (
        <div className="rounded-lg border border-zinc-200 p-4 text-sm dark:border-zinc-800">
          <p className="mb-2 font-medium">Free slots that day (≥ 1 hour)</p>
          {slots.length === 0 ? (
            <p className="text-zinc-500">No free slots.</p>
          ) : (
            <ul className="space-y-1 text-zinc-600 dark:text-zinc-400">
              {slots.map((slot) => (
                <li key={slot.start}>
                  {formatDateTime(slot.start)} → {formatDateTime(slot.end)}
                </li>
              ))}
            </ul>
          )}
        </div>
      )}
    </form>
  );
}
