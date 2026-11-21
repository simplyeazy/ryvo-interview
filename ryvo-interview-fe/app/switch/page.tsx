import { Skeleton } from "@/components/ui/skeleton";
import { Suspense } from "react";
import { UserRound } from "lucide-react";

import { switchUser } from "@/app/actions";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { apiFetch } from "@/lib/api";
import { getSessionUser } from "@/lib/session";
import type { User } from "@/lib/types";

export const metadata = { title: "Switch user" };

export default function SwitchUserPage() {
  return (
    <div className="mx-auto w-full max-w-4xl px-4 py-10">
      <h1 className="text-2xl font-semibold tracking-tight">Choose a user</h1>
      <p className="mt-1 max-w-2xl text-sm text-muted-foreground">
        Seeded development identities. The choice is stored in an HttpOnly cookie
        and forwarded to the API as <code className="font-mono text-xs">X-User-Id</code>
        {" "}on every server-side request.
      </p>

      <Suspense fallback={<UserGridSkeleton />}>
        <UserGrid />
      </Suspense>
    </div>
  );
}

async function UserGrid() {
  const [users, current] = await Promise.all([
    apiFetch<User[]>("/api/dev/users"),
    getSessionUser(),
  ]);

  return (
    <div className="mt-6 grid gap-3 sm:grid-cols-2">
      {users.map((user) => (
        <Card key={user.id} className={user.id === current?.id ? "ring-2 ring-ring" : undefined}>
          <CardHeader>
            <div className="flex items-center justify-between gap-2">
              <CardTitle className="flex items-center gap-2 text-base">
                <UserRound className="size-4 text-muted-foreground" />
                {user.name}
              </CardTitle>
              <Badge variant="secondary">{user.role}</Badge>
            </div>
            <CardDescription>
              {user.timeZoneId}
              {user.id === current?.id ? " · currently active" : ""}
            </CardDescription>
          </CardHeader>
          <CardContent>
            <form action={switchUser}>
              <input type="hidden" name="userId" value={user.id} />
              <Button type="submit" size="sm" variant={user.id === current?.id ? "outline" : "default"}>
                {user.id === current?.id ? "Still use this user" : "Continue as this user"}
              </Button>
            </form>
          </CardContent>
        </Card>
      ))}
    </div>
  );
}

function UserGridSkeleton() {
  return (
    <div className="mt-6 grid gap-3 sm:grid-cols-2">
      {Array.from({ length: 4 }, (_, index) => (
        <Skeleton key={index} className="h-36 w-full rounded-xl" />
      ))}
    </div>
  );
}
