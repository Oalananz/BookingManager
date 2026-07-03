"use client";

import { PageMeta } from "@/lib/types/common";
import { Button } from "@/components/ui/Button";

export function Pagination({
  meta,
  onPageChange,
}: {
  meta: PageMeta;
  onPageChange: (page: number) => void;
}) {
  if (meta.totalPages <= 1) return null;

  return (
    <div className="flex items-center justify-between text-sm">
      <span className="text-zinc-500">
        Page {meta.page} of {meta.totalPages} · {meta.totalCount} total
      </span>
      <div className="flex gap-2">
        <Button
          variant="secondary"
          disabled={meta.page <= 1}
          onClick={() => onPageChange(meta.page - 1)}
        >
          Previous
        </Button>
        <Button
          variant="secondary"
          disabled={meta.page >= meta.totalPages}
          onClick={() => onPageChange(meta.page + 1)}
        >
          Next
        </Button>
      </div>
    </div>
  );
}
