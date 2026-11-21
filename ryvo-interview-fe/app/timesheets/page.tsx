import { Suspense } from "react";
import Link from "next/link";
import { redirect } from "next/navigation";
import { CalendarDays, ChevronLeft, ChevronRight, Trash2 } from "lucide-react";
import type { Metadata } from "next";

import { deleteEntry, submitWeek } from "@/app/actions";
import { ActionButton } from "@/components/action-button";
import { AddEntryForm } from "./add-entry-form";
import { Forbidden } from "@/components/empty-state";
import { StatusBadge } from "@/components/status-badge";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { apiFetch } from "@/lib/api";
import {
  addDays,
  formatDate,
  formatDayMonth,
  formatHours,
  formatTime,
  formatWeekdayShort,
  localDateOf,
  mondayOf,
  todayIso,
} from "@/lib/dates";
import { getSessionUser } from "@/lib/session";
import { isEditable, type Engagement, type Week } from "@/lib/types";

export const metadata: Metadata = { title: "Timesheets" };

type SearchParams = Promise<Record<string, string | string[] | undefined>>;

export default function TimesheetsPage({
  searchParams,
}: {
  searchParams: SearchParams;
}) {
  return (
    <div className="mx-auto w-full max-w-6xl px-4 py-8">
      <Suspense fallback={<TimesheetSkeleton />}>
        <TimesheetsView searchParams={searchParams} />
      </Suspense>
    </div>
  );
}

