export function localInputToUtcIso(localValue: string): string {
  return new Date(localValue).toISOString();
}

export function formatDateTime(isoValue: string): string {
  return new Date(isoValue).toLocaleString(undefined, {
    dateStyle: "medium",
    timeStyle: "short",
  });
}
