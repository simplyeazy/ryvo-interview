import Link from "next/link";
import { notFound, redirect } from "next/navigation";
import { ArrowLeft, Download } from "lucide-react";
import type { Metadata } from "next";

import { StatusBadge } from "@/components/status-badge";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Separator } from "@/components/ui/separator";
import {
  Table,
  TableBody,
  TableCell,
  TableFooter,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { apiFetch, ApiError } from "@/lib/api";
import { formatDate, formatHours, formatMoney } from "@/lib/dates";
import { getSessionUser } from "@/lib/session";
import type { Invoice } from "@/lib/types";

export const metadata: Metadata = { title: "Invoice" };

/**
 * The detail page is meaningless without the invoice, so it blocks instead of
 * streaming a shell. That buys two things: a real 404 for a missing or foreign
 * invoice, and no layout shift on a page that is entirely data.
 */
export const instant = false;

type Params = Promise<{ id: string }>;

export default async function InvoicePage({ params }: { params: Params }) {
  const { id } = await params;
  const user = await getSessionUser();

  if (!user) redirect("/switch");

  let invoice: Invoice;
  try {
    invoice = await apiFetch<Invoice>(`/api/clients/invoices/${id}`, { userId: user.id });
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) notFound();
    throw error;
  }

  return (
    <div className="mx-auto w-full max-w-4xl px-4 py-8">
      <InvoiceContent invoice={invoice} />
    </div>
  );
}

function InvoiceContent({ invoice }: { invoice: Invoice }) {
  return (
    <div className="flex flex-col gap-6">
      <div>
        <Button asChild variant="ghost" size="sm" className="-ml-2 mb-2">
          <Link href="/invoices">
            <ArrowLeft />
            All invoices
          </Link>
        </Button>

        <div className="flex flex-wrap items-start justify-between gap-3">
          <div>
            <h1 className="text-2xl font-semibold tracking-tight">Invoice</h1>
            <p className="mt-1 font-mono text-xs text-muted-foreground">{invoice.id}</p>
          </div>
          <Button asChild variant="outline">
            <a href={`/api/invoices/${invoice.id}/csv`} download>
              <Download />
              Download CSV
            </a>
          </Button>
        </div>
      </div>

      <Card>
        <CardHeader>
          <div className="flex flex-wrap items-center justify-between gap-2">
            <CardTitle className="text-base">
              {formatDate(invoice.rangeStart)} – {formatDate(invoice.rangeEnd)}
            </CardTitle>
            <StatusBadge status="Approved" />
          </div>
          <CardDescription>
            Generated {formatDate(invoice.generatedAtUtc.slice(0, 10))} · client-local work dates
          </CardDescription>
        </CardHeader>

        <CardContent>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Contractor</TableHead>
                <TableHead>Work date</TableHead>
                <TableHead className="text-right">Hours</TableHead>
                <TableHead className="text-right">Rate</TableHead>
                <TableHead className="text-right">Line total</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {invoice.lineItems.map((line, index) => (
                <TableRow key={`${line.contractorId}-${line.workDateClientLocal}-${index}`}>
                  <TableCell>{line.contractorName}</TableCell>
                  <TableCell className="whitespace-nowrap">
                    {formatDate(line.workDateClientLocal)}
                  </TableCell>
                  <TableCell className="text-right tabular-nums">
                    {formatHours(line.approvedHours)}
                  </TableCell>
                  <TableCell className="text-right tabular-nums">
                    {formatMoney(line.hourlyRateUsd)}
                  </TableCell>
                  <TableCell className="text-right tabular-nums">
                    {formatMoney(line.lineTotalUsd)}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
            <TableFooter>
              <TableRow>
                <TableCell colSpan={2}>Total</TableCell>
                <TableCell className="text-right tabular-nums">
                  {formatHours(invoice.totalApprovedHours)}
                </TableCell>
                <TableCell />
                <TableCell className="text-right font-medium tabular-nums">
                  {formatMoney(invoice.totalAmountUsd)}
                </TableCell>
              </TableRow>
            </TableFooter>
          </Table>
        </CardContent>
      </Card>

      <Separator />

      <p className="text-xs text-muted-foreground">
        CSV export: {invoice.lineItems.length} line item
        {invoice.lineItems.length === 1 ? "" : "s"}, invariant decimals, RFC 4180.
      </p>
    </div>
  );
}

