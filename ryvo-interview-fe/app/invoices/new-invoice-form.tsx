"use client";

import { useActionState } from "react";

import { generateInvoice } from "@/app/actions";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

interface NewInvoiceFormProps {
  /** Range defaults, computed in the client's time zone on the server. */
  defaultStart: string;
  defaultEnd: string;
}

export function NewInvoiceForm({ defaultStart, defaultEnd }: NewInvoiceFormProps) {
  const [state, formAction, pending] = useActionState(generateInvoice, {});

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
            defaultValue={defaultStart}
          />
        </div>
        <div className="grid gap-1.5">
          <Label htmlFor="rangeEnd">To</Label>
          <Input
            id="rangeEnd"
            name="rangeEnd"
            type="date"
            required
            defaultValue={defaultEnd}
          />
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
