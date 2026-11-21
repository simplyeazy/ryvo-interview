// Shapes returned by the Ryvo API. Kept structurally identical to the backend
// DTOs in ryvo-interview-be/Api/Dtos.cs.

export type UserRole = "Contractor" | "Client";

export type TimesheetStatus = "Draft" | "Submitted" | "Approved" | "Rejected";

export interface User {
  id: string;
  name: string;
  role: UserRole;
  timeZoneId: string;
}

export interface Engagement {
  id: string;
  clientId: string;
  clientName: string;
  contractorId: string;
  contractorName: string;
  hourlyRateUsd: number;
}

export interface Entry {
  id: string;
  startUtc: string;
  endUtc: string;
  hours: number;
  description: string;
}

export interface Week {
  id: string;
  engagementId: string;
  contractorId: string;
  clientId: string;
  contractorTimeZoneId: string;
  weekStartLocal: string;
  weekEndLocal: string;
  weekStartUtc: string;
  weekEndUtcExclusive: string;
  status: TimesheetStatus;
  rejectionComment: string | null;
  totalHours: number;
  dailyTotals: Record<string, number>;
  entries: Entry[];
}

export interface InvoiceLine {
  contractorId: string;
  contractorName: string;
  workDateClientLocal: string;
  approvedHours: number;
  hourlyRateUsd: number;
  lineTotalUsd: number;
}

export interface Invoice {
  id: string;
  clientId: string;
  rangeStart: string;
  rangeEnd: string;
  generatedAtUtc: string;
  totalApprovedHours: number;
  totalAmountUsd: number;
  lineItems: InvoiceLine[];
}

/** Return shape for every Server Action used with `useActionState`. */
export interface ActionState {
  error?: string;
}

/** A week can be edited only while it is a draft or was sent back. */
export function isEditable(status: TimesheetStatus): boolean {
  return status === "Draft" || status === "Rejected";
}
