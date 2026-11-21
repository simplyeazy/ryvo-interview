import { redirect } from "next/navigation";
import { Suspense } from "react";

import { getSessionUser } from "@/lib/session";

export default function Home() {
  return (
    <div className="mx-auto w-full max-w-6xl px-4 py-10">
      <Suspense fallback={<p className="text-sm text-muted-foreground">Loading…</p>}>
        <RoleRedirect />
      </Suspense>
    </div>
  );
}

/** Sends each role to the screen it can actually use. */
async function RoleRedirect() {
  const user = await getSessionUser();

  if (!user) return redirect("/switch");
  return redirect(user.role === "Contractor" ? "/timesheets" : "/review");
}
