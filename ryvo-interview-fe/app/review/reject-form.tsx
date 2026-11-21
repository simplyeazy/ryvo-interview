"use client";

import { useActionState } from "react";

import { rejectWeek } from "@/app/actions";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";

interface RejectFormProps {
  engagementId: string;
  weekOf: string;
}

export function RejectForm({ engagementId, weekOf }: RejectFormProps) {
  const [state, formAction, pending] = useActionState(rejectWeek, {});

  return (
    <form action={formAction} className="flex flex-col gap-2">
      <input type="hidden" name="engagementId" value={engagementId} />
      <input type="hidden" name="weekOf" value={weekOf} />

      <Label htmlFor={`comment-${weekOf}`} className="text-xs text-muted-foreground">
        Send back with a comment
      </Label>
      <div className="flex flex-col gap-2 sm:flex-row">
        <Textarea
          id={`comment-${weekOf}`}
          name="comment"
          rows={2}
          required
          placeholder="What needs to change?"
          className="min-h-9 flex-1 resize-none"
        />
        <Button
          type="submit"
          variant="destructive"
          disabled={pending}
          className="self-start sm:self-auto"
        >
          {pending ? "Sending…" : "Reject"}
        </Button>
      </div>
      {state.error ? (
        <p role="alert" className="text-sm text-destructive">
          {state.error}
        </p>
      ) : null}
    </form>
  );
}
