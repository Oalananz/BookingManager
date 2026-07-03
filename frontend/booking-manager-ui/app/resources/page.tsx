"use client";

import { useEffect, useState } from "react";
import { resourcesApi } from "@/lib/api/resources";
import { Resource } from "@/lib/types/resource";
import { ResourceList } from "@/components/resource/ResourceList";
import { Alert } from "@/components/ui/Alert";

export default function ResourcesPage() {
  const [resources, setResources] = useState<Resource[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    resourcesApi
      .getAll()
      .then(setResources)
      .catch((err) => setError(err instanceof Error ? err.message : "Failed to load resources."))
      .finally(() => setLoading(false));
  }, []);

  return (
    <div className="space-y-6">
      <h1 className="text-xl font-semibold">Resources</h1>
      {error && <Alert variant="error">{error}</Alert>}
      {loading ? <p className="text-sm text-zinc-500">Loading...</p> : <ResourceList resources={resources} />}
    </div>
  );
}
