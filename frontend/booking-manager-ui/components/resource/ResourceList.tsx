import { Resource } from "@/lib/types/resource";
import { Card } from "@/components/ui/Card";

export function ResourceList({ resources }: { resources: Resource[] }) {
  if (resources.length === 0) {
    return <p className="text-sm text-zinc-500">No resources found.</p>;
  }

  return (
    <div className="grid gap-4 sm:grid-cols-2">
      {resources.map((resource) => (
        <Card key={resource.id}>
          <p className="font-medium">{resource.name}</p>
          <p className="text-sm text-zinc-500">{resource.type}</p>
        </Card>
      ))}
    </div>
  );
}
