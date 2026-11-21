"use client";

import { useActionState } from "react";

import { generateInvoice } from "@/app/actions";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { mondayOf, todayIso } from "@/lib/dates";

export function NewInvoiceForm() {
  const [state, formAction, pending] = useActionState(generateInvoice, {});
  const today = todayIso();

  return (
    <form action={formAction} className="flex flex-col gap-4">
      <div className="grid gap-4 sm:grid-cols-2">
        <div className="grid gap-1.5">
          <Label htmlFor="rangeStart">From</Label>
          <Input
            id="rangeStart"
            name="rangeStart"
            type="date"
            required
            defaultValue={mondayOf(today)}
          />
        </div>
        <div className="grid gap-1.5">
          <Label htmlFor="rangeEnd">To</Label>
          <Input id="rangeEnd" name="rangeEnd" type="date" required defaultValue={today} />
        </div>
      </div>

      <div className="flex flex-col gap-2">
        <Button type="submit" disabled={pending} className="self-start">
          {pending ? "Generating…" : "Generate invoice"}
        </Button>
        {state.error ? (
          <p role="alert" className="text-sm text-destructive">
            {state.error}
          </p>
        ) : null}
      </div>
    </form>
  );
}
