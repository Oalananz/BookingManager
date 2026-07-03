"use client";

import { FormEvent, useState } from "react";
import { Resource } from "@/lib/types/resource";
import { Booking } from "@/lib/types/booking";
import { localInputToUtcIso } from "@/lib/utils/date";
import { Button } from "@/components/ui/Button";
import { Alert } from "@/components/ui/Alert";

export function BookingForm({
  resources,
  onSubmit,
}: {
  resources: Resource[];
  onSubmit: (request: {
    resourceId: string;
    userId: string;
    startDateTime: string;
    endDateTime: string;
  }) => Promise<Booking>;
}) {
  const [resourceId, setResourceId] = useState(resources[0]?.id ?? "");
  const [userId, setUserId] = useState("");
  const [start, setStart] = useState("");
  const [end, setEnd] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<Booking | null>(null);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);
    setSuccess(null);

    const trimmedUserId = userId.trim();

    if (!resourceId || !trimmedUserId || !start || !end) {
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
        userId: trimmedUserId,
        startDateTime: localInputToUtcIso(start),
        endDateTime: localInputToUtcIso(end),
      });
      setSuccess(booking);
      setUserId("");
      setStart("");
      setEnd("");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to create booking.");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      {error && <Alert variant="error">{error}</Alert>}
      {success && <Alert variant="success">Booking created for {success.userId}.</Alert>}

      <div>
        <label htmlFor="booking-resource" className="mb-1 block text-sm font-medium">Resource</label>
        <select
          id="booking-resource"
          value={resourceId}
          onChange={(e) => setResourceId(e.target.value)}
          className="w-full rounded-md border border-zinc-300 px-3 py-2 text-sm dark:border-zinc-700 dark:bg-zinc-900"
        >
          {resources.map((resource) => (
            <option key={resource.id} value={resource.id}>
              {resource.name}
            </option>
          ))}
        </select>
      </div>

      <div>
        <label htmlFor="booking-user-id" className="mb-1 block text-sm font-medium">User ID</label>
        <input
          id="booking-user-id"
          type="text"
          value={userId}
          onChange={(e) => setUserId(e.target.value)}
          placeholder="e.g. alice"
          className="w-full rounded-md border border-zinc-300 px-3 py-2 text-sm dark:border-zinc-700 dark:bg-zinc-900"
        />
      </div>

      <div className="grid gap-4 sm:grid-cols-2">
        <div>
          <label htmlFor="booking-start" className="mb-1 block text-sm font-medium">Start</label>
          <input
            id="booking-start"
            type="datetime-local"
            value={start}
            onChange={(e) => setStart(e.target.value)}
            className="w-full rounded-md border border-zinc-300 px-3 py-2 text-sm dark:border-zinc-700 dark:bg-zinc-900"
          />
        </div>
        <div>
          <label htmlFor="booking-end" className="mb-1 block text-sm font-medium">End</label>
          <input
            id="booking-end"
            type="datetime-local"
            value={end}
            onChange={(e) => setEnd(e.target.value)}
            className="w-full rounded-md border border-zinc-300 px-3 py-2 text-sm dark:border-zinc-700 dark:bg-zinc-900"
          />
        </div>
      </div>

      <Button type="submit" disabled={submitting}>
        {submitting ? "Creating..." : "Create Booking"}
      </Button>
    </form>
  );
}
