"use server";

import { revalidatePath } from "next/cache";
import { cookies } from "next/headers";
import { redirect } from "next/navigation";

import { apiFetch, errorMessage } from "@/lib/api";
import {
  COOKIE_MAX_AGE_SECONDS,
  ROLE_COOKIE,
  USER_COOKIE,
} from "@/lib/cookies";
import { wallTimeToUtc } from "@/lib/dates";
import { getSessionUser } from "@/lib/session";
import type { ActionState, Invoice, User, Week } from "@/lib/types";

function text(formData: FormData, key: string): string {
  const value = formData.get(key);
  return typeof value === "string" ? value.trim() : "";
}

async function currentUserId(): Promise<string | null> {
  const user = await getSessionUser();
  return user?.id ?? null;
}

/**
 * Sets the selected dev user. The role is mirrored into a second cookie so the
 * proxy can route without calling the API.
 */
export async function switchUser(formData: FormData): Promise<void> {
  const userId = text(formData, "userId");
  const users = await apiFetch<User[]>("/api/dev/users");
  const user = users.find((candidate) => candidate.id === userId);

  if (!user) {
    throw new Error("Unknown user id.");
  }

  const store = await cookies();
  const options = {
    httpOnly: true,
    sameSite: "lax" as const,
    path: "/",
    maxAge: COOKIE_MAX_AGE_SECONDS,
  };

  store.set(USER_COOKIE, user.id, options);
  store.set(ROLE_COOKIE, user.role, options);

  redirect("/");
}

export async function addEntry(
  _previous: ActionState,
  formData: FormData,
): Promise<ActionState> {
  const userId = await currentUserId();
  if (!userId) return { error: "Choose a user first." };

  const engagementId = text(formData, "engagementId");
  const weekOf = text(formData, "weekOf");
  const startLocal = text(formData, "startLocal");
  const endLocal = text(formData, "endLocal");
  const description = text(formData, "description");

  if (!startLocal || !endLocal) return { error: "Start and end are required." };
  if (startLocal >= endLocal) return { error: "End must be after start." };

  try {
    // The week carries the contractor's zone; wall-clock input is converted to
    // UTC with that zone so the domain's boundary and cap rules see the same
    // instants a browser in Jakarta would.
    const week = await apiFetch<Week>(
      `/api/engagements/${engagementId}/weeks/${weekOf}`,
      { userId },
    );

    await apiFetch(`/api/engagements/${engagementId}/weeks/${weekOf}/entries`, {
      userId,
      method: "POST",
      body: {
        startUtc: wallTimeToUtc(startLocal, week.contractorTimeZoneId),
        endUtc: wallTimeToUtc(endLocal, week.contractorTimeZoneId),
        description,
      },
    });
  } catch (error) {
    return { error: errorMessage(error) };
  }

  revalidatePath("/timesheets");
  return {};
}

export async function deleteEntry(
  _previous: ActionState,
  formData: FormData,
): Promise<ActionState> {
  const userId = await currentUserId();
  if (!userId) return { error: "Choose a user first." };

  const engagementId = text(formData, "engagementId");
  const weekOf = text(formData, "weekOf");
  const entryId = text(formData, "entryId");

  try {
    await apiFetch(
      `/api/engagements/${engagementId}/weeks/${weekOf}/entries/${entryId}`,
      { userId, method: "DELETE" },
    );
  } catch (error) {
    return { error: errorMessage(error) };
  }

  revalidatePath("/timesheets");
  return {};
}

export async function submitWeek(
  _previous: ActionState,
  formData: FormData,
): Promise<ActionState> {
  const userId = await currentUserId();
  if (!userId) return { error: "Choose a user first." };

  try {
    await apiFetch(
      `/api/engagements/${text(formData, "engagementId")}/weeks/${text(formData, "weekOf")}/submit`,
      { userId, method: "POST" },
    );
  } catch (error) {
    return { error: errorMessage(error) };
  }

  revalidatePath("/timesheets");
  return {};
}

export async function approveWeek(
  _previous: ActionState,
  formData: FormData,
): Promise<ActionState> {
  const userId = await currentUserId();
  if (!userId) return { error: "Choose a user first." };

  try {
    await apiFetch(
      `/api/engagements/${text(formData, "engagementId")}/weeks/${text(formData, "weekOf")}/approve`,
      { userId, method: "POST" },
    );
  } catch (error) {
    return { error: errorMessage(error) };
  }

  revalidatePath("/review");
  return {};
}

export async function rejectWeek(
  _previous: ActionState,
  formData: FormData,
): Promise<ActionState> {
  const userId = await currentUserId();
  if (!userId) return { error: "Choose a user first." };

  const comment = text(formData, "comment");
  if (!comment) return { error: "A rejection comment is required." };

  try {
    await apiFetch(
      `/api/engagements/${text(formData, "engagementId")}/weeks/${text(formData, "weekOf")}/reject`,
      { userId, method: "POST", body: { comment } },
    );
  } catch (error) {
    return { error: errorMessage(error) };
  }

  revalidatePath("/review");
  return {};
}

export async function generateInvoice(
  _previous: ActionState,
  formData: FormData,
): Promise<ActionState> {
  const userId = await currentUserId();
  if (!userId) return { error: "Choose a user first." };

  const rangeStart = text(formData, "rangeStart");
  const rangeEnd = text(formData, "rangeEnd");

  if (!rangeStart || !rangeEnd) return { error: "Both dates are required." };
  if (rangeEnd < rangeStart) return { error: "End date cannot be before start date." };

  let invoice: Invoice;
  try {
    invoice = await apiFetch<Invoice>("/api/clients/invoices", {
      userId,
      method: "POST",
      body: { rangeStart, rangeEnd },
    });
  } catch (error) {
    return { error: errorMessage(error) };
  }

  revalidatePath("/invoices");
  redirect(`/invoices/${invoice.id}`);
}
