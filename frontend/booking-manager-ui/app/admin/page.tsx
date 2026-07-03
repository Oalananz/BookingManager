"use client";

import { FormEvent, useCallback, useEffect, useState } from "react";
import { adminApi } from "@/lib/api/bookings";
import { resourcesApi } from "@/lib/api/resources";
import { Booking, AuditLog } from "@/lib/types/booking";
import { Resource, ResourceRequest, ResourceStatus } from "@/lib/types/resource";
import { PageMeta } from "@/lib/types/common";
import { formatDateTime } from "@/lib/utils/date";
import { BookingList } from "@/components/booking/BookingList";
import { Pagination } from "@/components/ui/Pagination";
import { RequireAuth } from "@/components/auth/RequireAuth";
import { Button } from "@/components/ui/Button";
import { Alert } from "@/components/ui/Alert";
import { Card } from "@/components/ui/Card";

const inputClass =
  "w-full rounded-md border border-zinc-300 px-3 py-2 text-sm dark:border-zinc-700 dark:bg-zinc-900";

type Tab = "bookings" | "resources" | "audit";

function AdminBookings() {
  const [bookings, setBookings] = useState<Booking[]>([]);
  const [meta, setMeta] = useState<PageMeta | null>(null);
  const [page, setPage] = useState(1);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setError(null);
    try {
      const result = await adminApi.getBookings({ page, pageSize: 20 });
      setBookings(result.data);
      setMeta(result.meta);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load bookings.");
    }
  }, [page]);

  useEffect(() => {
    load();
  }, [load]);

  async function handleCancel(id: string) {
    await adminApi.cancelBooking(id);
    await load();
  }

  return (
    <div className="space-y-4">
      {error && <Alert variant="error">{error}</Alert>}
      <BookingList bookings={bookings} onCancel={handleCancel} showUser />
      {meta && <Pagination meta={meta} onPageChange={setPage} />}
    </div>
  );
}

