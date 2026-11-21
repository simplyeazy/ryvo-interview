import { NextResponse, type NextRequest } from "next/server";

import { ROLE_COOKIE, USER_COOKIE } from "@/lib/cookies";

/**
 * Optimistic routing only: it decides *where* to send someone based on the
 * cookie, and never authorises data access. Every page and action still checks
 * the user against the API.
 *
 * Doing the redirects here rather than inside a Suspense boundary means they are
 * real HTTP redirects (no flash of the static shell while the cookie resolves).
 */
export function proxy(request: NextRequest) {
  const { pathname } = request.nextUrl;
  const userId = request.cookies.get(USER_COOKIE)?.value;
  const role = request.cookies.get(ROLE_COOKIE)?.value;

  if (!userId) {
    return pathname === "/switch"
      ? NextResponse.next()
      : NextResponse.redirect(new URL("/switch", request.url));
  }

  if (pathname === "/") {
    const home =
      role === "Client" ? "/review" : role === "Contractor" ? "/timesheets" : "/switch";
    return NextResponse.redirect(new URL(home, request.url));
  }

  if (role === "Contractor" && (pathname === "/review" || pathname.startsWith("/invoices"))) {
    return NextResponse.redirect(new URL("/timesheets", request.url));
  }

  if (role === "Client" && pathname === "/timesheets") {
    return NextResponse.redirect(new URL("/review", request.url));
  }

  return NextResponse.next();
}

export const config = {
  // Skip the API proxy (it authorises itself), Next internals and the icon.
  matcher: ["/((?!api|_next/static|_next/image|favicon.ico).*)"],
};
