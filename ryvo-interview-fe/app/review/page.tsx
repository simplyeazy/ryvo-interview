import { Suspense } from "react";
import Link from "next/link";
import { redirect } from "next/navigation";
import { ChevronLeft, ChevronRight } from "lucide-react";
import type { Metadata } from "next";

import { approveWeek } from "@/app/actions";
import { ActionButton } from "@/components/action-button";
import { EmptyState, Forbidden } from "@/components/empty-state";
import { StatusBadge } from "@/components/status-badge";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { apiFetch } from "@/lib/api";
import {
  addDays,
  formatDate,
  formatHours,
  formatMoney,
  formatTime,
  formatWeekdayShort,
  localDateOf,
  mondayOf,
  todayInZone,
} from "@/lib/dates";
import { getSessionUser } from "@/lib/session";
import type { Engagement, Week } from "@/lib/types";
import { RejectForm } from "./reject-form";

export const metadata: Metadata = { title: "Review" };

type SearchParams = Promise<Record<string, string | string[] | undefined>>;

export default function ReviewPage({ searchParams }: { searchParams: SearchParams }) {
  return (
    <div className="mx-auto w-full max-w-6xl px-4 py-8">
      <Suspense fallback={<ReviewSkeleton />}>
        <ReviewView searchParams={searchParams} />
      </Suspense>
    </div>
  );
}

async function ReviewView({ searchParams }: { searchParams: SearchParams }) {
  const params = await searchParams;
  const user = await getSessionUser();

  if (!user) redirect("/switch");
  if (user.role !== "Client") {
    return <Forbidden message="Only clients review timesheets. Switch to Acme or Globex." />;
  }

  const thisWeek = mondayOf(todayInZone(user.timeZoneId));
  const weekOf = single(params.week) ?? thisWeek;
  const [weeks, engagements] = await Promise.all([
    apiFetch<Week[]>(`/api/clients/weeks?weekOf=${weekOf}`, { userId: user.id }),
    apiFetch<Engagement[]>("/api/me/engagements", { userId: user.id }),
  ]);

  const engagementById = new Map(engagements.map((item) => [item.id, item]));

  return (
    <div className="flex flex-col gap-6">
      <header className="flex flex-col gap-1">
        <h1 className="text-2xl font-semibold tracking-tight">Review timesheets</h1>
        <p className="text-sm text-muted-foreground">
          {formatDate(weekOf)} week · {weeks.length} contractor
          {weeks.length === 1 ? "" : "s"} with a timesheet
        </p>
      </header>

      <div className="flex flex-wrap items-center gap-2">
        <Button asChild variant="outline" size="icon-sm" aria-label="Previous week">
          <Link href={`/review?week=${addDays(weekOf, -7)}`} prefetch={false}>
            <ChevronLeft />
          </Link>
        </Button>
        <Button asChild variant="outline" size="icon-sm" aria-label="Next week">
          <Link href={`/review?week=${addDays(weekOf, 7)}`} prefetch={false}>
            <ChevronRight />
          </Link>
        </Button>
        {weekOf !== thisWeek ? (
          <Button asChild variant="ghost" size="sm">
            <Link href={`/review?week=${thisWeek}`} prefetch={false}>
              This week
            </Link>
          </Button>
        ) : null}
      </div>

      {weeks.length === 0 ? (
        <EmptyState
          title="Nothing to review this week"
          description="No contractor has created a timesheet for this week yet."
        />
      ) : (
        <div className="grid gap-4">
          {weeks.map((week) => {
            const engagement = engagementById.get(week.engagementId);
            const zone = week.contractorTimeZoneId;
            const submitted = week.status === "Submitted";

            return (
              <Card key={week.id}>
                <CardHeader>
                  <div className="flex flex-wrap items-center justify-between gap-2">
                    <CardTitle className="text-base">
                      {engagement?.contractorName ?? "Contractor"}
                    </CardTitle>
                    <div className="flex items-center gap-2">
                      <StatusBadge status={week.status} />
                      {engagement ? (
                        <Badge variant="outline" className="font-mono text-xs">
                          {formatMoney(engagement.hourlyRateUsd)}/h
                        </Badge>
                      ) : null}
                    </div>
                  </div>
                  <CardDescription>
                    {week.entries.length} entr{week.entries.length === 1 ? "y" : "ies"} ·{" "}
                    {formatHours(week.totalHours)} h · {zone}
                  </CardDescription>
                </CardHeader>

                <CardContent className="flex flex-col gap-3">
                  {week.status === "Rejected" && week.rejectionComment ? (
                    <p className="rounded-md border border-destructive/30 bg-destructive/5 px-3 py-2 text-sm">
                      Sent back: “{week.rejectionComment}”
                    </p>
                  ) : null}

                  {week.entries.length === 0 ? (
                    <p className="text-sm text-muted-foreground">No entries.</p>
                  ) : (
                    <ul className="divide-y text-sm">
                      {week.entries.map((entry) => {
                        const day = localDateOf(entry.startUtc, zone);
                        return (
                          <li
                            key={entry.id}
                            className="flex flex-wrap items-baseline justify-between gap-2 py-1.5"
                          >
                            <span className="tabular-nums text-muted-foreground">
                              {formatWeekdayShort(day)} {formatTime(entry.startUtc, zone)}–
                              {formatTime(entry.endUtc, zone)}
                            </span>
                            <span className="flex-1 truncate">
                              {entry.description || (
                                <span className="text-muted-foreground">—</span>
                              )}
                            </span>
                            <span className="tabular-nums">{formatHours(entry.hours)} h</span>
                          </li>
                        );
                      })}
                    </ul>
                  )}
                </CardContent>

                {submitted ? (
                  <CardFooter className="flex flex-col items-stretch gap-3 border-t pt-4 sm:flex-row sm:items-start">
                    <ActionButton
                      action={approveWeek}
                      fields={{ engagementId: week.engagementId, weekOf: week.weekStartLocal }}
                      pendingLabel="Approving…"
                    >
                      Approve week
                    </ActionButton>
                    <div className="flex-1">
                      <RejectForm
                        engagementId={week.engagementId}
                        weekOf={week.weekStartLocal}
                      />
                    </div>
                  </CardFooter>
                ) : (
                  <CardFooter className="border-t pt-4 text-xs text-muted-foreground">
                    {week.status === "Draft"
                      ? "Waiting for the contractor to submit."
                      : week.status === "Approved"
                        ? "Approved — included in invoices."
                        : "Sent back to the contractor."}
                  </CardFooter>
                )}
              </Card>
            );
          })}
        </div>
      )}
    </div>
  );
}

function ReviewSkeleton() {
  return (
    <div className="flex flex-col gap-6">
      <Skeleton className="h-8 w-64" />
      <Skeleton className="h-10 w-40" />
      <Skeleton className="h-56 w-full" />
      <Skeleton className="h-56 w-full" />
    </div>
  );
}

function single(value: string | string[] | undefined): string | undefined {
  return Array.isArray(value) ? value[0] : value;
}