async function TimesheetsView({ searchParams }: { searchParams: SearchParams }) {
  const params = await searchParams;
  const user = await getSessionUser();

  if (!user) redirect("/switch");
  if (user.role !== "Contractor") {
    return <Forbidden message="Only contractors fill in timesheets. Switch to Budi or Siti." />;
  }

  const engagements = await apiFetch<Engagement[]>("/api/me/engagements", {
    userId: user.id,
  });

  if (engagements.length === 0) {
    return (
      <Alert>
        <AlertTitle>No engagement yet</AlertTitle>
        <AlertDescription>You are not assigned to a client engagement.</AlertDescription>
      </Alert>
    );
  }

  const requestedEngagement = single(params.engagement);
  const engagement =
    engagements.find((item) => item.id === requestedEngagement) ?? engagements[0];

  const weekOf = single(params.week) ?? mondayOf(todayIso());
  const week = await apiFetch<Week>(
    `/api/engagements/${engagement.id}/weeks/${weekOf}`,
    { userId: user.id },
  );

  const zone = week.contractorTimeZoneId;
  const editable = isEditable(week.status);
  const thisWeek = mondayOf(todayIso());
  const query = (target: string) =>
    `/timesheets?engagement=${engagement.id}&week=${target}`;

  return (
    <div className="flex flex-col gap-6">
      <header className="flex flex-col gap-3">
        <div className="flex flex-wrap items-center gap-2">
          <h1 className="text-2xl font-semibold tracking-tight">Timesheet</h1>
          <StatusBadge status={week.status} />
          <Badge variant="outline" className="font-mono text-xs">
            {zone}
          </Badge>
        </div>
        <p className="text-sm text-muted-foreground">
          {engagement.clientName} · {formatDayMonth(week.weekStartLocal)} –{" "}
          {formatDayMonth(week.weekEndLocal)} · {formatHours(week.totalHours)} h
        </p>

        {engagements.length > 1 ? (
          <div className="flex flex-wrap gap-2">
            {engagements.map((item) => (
              <Button
                key={item.id}
                asChild
                size="sm"
                variant={item.id === engagement.id ? "default" : "outline"}
              >
                <Link href={`/timesheets?engagement=${item.id}&week=${weekOf}`}>
                  {item.clientName}
                </Link>
              </Button>
            ))}
          </div>
        ) : null}
      </header>

      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex items-center gap-2">
          <Button asChild variant="outline" size="icon-sm" aria-label="Previous week">
            <Link href={query(addDays(week.weekStartLocal, -7))} prefetch={false}>
              <ChevronLeft />
            </Link>
          </Button>
          <div className="flex items-center gap-2 text-sm font-medium">
            <CalendarDays className="size-4 text-muted-foreground" />
            {formatDate(week.weekStartLocal)} – {formatDate(week.weekEndLocal)}
          </div>
          <Button asChild variant="outline" size="icon-sm" aria-label="Next week">
            <Link href={query(addDays(week.weekStartLocal, 7))} prefetch={false}>
              <ChevronRight />
            </Link>
          </Button>
        </div>
        {week.weekStartLocal !== thisWeek ? (
          <Button asChild variant="ghost" size="sm">
            <Link href={query(thisWeek)} prefetch={false}>
              This week
            </Link>
          </Button>
        ) : null}
      </div>

      <StatusNotice week={week} clientName={engagement.clientName} />

      <section aria-label="Daily totals" className="grid grid-cols-4 gap-2 sm:grid-cols-7">
        {Array.from({ length: 7 }, (_, index) => {
          const day = addDays(week.weekStartLocal, index);
          const hours = week.dailyTotals[day] ?? 0;
          const atCap = hours >= 12;

          return (
            <div
              key={day}
              className="rounded-lg border px-2 py-2 text-center"
            >
              <p className="text-xs text-muted-foreground">{formatWeekdayShort(day)}</p>
              <p
                className={
                  atCap
                    ? "text-sm font-medium text-amber-600 tabular-nums dark:text-amber-400"
                    : "text-sm font-medium tabular-nums"
                }
              >
                {formatHours(hours)}
              </p>
            </div>
          );
        })}
      </section>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Entries</CardTitle>
          <CardDescription>
            Times shown in {zone}. A day may hold at most 12 hours.
          </CardDescription>
        </CardHeader>
        <CardContent>
          {week.entries.length === 0 ? (
            <p className="py-6 text-center text-sm text-muted-foreground">
              No entries yet for this week.
            </p>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Start</TableHead>
                  <TableHead>End</TableHead>
                  <TableHead className="text-right">Hours</TableHead>
                  <TableHead>Description</TableHead>
                  <TableHead className="w-10" />
                </TableRow>
              </TableHeader>
              <TableBody>
                {week.entries.map((entry) => (
                  <TableRow key={entry.id}>
                    <TableCell className="whitespace-nowrap tabular-nums">
                      {formatWeekdayShort(localDateOf(entry.startUtc, zone))}{" "}
                      {formatTime(entry.startUtc, zone)}
                    </TableCell>
                    <TableCell className="whitespace-nowrap tabular-nums">
                      {formatWeekdayShort(localDateOf(entry.endUtc, zone))}{" "}
                      {formatTime(entry.endUtc, zone)}
                    </TableCell>
                    <TableCell className="text-right tabular-nums">
                      {formatHours(entry.hours)}
                    </TableCell>
                    <TableCell className="max-w-64 truncate">
                      {entry.description || (
                        <span className="text-muted-foreground">—</span>
                      )}
                    </TableCell>
                    <TableCell className="text-right">
                      {editable ? (
                        <ActionButton
                          action={deleteEntry}
                          fields={{
                            engagementId: engagement.id,
                            weekOf: week.weekStartLocal,
                            entryId: entry.id,
                          }}
                          variant="ghost"
                          size="icon-sm"
                          pendingLabel="…"
                        >
                          <Trash2 />
                        </ActionButton>
                      ) : null}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      {editable ? (
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Add entry</CardTitle>
            <CardDescription>
              Enter wall-clock time in {zone}; it is stored as UTC.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <AddEntryForm
              engagementId={engagement.id}
              weekStart={week.weekStartLocal}
              weekEnd={week.weekEndLocal}
            />
          </CardContent>
        </Card>
      ) : null}

      {editable && week.entries.length > 0 ? (
        <div className="flex justify-end">
          <ActionButton
            action={submitWeek}
            fields={{ engagementId: engagement.id, weekOf: week.weekStartLocal }}
            pendingLabel="Submitting…"
          >
            Submit for approval
          </ActionButton>
        </div>
      ) : null}
    </div>
  );
}

function StatusNotice({ week, clientName }: { week: Week; clientName: string }) {
  if (week.status === "Rejected") {
    return (
      <Alert variant="destructive">
        <AlertTitle>Sent back by {clientName}</AlertTitle>
        <AlertDescription>
          {week.rejectionComment ?? "No comment provided."} Fix the week and submit it again.
        </AlertDescription>
      </Alert>
    );
  }

  if (week.status === "Submitted") {
    return (
      <Alert>
        <AlertTitle>Awaiting review</AlertTitle>
        <AlertDescription>
          {clientName} can approve or reject this week. Editing is locked until then.
        </AlertDescription>
      </Alert>
    );
  }

  if (week.status === "Approved") {
    return (
      <Alert>
        <AlertTitle>Approved</AlertTitle>
        <AlertDescription>
          These hours are billable and appear on {clientName}&apos;s invoices.
        </AlertDescription>
      </Alert>
    );
  }

  return null;
}

function TimesheetSkeleton() {
  return (
    <div className="flex flex-col gap-6">
      <Skeleton className="h-8 w-56" />
      <Skeleton className="h-16 w-full" />
      <Skeleton className="h-48 w-full" />
      <Skeleton className="h-40 w-full" />
    </div>
  );
}

function single(value: string | string[] | undefined): string | undefined {
  return Array.isArray(value) ? value[0] : value;
}
