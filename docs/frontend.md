# Frontend

Next.js (App Router) + TypeScript, no auth/state-management/form libraries — a hand-rolled `AuthProvider` context, plain `useState`/`useEffect` data fetching, and a small typed `fetch` wrapper. Kept intentionally simple: this exercises the API end to end, it isn't a polished product.

## Routes and access control

```mermaid
graph TD
    Login["/login (public)"] --> Dash
    Register["/register (public)"] --> Dash
    Dash["/ Dashboard (any user)"]
    Dash --> Resources["/resources (any user)"]
    Dash --> Create["/create-booking (any user)"]
    Dash --> Bookings["/bookings — My Bookings (any user)"]
    Dash -.admin only.-> Admin["/admin (Admin role)"]

    Admin --> AdminBookings["tab: All Bookings"]
    Admin --> AdminResources["tab: Resources CRUD"]
    Admin --> AdminAudit["tab: Audit Logs"]
```

`/admin` is a single page with three client-side tabs (not separate routes) — `AdminBookings`, `AdminResources`, `AdminAuditLogs` are local components inside `app/admin/page.tsx`.

Access control is enforced entirely by a client-side `<RequireAuth>` wrapper component — there is **no** Next.js `middleware.ts`, so protected pages' JS still ships to an unauthenticated browser before the redirect fires (a brief "Loading..." flash is expected, not a bug).

## Auth flow

```mermaid
sequenceDiagram
    participant U as Browser
    participant P as AuthProvider (React Context)
    participant API as Backend
    participant LS as localStorage

    U->>P: login(email, password)
    P->>API: POST /api/auth/login
    API-->>P: { accessToken, expiresAt, user }
    P->>LS: save bm.token, bm.user
    P->>P: setUser(user)
    P-->>U: redirect to /

    Note over U,P: On every subsequent page load
    P->>LS: read bm.user (hydration)
    P-->>U: user available once hydrated=true

    Note over U,API: On any API call
    U->>API: request with Authorization: Bearer <token>
    API-->>U: 401 if expired/invalid
    U->>LS: clear() on 401
    U->>U: window.location.href = "/login"
```

Session storage is plain `localStorage` (keys `bm.token`, `bm.user`) — no cookies, no server-side session. A global `401` from any API call forces a full logout + redirect, so an expired token can't leave the UI in a half-authenticated state.

## Component structure

```mermaid
graph TD
    Layout["app/layout.tsx (AuthProvider + Nav)"]
    Layout --> Nav["Nav — role-aware links, user pill, log out"]
    Layout --> Pages

    subgraph Pages
        Dashboard["/ — nav cards, admin card if Admin"]
        ResourcesPage["/resources — ResourceList"]
        CreatePage["/create-booking — BookingForm"]
        BookingsPage["/bookings — filters + BookingList"]
        AdminPage["/admin — 3 tabs"]
    end

    ResourcesPage --> ResourceList["components/resource/ResourceList<br/>(status badge, capacity)"]
    CreatePage --> BookingForm["components/booking/BookingForm<br/>(resource select, availability check, submit)"]
    BookingsPage --> BookingList["components/booking/BookingList<br/>(status badge, cancel action, optional user column)"]
    AdminPage --> BookingList
    AdminPage --> ResourceCRUD["inline CRUD form + table"]
    AdminPage --> AuditTable["inline audit log table"]

    BookingList --> UI["components/ui: Button, Card, Alert, Pagination"]
    ResourceList --> UI
```

## Data flow: API client

```mermaid
graph LR
    Component --> apiClient["lib/api/client.ts<br/>fetch wrapper"]
    apiClient -->|"Authorization: Bearer <token>"| Backend[(Backend API)]
    Backend -->|"{ data } / { data, meta } / { code, message }"| apiClient
    apiClient -->|throws ApiError on non-2xx| Component
    apiClient -->|401 with existing token| ForceLogout["clear() + redirect /login"]
```

`lib/api/` is split by resource: `auth.ts`, `bookings.ts` (also holds `adminApi` for admin bookings/audit-log endpoints), `resources.ts`. Response types in `lib/types/common.ts` mirror the backend's envelope exactly: `ApiEnvelope<T> = { data: T }` and `Paged<T> = { data: T[], meta: PageMeta }`.

## Key types

```ts
type BookingStatus = "Active" | "Cancelled" | "Completed";
type ResourceStatus = "Available" | "Maintenance" | "Disabled";
type UserRole = "User" | "Admin";

interface Booking {
  id: string; resourceId: string; resourceName: string | null; userId: string;
  startDateTime: string; endDateTime: string; status: BookingStatus;
  cancelledAt: string | null; cancelledBy: string | null;
}
```

These mirror the backend's `BookingStatus`/`ResourceStatus` enums and DTO shapes exactly — see [`backend.md`](backend.md) for the source of truth.

## Notable UX details

- `BookingForm` filters the resource dropdown to `status === "Available"` only, and has a "check availability" action that calls `GET /api/resources/{id}/availability` to show free slots before submitting.
- `BookingList` shows a color-coded status badge and only renders the Cancel button when `status === "Active"`.
- Datetime inputs are plain `datetime-local` fields; `lib/utils/date.ts` converts the local input to a UTC ISO string on the way out and formats ISO timestamps back to the viewer's local time for display.
