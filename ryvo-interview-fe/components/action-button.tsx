"use client";

import { useActionState } from "react";

import { Button } from "@/components/ui/button";
import type { ActionState } from "@/lib/types";

type ButtonProps = React.ComponentProps<typeof Button>;

interface ActionButtonProps {
  action: (state: ActionState, formData: FormData) => Promise<ActionState>;
  /** Hidden inputs posted with the form. */
  fields?: Record<string, string>;
  children: React.ReactNode;
  pendingLabel?: string;
  variant?: ButtonProps["variant"];
  size?: ButtonProps["size"];
  className?: string;
}

/**
 * A one-button form bound to a Server Action. Owns the pending state and renders
 * the API's ProblemDetails message when the action reports a domain violation.
 */
export function ActionButton({
  action,
  fields = {},
  children,
  pendingLabel = "Working…",
  variant,
  size,
  className,
}: ActionButtonProps) {
  const [state, formAction, pending] = useActionState(action, {});

  return (
    <div className={className}>
      <form action={formAction}>
        {Object.entries(fields).map(([name, value]) => (
          <input key={name} type="hidden" name={name} value={value} />
        ))}
        <Button type="submit" variant={variant} size={size} disabled={pending}>
          {pending ? pendingLabel : children}
        </Button>
      </form>
      {state.error ? <p className="mt-1 text-xs text-destructive">{state.error}</p> : null}
    </div>
  );
}
