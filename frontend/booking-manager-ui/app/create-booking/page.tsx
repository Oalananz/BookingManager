"use client";

import { useEffect, useState } from "react";
import { resourcesApi } from "@/lib/api/resources";
import { bookingsApi } from "@/lib/api/bookings";
import { Resource } from "@/lib/types/resource";
import { CreateBookingRequest } from "@/lib/types/booking";
import { BookingForm } from "@/components/booking/BookingForm";
import { RequireAuth } from "@/components/auth/RequireAuth";
import { Alert } from "@/components/ui/Alert";

function CreateBooking() {
  const [resources, setResources] = useState<Resource[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    resourcesApi
      .getAll()
      .then(({ data }) => setResources(data))
      .catch((err) => setError(err instanceof Error ? err.message : "Failed to load resources."))
      .finally(() => setLoading(false));
  }, []);

  async function handleSubmit(request: CreateBookingRequest) {
    const { data } = await bookingsApi.create(request);
    return data;
  }

  return (
    <div className="max-w-lg space-y-6">
      <h1 className="text-xl font-semibold">Create Booking</h1>
      {error && <Alert variant="error">{error}</Alert>}
      {loading ? (
        <p className="text-sm text-zinc-500">Loading...</p>
      ) : resources.length === 0 ? (
        <p className="text-sm text-zinc-500">No resources available.</p>
      ) : (
        <BookingForm resources={resources} onSubmit={handleSubmit} />
      )}
    </div>
  );
}

export default function CreateBookingPage() {
  return (
    <RequireAuth>
      <CreateBooking />
    </RequireAuth>
  );
}
