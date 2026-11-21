import { cache } from "react";
import { cookies } from "next/headers";

import { apiFetch } from "./api";
import { USER_COOKIE } from "./cookies";
import type { User } from "./types";

export { USER_COOKIE };

/**
 * The dev user selected in the UI. Memoised per request so a page, its layout
 * and several Server Components don't each re-fetch `/api/me`.
 */
export const getSessionUser = cache(async (): Promise<User | null> => {
  const store = await cookies();
  const userId = store.get(USER_COOKIE)?.value;
  if (!userId) return null;

  try {
    return await apiFetch<User>("/api/me", { userId });
  } catch {
    // Stale cookie (user deleted or API reset): treat as signed out.
    return null;
  }
});
