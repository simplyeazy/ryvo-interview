import Link from "next/link";
import { Suspense } from "react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { getSessionUser } from "@/lib/session";

/**
 * Static shell plus a suspended session section: reading the cookie happens in
 * `HeaderContent`, so the header itself stays out of the dynamic path and the
 * page can stream (required with Cache Components enabled).
 */
export function AppHeader() {
  return (
    <header className="border-b bg-background">
      <div className="mx-auto flex h-14 w-full max-w-6xl items-center gap-3 px-4">
        <Link href="/" className="font-semibold tracking-tight">
          Ryvo
        </Link>
        <Suspense fallback={<div className="ml-auto h-7 w-32 animate-pulse rounded-md bg-muted" />}>
          <HeaderContent />
        </Suspense>
      </div>
    </header>
  );
}

async function HeaderContent() {
  const user = await getSessionUser();

  if (!user) {
    return (
      <div className="ml-auto">
        <Button asChild size="sm">
          <Link href="/switch">Choose a user</Link>
        </Button>
      </div>
    );
  }

  return (
    <>
      <nav className="flex items-center gap-1 text-sm">
        {user.role === "Contractor" ? (
          <NavLink href="/timesheets">Timesheets</NavLink>
        ) : (
          <>
            <NavLink href="/review">Review</NavLink>
            <NavLink href="/invoices">Invoices</NavLink>
          </>
        )}
      </nav>

      <div className="ml-auto flex items-center gap-2">
        <span className="hidden text-sm text-muted-foreground sm:inline">{user.name}</span>
        <Badge variant="secondary">{user.role}</Badge>
        <Button asChild variant="outline" size="sm">
          <Link href="/switch">Switch</Link>
        </Button>
      </div>
    </>
  );
}

function NavLink({ href, children }: { href: string; children: React.ReactNode }) {
  return (
    <Link
      href={href}
      className="rounded-md px-2 py-1 text-muted-foreground transition-colors hover:bg-muted hover:text-foreground"
    >
      {children}
    </Link>
  );
}
