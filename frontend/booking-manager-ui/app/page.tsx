import Link from "next/link";
import { Card } from "@/components/ui/Card";

const links = [
  { href: "/resources", title: "Resources", description: "Browse meeting rooms and equipment." },
  { href: "/create-booking", title: "Create Booking", description: "Reserve a resource for a time window." },
  { href: "/bookings", title: "Bookings", description: "View and cancel bookings for a resource." },
];

export default function Home() {
  return (
    <div className="space-y-8">
      <div>
        <h1 className="text-2xl font-semibold">BookingManager</h1>
        <p className="mt-1 text-sm text-zinc-500">
          Manage bookings for shared meeting rooms and equipment.
        </p>
      </div>
      <div className="grid gap-4 sm:grid-cols-3">
        {links.map((link) => (
          <Link key={link.href} href={link.href}>
            <Card className="h-full transition-colors hover:border-zinc-400 dark:hover:border-zinc-600">
              <p className="font-medium">{link.title}</p>
              <p className="mt-1 text-sm text-zinc-500">{link.description}</p>
            </Card>
          </Link>
        ))}
      </div>
    </div>
  );
}
