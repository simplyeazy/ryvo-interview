/**
 * Cookie names shared by the Server Actions (which write them), the proxy
 * (which reads them for optimistic redirects) and the session helper.
 */
export const USER_COOKIE = "ryvo_user";
export const ROLE_COOKIE = "ryvo_role";

export const COOKIE_MAX_AGE_SECONDS = 60 * 60 * 24 * 30;
