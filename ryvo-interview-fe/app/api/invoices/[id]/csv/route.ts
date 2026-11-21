import { apiFetchFile } from "@/lib/api";
import { getSessionUser } from "@/lib/session";

/**
 * Proxies the API's CSV export. A plain `<a download>` cannot attach the
 * `X-User-Id` header, and this keeps the browser on our origin.
 */
export async function GET(
  _request: Request,
  context: RouteContext<"/api/invoices/[id]/csv">,
): Promise<Response> {
  const { id } = await context.params;

  const user = await getSessionUser();
  if (!user) {
    return new Response("Choose a user first.", { status: 401 });
  }

  const upstream = await apiFetchFile(`/api/clients/invoices/${id}/csv`, user.id);

  if (!upstream.ok) {
    return new Response(upstream.status === 404 ? "Invoice not found." : "Export failed.", {
      status: upstream.status,
    });
  }

  return new Response(upstream.body, {
    headers: {
      "Content-Type": upstream.headers.get("content-type") ?? "text/csv; charset=utf-8",
      "Content-Disposition":
        upstream.headers.get("content-disposition") ??
        `attachment; filename="invoice-${id}.csv"`,
      "Cache-Control": "private, no-store",
    },
  });
}
