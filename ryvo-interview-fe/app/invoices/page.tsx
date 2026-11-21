import { Suspense } from "react";
import Link from "next/link";
import { redirect } from "next/navigation";
import { FileText } from "lucide-react";
import type { Metadata } from "next";

import { EmptyState, Forbidden } from "@/components/empty-state";
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
import { formatDate, formatHours, formatMoney } from "@/lib/dates";
import { getSessionUser } from "@/lib/session";
import type { Invoice } from "@/lib/types";
import { NewInvoiceForm } from "./new-invoice-form";

export const metadata: Metadata = { title: "Invoices" };

export default function InvoicesPage() {
  return (
    <div className="mx-auto w-full max-w-6xl px-4 py-8">
      <Suspense fallback={<InvoicesSkeleton />}>
        <InvoicesView />
      </Suspense>
    </div>
  );
}

async function InvoicesView() {
  const user = await getSessionUser();

  if (!user) redirect("/switch");
  if (user.role !== "Client") {
    return <Forbidden message="Only clients generate invoices. Switch to Acme or Globex." />;
  }

  const invoices = await apiFetch<Invoice[]>("/api/clients/invoices", {
    userId: user.id,
  });

  return (
    <div className="flex flex-col gap-6">
      <header className="flex flex-col gap-1">
        <h1 className="text-2xl font-semibold tracking-tight">Invoices</h1>
        <p className="text-sm text-muted-foreground">
          Generated from approved weeks, billed per client-local work date.
        </p>
      </header>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Generate invoice</CardTitle>
          <CardDescription>
            Only approved weeks inside the range are billed, rounded once per day.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <NewInvoiceForm />
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">History</CardTitle>
          <CardDescription>
            {invoices.length} invoice{invoices.length === 1 ? "" : "s"}
          </CardDescription>
        </CardHeader>
        <CardContent>
          {invoices.length === 0 ? (
            <EmptyState
              title="No invoices yet"
              description="Pick a date range above to generate the first one."
            />
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Range</TableHead>
                  <TableHead>Generated</TableHead>
                  <TableHead className="text-right">Hours</TableHead>
                  <TableHead className="text-right">Amount</TableHead>
                  <TableHead className="w-20" />
                </TableRow>
              </TableHeader>
              <TableBody>
                {invoices.map((invoice) => (
                  <TableRow key={invoice.id}>
                    <TableCell className="whitespace-nowrap">
                      {formatDate(invoice.rangeStart)} – {formatDate(invoice.rangeEnd)}
                    </TableCell>
                    <TableCell className="whitespace-nowrap text-muted-foreground">
                      {formatDate(invoice.generatedAtUtc.slice(0, 10))}
                    </TableCell>
                    <TableCell className="text-right tabular-nums">
                      {formatHours(invoice.totalApprovedHours)}
                    </TableCell>
                    <TableCell className="text-right tabular-nums">
                      {formatMoney(invoice.totalAmountUsd)}
                    </TableCell>
                    <TableCell className="text-right">
                      <Button asChild variant="outline" size="sm">
                        <Link href={`/invoices/${invoice.id}`}>
                          <FileText />
                          View
                        </Link>
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>
    </div>
  );
}

function InvoicesSkeleton() {
  return (
    <div className="flex flex-col gap-6">
      <Skeleton className="h-8 w-40" />
      <Skeleton className="h-40 w-full" />
      <Skeleton className="h-56 w-full" />
    </div>
  );
}
