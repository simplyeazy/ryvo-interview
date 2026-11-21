"use client";

import { useActionState } from "react";

import { addEntry } from "@/app/actions";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { addDays } from "@/lib/dates";

interface AddEntryFormProps {
  engagementId: string;
  weekStart: string;
  weekEnd: string;
}

/**
 * Posts wall-clock times. The Server Action converts them to UTC using the
 * contractor's time zone before calling the API.
 */
export function AddEntryForm({ engagementId, weekStart, weekEnd }: AddEntryFormProps) {
  const [state, formAction, pending] = useActionState(addEntry, {});

  return (
    <form action={formAction} className="grid gap-4 sm:grid-cols-2">
      <input type="hidden" name="engagementId" value={engagementId} />
      <input type="hidden" name="weekOf" value={weekStart} />

      <div className="grid gap-1.5">
        <Label htmlFor="startLocal">Start</Label>
        <Input
          id="startLocal"
          name="startLocal"
          type="datetime-local"
          required
          defaultValue={`${weekStart}T09:00`}
          min={`${weekStart}T00:00`}
          max={`${weekEnd}T23:59`}
        />
      </div>

      <div className="grid gap-1.5">
        <Label htmlFor="endLocal">End</Label>
        <Input
          id="endLocal"
          name="endLocal"
          type="datetime-local"
          required
          defaultValue={`${weekStart}T17:00`}
          min={`${weekStart}T00:00`}
          max={`${addDays(weekEnd, 1)}T00:00`}
        />
      </div>

      <div className="grid gap-1.5 sm:col-span-2">
        <Label htmlFor="description">Description</Label>
        <Input
          id="description"
          name="description"
          placeholder="What did you work on?"
          maxLength={500}
        />
      </div>

      <div className="flex flex-col gap-2 sm:col-span-2">
        <Button type="submit" disabled={pending} className="self-start">
          {pending ? "Adding…" : "Add entry"}
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
