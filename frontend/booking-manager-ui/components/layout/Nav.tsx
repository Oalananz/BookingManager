"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useAuth } from "@/components/auth/AuthProvider";

const links = [
  { href: "/", label: "Dashboard" },
  { href: "/resources", label: "Resources" },
  { href: "/bookings", label: "My Bookings" },
  { href: "/create-booking", label: "Create Booking" },
];

export function Nav() {
  const pathname = usePathname();
  const { user, hydrated, logout } = useAuth();

  const visibleLinks = user?.role === "Admin"
    ? [...links, { href: "/admin", label: "Admin" }]
    : links;

  return (
    <header className="border-b border-zinc-200 dark:border-zinc-800">
      <nav className="mx-auto flex max-w-5xl items-center gap-6 px-6 py-4">
        <span className="font-semibold">BookingManager</span>
        {user && (
          <div className="flex gap-4 text-sm">
            {visibleLinks.map((link) => (
              <Link
                key={link.href}
                href={link.href}
                className={
                  pathname === link.href
                    ? "font-medium text-zinc-900 dark:text-zinc-50"
                    : "text-zinc-500 hover:text-zinc-900 dark:hover:text-zinc-50"
                }
              >
                {link.label}
              </Link>
            ))}
          </div>
        )}
        <div className="ml-auto flex items-center gap-3 text-sm">
          {hydrated && user ? (
            <>
              <span className="text-zinc-500">
                {user.fullName}
                {user.role === "Admin" && (
                  <span className="ml-1 rounded-full bg-zinc-900 px-2 py-0.5 text-xs text-white dark:bg-zinc-100 dark:text-zinc-900">
                    Admin
                  </span>
                )}
              </span>
              <button
                onClick={logout}
                className="text-zinc-500 hover:text-zinc-900 dark:hover:text-zinc-50"
              >
                Log out
              </button>
            </>
          ) : hydrated ? (
            <Link href="/login" className="text-zinc-500 hover:text-zinc-900 dark:hover:text-zinc-50">
              Log in
            </Link>
          ) : null}
        </div>
      </nav>
    </header>
  );
}
