"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/components/auth/AuthProvider";

export function RequireAuth({
  children,
  adminOnly = false,
}: {
  children: React.ReactNode;
  adminOnly?: boolean;
}) {
  const { user, hydrated } = useAuth();
  const router = useRouter();

  useEffect(() => {
    if (!hydrated) return;
    if (!user) {
      router.replace("/login");
    } else if (adminOnly && user.role !== "Admin") {
      router.replace("/");
    }
  }, [hydrated, user, adminOnly, router]);

  if (!hydrated || !user || (adminOnly && user.role !== "Admin")) {
    return <p className="text-sm text-zinc-500">Loading...</p>;
  }

  return <>{children}</>;
}
