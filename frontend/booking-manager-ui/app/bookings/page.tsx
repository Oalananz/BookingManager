"use client";

import { useCallback, useEffect, useState } from "react";
import { resourcesApi } from "@/lib/api/resources";
import { bookingsApi } from "@/lib/api/bookings";
import { Resource } from "@/lib/types/resource";
import { Booking } from "@/lib/types/booking";
import { localInputToUtcIso } from "@/lib/utils/date";
import { BookingList } from "@/components/booking/BookingList";
import { Alert } from "@/components/ui/Alert";

export default function BookingsPage() {
  const [resources, setResources] = useState<Resource[]>([]);
  const [resourceId, setResourceId] = useState("");
  const [from, setFrom] = useState("");
  const [to, setTo] = useState("");
  const [bookings, setBookings] = useState<Booking[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    resourcesApi.getAll().then((all) => {
      setResources(all);
      if (all[0]) setResourceId(all[0].id);
    });
  }, []);

  const loadBookings = useCallback(async () => {
    if (!resourceId) return;
    setLoading(true);
    setError(null);
    try {
      const results = await bookingsApi.getForResource(
        resourceId,
        from ? localInputToUtcIso(from) : undefined,
        to ? localInputToUtcIso(to) : undefined
      );
      setBookings(results);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load bookings.");
    } finally {
      setLoading(false);
    }
  }, [resourceId, from, to]);

  useEffect(() => {
    loadBookings();
  }, [loadBookings]);

  async function handleCancel(id: string) {
    await bookingsApi.cancel(id);
    await loadBookings();
  }

  if (resources.length === 0) {
    return (
      <div className="space-y-6">
        <h1 className="text-xl font-semibold">Bookings</h1>
        <p className="text-sm text-zinc-500">No resources available.</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <h1 className="text-xl font-semibold">Bookings</h1>

      <div className="grid gap-4 sm:grid-cols-3">
        <div>
          <label htmlFor="filter-resource" className="mb-1 block text-sm font-medium">Resource</label>
          <select
            id="filter-resource"
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
          <label htmlFor="filter-from" className="mb-1 block text-sm font-medium">From</label>
          <input
            id="filter-from"
            type="datetime-local"
            value={from}
            onChange={(e) => setFrom(e.target.value)}
            className="w-full rounded-md border border-zinc-300 px-3 py-2 text-sm dark:border-zinc-700 dark:bg-zinc-900"
          />
        </div>
        <div>
          <label htmlFor="filter-to" className="mb-1 block text-sm font-medium">To</label>
          <input
            id="filter-to"
            type="datetime-local"
            value={to}
            onChange={(e) => setTo(e.target.value)}
            className="w-full rounded-md border border-zinc-300 px-3 py-2 text-sm dark:border-zinc-700 dark:bg-zinc-900"
          />
        </div>
      </div>

      {error && <Alert variant="error">{error}</Alert>}
      {loading ? <p className="text-sm text-zinc-500">Loading...</p> : <BookingList bookings={bookings} onCancel={handleCancel} />}
    </div>
  );
}
