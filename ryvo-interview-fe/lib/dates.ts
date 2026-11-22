/**
 * Date and time-zone helpers built on `Intl`, so the UI needs no date library.
 *
 * The domain stores UTC; contractors think in wall-clock time in their own zone.
 * `wallTimeToUtc` is the bridge and mirrors the backend's own zone rules.
 */

const ISO_DATE = /^\d{4}-\d{2}-\d{2}$/;

/** Today as a plain UTC date (`yyyy-MM-dd`). */
export function todayIso(now: Date = new Date()): string {
  return now.toISOString().slice(0, 10);
}

/** Monday of the week containing `isoDate`. */
export function mondayOf(isoDate: string): string {
  assertIsoDate(isoDate);
  const date = new Date(`${isoDate}T00:00:00Z`);
  const daysSinceMonday = (date.getUTCDay() + 6) % 7;
  date.setUTCDate(date.getUTCDate() - daysSinceMonday);
  return date.toISOString().slice(0, 10);
}

export function addDays(isoDate: string, days: number): string {
  assertIsoDate(isoDate);
  const date = new Date(`${isoDate}T00:00:00Z`);
  date.setUTCDate(date.getUTCDate() + days);
  return date.toISOString().slice(0, 10);
}

/** "Mon 16 Nov 2026" — for a date with no time component. */
export function formatDate(isoDate: string): string {
  assertIsoDate(isoDate);
  return new Intl.DateTimeFormat("en-GB", {
    timeZone: "UTC",
    weekday: "short",
    day: "numeric",
    month: "short",
    year: "numeric",
  }).format(new Date(`${isoDate}T00:00:00Z`));
}

/** "16 Nov" — compact, for week ranges. */
export function formatDayMonth(isoDate: string): string {
  assertIsoDate(isoDate);
  return new Intl.DateTimeFormat("en-GB", {
    timeZone: "UTC",
    day: "numeric",
    month: "short",
  }).format(new Date(`${isoDate}T00:00:00Z`));
}

/** "Mon" — for the seven-day totals strip. */
export function formatWeekdayShort(isoDate: string): string {
  assertIsoDate(isoDate);
  return new Intl.DateTimeFormat("en-GB", {
    timeZone: "UTC",
    weekday: "short",
  }).format(new Date(`${isoDate}T00:00:00Z`));
}

/** UTC instant rendered as "08:00" in the given zone. */
export function formatTime(isoUtc: string, timeZone: string): string {
  return new Intl.DateTimeFormat("en-GB", {
    timeZone,
    hour: "2-digit",
    minute: "2-digit",
    hour12: false,
  }).format(new Date(isoUtc));
}

/** Calendar date of a UTC instant as seen in `timeZone` (`yyyy-MM-dd`). */
export function localDateOf(isoUtc: string, timeZone: string): string {
  return new Intl.DateTimeFormat("en-CA", {
    timeZone,
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
  }).format(new Date(isoUtc));
}

/**
 * Today as seen in `timeZone`. The server's own date is not good enough: at
 * 18:31 UTC it is still Sunday in UTC and already Monday in Jakarta.
 */
export function todayInZone(timeZone: string): string {
  return localDateOf(new Date().toISOString(), timeZone);
}

/** UTC instant rendered as "Mon 16 Nov, 08:00" in the given zone. */
export function formatDateTime(isoUtc: string, timeZone: string): string {
  return new Intl.DateTimeFormat("en-GB", {
    timeZone,
    weekday: "short",
    day: "numeric",
    month: "short",
    hour: "2-digit",
    minute: "2-digit",
    hour12: false,
  }).format(new Date(isoUtc));
}

/** The UTC offset of `date` in `timeZone`, in milliseconds east of UTC. */
function offsetMs(date: Date, timeZone: string): number {
  const parts = new Intl.DateTimeFormat("en-US", {
    timeZone,
    hour12: false,
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
  }).formatToParts(date);

  const part = (type: string) => Number(parts.find((p) => p.type === type)?.value);
  const asIfUtc = Date.UTC(
    part("year"),
    part("month") - 1,
    part("day"),
    part("hour") % 24,
    part("minute"),
    part("second"),
  );

  return asIfUtc - date.getTime() + date.getMilliseconds();
}

/**
 * Converts a `datetime-local` value ("2026-11-16T08:00") in `timeZone` to the
 * equivalent UTC instant. One correction pass handles DST boundaries, where the
 * offset at the guessed instant differs from the offset at the result.
 */
export function wallTimeToUtc(localDateTime: string, timeZone: string): string {
  const naive = new Date(`${localDateTime}:00Z`);
  if (Number.isNaN(naive.getTime())) throw new Error("Invalid date/time.");

  let offset = offsetMs(naive, timeZone);
  let result = new Date(naive.getTime() - offset);

  const corrected = offsetMs(result, timeZone);
  if (corrected !== offset) {
    offset = corrected;
    result = new Date(naive.getTime() - offset);
  }

  return result.toISOString();
}

export function formatHours(hours: number): string {
  return hours.toFixed(2);
}

export function formatMoney(amount: number): string {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD",
  }).format(amount);
}

/**
 * Short zone label for a controller, e.g. "WIB" or "EST", falling back to
 * "GMT+7" where the runtime has no abbreviation.
 */
export function formatZoneLabel(isoUtc: string, timeZone: string): string {
  const part = new Intl.DateTimeFormat("en-US", {
    timeZone,
    timeZoneName: "short",
  })
    .formatToParts(new Date(isoUtc))
    .find((p) => p.type === "timeZoneName");

  return part?.value ?? timeZone;
}

function assertIsoDate(value: string): void {
  if (!ISO_DATE.test(value)) {
    throw new Error(`Expected a yyyy-MM-dd date, received "${value}".`);
  }
}
