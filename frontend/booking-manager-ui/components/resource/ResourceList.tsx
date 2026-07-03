import { Resource, ResourceStatus } from "@/lib/types/resource";
import { Card } from "@/components/ui/Card";

const statusClasses: Record<ResourceStatus, string> = {
  Available:
    "rounded-full bg-green-100 px-2 py-0.5 text-xs text-green-800 dark:bg-green-950 dark:text-green-200",
  Maintenance:
    "rounded-full bg-amber-100 px-2 py-0.5 text-xs text-amber-800 dark:bg-amber-950 dark:text-amber-200",
  Disabled:
    "rounded-full bg-zinc-100 px-2 py-0.5 text-xs text-zinc-600 dark:bg-zinc-800 dark:text-zinc-400",
};

export function ResourceList({ resources }: { resources: Resource[] }) {
  if (resources.length === 0) {
    return <p className="text-sm text-zinc-500">No resources found.</p>;
  }

  return (
    <div className="grid gap-4 sm:grid-cols-2">
      {resources.map((resource) => (
        <Card key={resource.id}>
          <div className="flex items-start justify-between">
            <p className="font-medium">{resource.name}</p>
            <span className={statusClasses[resource.status]}>{resource.status}</span>
          </div>
          <p className="text-sm text-zinc-500">
            {resource.type}
            {resource.capacity != null && ` · capacity ${resource.capacity}`}
          </p>
        </Card>
      ))}
    </div>
  );
}
