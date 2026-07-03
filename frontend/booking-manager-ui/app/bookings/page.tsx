"use client";

import { useCallback, useEffect, useState } from "react";
import { resourcesApi } from "@/lib/api/resources";
import { bookingsApi } from "@/lib/api/bookings";
import { Resource } from "@/lib/types/resource";
import { Booking } from "@/lib/types/booking";
import { PageMeta } from "@/lib/types/common";
import { localInputToUtcIso } from "@/lib/utils/date";
import { BookingList } from "@/components/booking/BookingList";
import { Pagination } from "@/components/ui/Pagination";
import { RequireAuth } from "@/components/auth/RequireAuth";
import { Alert } from "@/components/ui/Alert";

const inputClass =
  "w-full rounded-md border border-zinc-300 px-3 py-2 text-sm dark:border-zinc-700 dark:bg-zinc-900";

function MyBookings() {
  const [resources, setResources] = useState<Resource[]>([]);
  const [resourceId, setResourceId] = useState("");
  const [status, setStatus] = useState("");
  const [from, setFrom] = useState("");
  const [to, setTo] = useState("");
  const [page, setPage] = useState(1);
  const [bookings, setBookings] = useState<Booking[]>([]);
  const [meta, setMeta] = useState<PageMeta | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    resourcesApi.getAll().then(({ data }) => setResources(data)).catch(() => undefined);
  }, []);

  const loadBookings = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const result = await bookingsApi.getMine({
        resourceId: resourceId || undefined,
        status: status || undefined,
        from: from ? localInputToUtcIso(from) : undefined,
        to: to ? localInputToUtcIso(to) : undefined,
        page,
        pageSize: 20,
      });
      setBookings(result.data);
      setMeta(result.meta);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load bookings.");
    } finally {
      setLoading(false);
    }
  }, [resourceId, status, from, to, page]);

  useEffect(() => {
    loadBookings();
  }, [loadBookings]);

  async function handleCancel(id: string) {
    await bookingsApi.cancel(id);
    await loadBookings();
  }

  return (
    <div className="space-y-6">
      <h1 className="text-xl font-semibold">My Bookings</h1>

      <div className="grid gap-4 sm:grid-cols-4">
        <div>
          <label htmlFor="filter-resource" className="mb-1 block text-sm font-medium">Resource</label>
          <select
            id="filter-resource"
            value={resourceId}
            onChange={(e) => {
              setResourceId(e.target.value);
              setPage(1);
            }}
            className={inputClass}
          >
            <option value="">All resources</option>
            {resources.map((resource) => (
              <option key={resource.id} value={resource.id}>
                {resource.name}
              </option>
            ))}
          </select>
        </div>
        <div>
          <label htmlFor="filter-status" className="mb-1 block text-sm font-medium">Status</label>
          <select
            id="filter-status"
            value={status}
            onChange={(e) => {
              setStatus(e.target.value);
              setPage(1);
            }}
            className={inputClass}
          >
            <option value="">All statuses</option>
            <option value="Active">Active</option>
            <option value="Completed">Completed</option>
            <option value="Cancelled">Cancelled</option>
          </select>
        </div>
        <div>
          <label htmlFor="filter-from" className="mb-1 block text-sm font-medium">From</label>
          <input
            id="filter-from"
            type="datetime-local"
            value={from}
            onChange={(e) => {
              setFrom(e.target.value);
              setPage(1);
            }}
            className={inputClass}
          />
        </div>
        <div>
          <label htmlFor="filter-to" className="mb-1 block text-sm font-medium">To</label>
          <input
            id="filter-to"
            type="datetime-local"
            value={to}
            onChange={(e) => {
              setTo(e.target.value);
              setPage(1);
            }}
            className={inputClass}
          />
        </div>
      </div>

      {error && <Alert variant="error">{error}</Alert>}
      {loading ? (
        <p className="text-sm text-zinc-500">Loading...</p>
      ) : (
        <>
          <BookingList bookings={bookings} onCancel={handleCancel} />
          {meta && <Pagination meta={meta} onPageChange={setPage} />}
        </>
      )}
    </div>
  );
}

export default function BookingsPage() {
  return (
    <RequireAuth>
      <MyBookings />
    </RequireAuth>
  );
}
