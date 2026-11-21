/**
 * The only module that knows how to talk to the Ryvo API. Everything else deals
 * in typed domain objects or `ApiError`.
 *
 * Calls are made server-side (Server Components and Server Actions), so the
 * browser never sees the API URL and no CORS setup is required.
 */

export const API_URL = process.env.RYVO_API_URL ?? "http://localhost:5098";

export class ApiError extends Error {
  constructor(
    readonly status: number,
    readonly title: string,
    readonly detail: string,
  ) {
    super(detail);
  }
}

interface RequestOptions {
  userId?: string;
  method?: "GET" | "POST" | "PUT" | "DELETE";
  body?: unknown;
}

export async function apiFetch<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const headers: Record<string, string> = { Accept: "application/json" };
  if (options.body !== undefined) headers["Content-Type"] = "application/json";
  if (options.userId) headers["X-User-Id"] = options.userId;

  const response = await fetch(`${API_URL}${path}`, {
    method: options.method ?? "GET",
    headers,
    body: options.body === undefined ? undefined : JSON.stringify(options.body),
  });

  if (!response.ok) {
    const problem = (await response.json().catch(() => null)) as
      | { title?: string; detail?: string }
      | null;

    throw new ApiError(
      response.status,
      problem?.title ?? response.statusText,
      problem?.detail ?? "The request failed.",
    );
  }

  if (response.status === 204) return undefined as T;
  return (await response.json()) as T;
}

/** Fetches a binary response (CSV export) and proxies it through. */
export async function apiFetchFile(
  path: string,
  userId: string,
): Promise<Response> {
  return fetch(`${API_URL}${path}`, { headers: { "X-User-Id": userId } });
}

export function errorMessage(error: unknown): string {
  if (error instanceof ApiError) return error.detail;
  if (error instanceof Error) return error.message;
  return "Something went wrong.";
}