function AdminResources() {
  const [resources, setResources] = useState<Resource[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [editing, setEditing] = useState<Resource | null>(null);
  const [form, setForm] = useState<ResourceRequest>({
    name: "",
    type: "Room",
    description: "",
    capacity: null,
    status: "Available",
  });

  const load = useCallback(async () => {
    try {
      const { data } = await resourcesApi.getAll();
      setResources(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load resources.");
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  function startEdit(resource: Resource) {
    setEditing(resource);
    setForm({
      name: resource.name,
      type: resource.type,
      description: resource.description ?? "",
      capacity: resource.capacity,
      status: resource.status,
    });
  }

  function resetForm() {
    setEditing(null);
    setForm({ name: "", type: "Room", description: "", capacity: null, status: "Available" });
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);
    try {
      if (editing) {
        await resourcesApi.update(editing.id, form);
      } else {
        await resourcesApi.create(form);
      }
      resetForm();
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to save resource.");
    }
  }

  async function handleDelete(id: string) {
    setError(null);
    try {
      await resourcesApi.remove(id);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to delete resource.");
    }
  }

  return (
    <div className="space-y-6">
      {error && <Alert variant="error">{error}</Alert>}

      <Card>
        <h2 className="mb-4 font-medium">{editing ? `Edit: ${editing.name}` : "New resource"}</h2>
        <form onSubmit={handleSubmit} className="grid gap-4 sm:grid-cols-2">
          <div>
            <label htmlFor="res-name" className="mb-1 block text-sm font-medium">Name</label>
            <input
              id="res-name"
              required
              value={form.name}
              onChange={(e) => setForm({ ...form, name: e.target.value })}
              className={inputClass}
            />
          </div>
          <div>
            <label htmlFor="res-type" className="mb-1 block text-sm font-medium">Type</label>
            <input
              id="res-type"
              required
              value={form.type}
              onChange={(e) => setForm({ ...form, type: e.target.value })}
              className={inputClass}
            />
          </div>
          <div>
            <label htmlFor="res-capacity" className="mb-1 block text-sm font-medium">Capacity</label>
            <input
              id="res-capacity"
              type="number"
              min={1}
              value={form.capacity ?? ""}
              onChange={(e) =>
                setForm({ ...form, capacity: e.target.value ? Number(e.target.value) : null })
              }
              className={inputClass}
            />
          </div>
          <div>
            <label htmlFor="res-status" className="mb-1 block text-sm font-medium">Status</label>
            <select
              id="res-status"
              value={form.status}
              onChange={(e) => setForm({ ...form, status: e.target.value as ResourceStatus })}
              className={inputClass}
            >
              <option value="Available">Available</option>
              <option value="Maintenance">Maintenance</option>
              <option value="Disabled">Disabled</option>
            </select>
          </div>
          <div className="sm:col-span-2">
            <label htmlFor="res-description" className="mb-1 block text-sm font-medium">Description</label>
            <input
              id="res-description"
              value={form.description ?? ""}
              onChange={(e) => setForm({ ...form, description: e.target.value })}
              className={inputClass}
            />
          </div>
          <div className="flex gap-3 sm:col-span-2">
            <Button type="submit">{editing ? "Save changes" : "Create resource"}</Button>
            {editing && (
              <Button type="button" variant="secondary" onClick={resetForm}>
                Cancel edit
              </Button>
            )}
          </div>
        </form>
      </Card>

      <div className="overflow-x-auto rounded-lg border border-zinc-200 dark:border-zinc-800">
        <table className="w-full text-sm">
          <thead className="bg-zinc-50 text-left dark:bg-zinc-900">
            <tr>
              <th className="px-4 py-2 font-medium">Name</th>
              <th className="px-4 py-2 font-medium">Type</th>
              <th className="px-4 py-2 font-medium">Capacity</th>
              <th className="px-4 py-2 font-medium">Status</th>
              <th className="px-4 py-2" />
            </tr>
          </thead>
          <tbody>
            {resources.map((resource) => (
              <tr key={resource.id} className="border-t border-zinc-100 dark:border-zinc-800">
                <td className="px-4 py-2">{resource.name}</td>
                <td className="px-4 py-2">{resource.type}</td>
                <td className="px-4 py-2">{resource.capacity ?? "—"}</td>
                <td className="px-4 py-2">{resource.status}</td>
                <td className="px-4 py-2 text-right">
                  <div className="flex justify-end gap-2">
                    <Button variant="secondary" onClick={() => startEdit(resource)}>
                      Edit
                    </Button>
                    <Button variant="danger" onClick={() => handleDelete(resource.id)}>
                      Delete
                    </Button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

function AdminAuditLogs() {
  const [logs, setLogs] = useState<AuditLog[]>([]);
  const [meta, setMeta] = useState<PageMeta | null>(null);
  const [page, setPage] = useState(1);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    adminApi
      .getAuditLogs(page, 20)
      .then((result) => {
        setLogs(result.data);
        setMeta(result.meta);
      })
      .catch((err) => setError(err instanceof Error ? err.message : "Failed to load audit logs."));
  }, [page]);

  return (
    <div className="space-y-4">
      {error && <Alert variant="error">{error}</Alert>}
      <div className="overflow-x-auto rounded-lg border border-zinc-200 dark:border-zinc-800">
        <table className="w-full text-sm">
          <thead className="bg-zinc-50 text-left dark:bg-zinc-900">
            <tr>
              <th className="px-4 py-2 font-medium">When</th>
              <th className="px-4 py-2 font-medium">Action</th>
              <th className="px-4 py-2 font-medium">Entity</th>
              <th className="px-4 py-2 font-medium">Actor</th>
              <th className="px-4 py-2 font-medium">IP</th>
            </tr>
          </thead>
          <tbody>
            {logs.map((log) => (
              <tr key={log.id} className="border-t border-zinc-100 dark:border-zinc-800">
                <td className="px-4 py-2 whitespace-nowrap">{formatDateTime(log.createdAt)}</td>
                <td className="px-4 py-2">{log.action}</td>
                <td className="px-4 py-2">
                  {log.entityType}
                  {log.entityId && (
                    <span className="ml-1 font-mono text-xs text-zinc-500">
                      {log.entityId.slice(0, 8)}…
                    </span>
                  )}
                </td>
                <td className="px-4 py-2 font-mono text-xs">
                  {log.actorUserId ? `${log.actorUserId.slice(0, 8)}…` : "—"}
                </td>
                <td className="px-4 py-2">{log.ipAddress ?? "—"}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {meta && <Pagination meta={meta} onPageChange={setPage} />}
    </div>
  );
}

function AdminPanel() {
  const [tab, setTab] = useState<Tab>("bookings");

  const tabs: { id: Tab; label: string }[] = [
    { id: "bookings", label: "All Bookings" },
    { id: "resources", label: "Resources" },
    { id: "audit", label: "Audit Logs" },
  ];

  return (
    <div className="space-y-6">
      <h1 className="text-xl font-semibold">Admin</h1>
      <div className="flex gap-2 border-b border-zinc-200 dark:border-zinc-800">
        {tabs.map((t) => (
          <button
            key={t.id}
            onClick={() => setTab(t.id)}
            className={
              tab === t.id
                ? "border-b-2 border-zinc-900 px-3 py-2 text-sm font-medium dark:border-zinc-100"
                : "px-3 py-2 text-sm text-zinc-500 hover:text-zinc-900 dark:hover:text-zinc-50"
            }
          >
            {t.label}
          </button>
        ))}
      </div>
      {tab === "bookings" && <AdminBookings />}
      {tab === "resources" && <AdminResources />}
      {tab === "audit" && <AdminAuditLogs />}
    </div>
  );
}

export default function AdminPage() {
  return (
    <RequireAuth adminOnly>
      <AdminPanel />
    </RequireAuth>
  );
}
