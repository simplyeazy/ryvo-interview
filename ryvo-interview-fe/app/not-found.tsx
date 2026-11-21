import Link from "next/link";

import { Button } from "@/components/ui/button";

export default function NotFound() {
  return (
    <div className="mx-auto flex w-full max-w-xl flex-col items-center gap-3 px-4 py-24 text-center">
      <p className="text-sm font-medium text-muted-foreground">404</p>
      <h1 className="text-2xl font-semibold tracking-tight">Not found</h1>
      <p className="max-w-sm text-sm text-muted-foreground">
        That page or invoice does not exist, or it belongs to a different client.
      </p>
      <Button asChild variant="outline" size="sm">
        <Link href="/">Back to start</Link>
      </Button>
    </div>
  );
}
