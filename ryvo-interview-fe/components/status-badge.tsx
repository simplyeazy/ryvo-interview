import { Badge } from "@/components/ui/badge";
import type { TimesheetStatus } from "@/lib/types";

const TONE: Record<TimesheetStatus, string> = {
  Draft: "border-transparent bg-muted text-muted-foreground",
  Submitted: "border-transparent bg-blue-500/10 text-blue-700 dark:text-blue-300",
  Approved: "border-transparent bg-emerald-500/10 text-emerald-700 dark:text-emerald-300",
  Rejected: "border-transparent bg-destructive/10 text-destructive",
};

export function StatusBadge({ status }: { status: TimesheetStatus }) {
  return (
    <Badge variant="outline" className={TONE[status]}>
      {status}
    </Badge>
  );
}
