type Variant = "error" | "success";

const variantClasses: Record<Variant, string> = {
  error: "bg-red-50 text-red-800 border-red-200 dark:bg-red-950 dark:text-red-200 dark:border-red-900",
  success:
    "bg-green-50 text-green-800 border-green-200 dark:bg-green-950 dark:text-green-200 dark:border-green-900",
};

export function Alert({ variant, children }: { variant: Variant; children: React.ReactNode }) {
  return (
    <div className={`rounded-md border px-4 py-3 text-sm ${variantClasses[variant]}`}>
      {children}
    </div>
  );
}
