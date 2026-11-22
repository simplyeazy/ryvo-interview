# Ryvo Timesheets

Contractor timesheets and client invoices.

| Path | What it is |
|---|---|
| `ryvo-interview-be/` | ASP.NET Core minimal API, EF Core + SQLite |
| `ryvo-interview-be.Tests/` | xUnit: domain rules + persistence |
| `ryvo-interview-fe/` | Next.js frontend |

```bash
cd ryvo-interview-be
dotnet run          # http://localhost:5098/swagger
dotnet test ../ryvo-interview-be.Tests
```

The SQLite file (`ryvo.db`) is created from the initial migration on first run
and seeded with dev users, engagements and timesheet weeks. Delete it to reset.

# How to run the app

Takes about two minutes. The database creates and seeds itself on first start —
there is no SQLite install, no migration command and no `.env` file.

## 1. Install Git

```bash
sudo apt update && sudo apt install -y git
```

## 2. Clone the repository

```bash
git clone https://github.com/simplyeazy/ryvo-interview.git
cd ryvo-interview
```

Use `git@github.com:simplyeazy/ryvo-interview.git` only if you already have an
SSH key registered with GitHub; HTTPS works without one.

## 3. Install the .NET 10 SDK

Ubuntu 24.04 and newer ship .NET 10 in the default repositories:

```bash
sudo apt update && sudo apt install -y dotnet-sdk-10.0
dotnet --version        # expect 10.0.x
```

Snap alternative (needs the symlink and `DOTNET_ROOT`, or `dotnet` won't resolve):

```bash
sudo snap install dotnet-sdk-100
sudo ln -s /snap/dotnet-sdk-100/current/usr/bin/dotnet /usr/local/bin/dotnet
export DOTNET_ROOT=/snap/dotnet-sdk-100/current/usr/lib/dotnet
```

## 4. Install Node.js 20.9 or newer

Download the LTS installer from <https://nodejs.org/en/download>, or:

```bash
curl -fsSL https://deb.nodesource.com/setup_22.x | sudo -E bash -
sudo apt install -y nodejs
node --version          # v20.9+ or newer
```

## 5. Install the frontend dependencies

```bash
cd ryvo-interview-fe
npm ci
```

`node_modules` is not in the repository, so this step is required once after cloning.

## 6. Start the API

In one terminal:

```bash
cd ryvo-interview-be
dotnet run
```

The first run applies the migration, seeds the demo data and creates
`ryvo-interview-be/ryvo.db`. You should see:

```
Applying migration '20261121143737_InitialCreate'.
SQLite ready: 4 users, 3 engagements, 4 weeks.
Now listening on: http://localhost:5098
```

Swagger UI: <http://localhost:5098/swagger>

## 7. Start the web app

In a second terminal:

```bash
cd ryvo-interview-fe
npm run dev
```

Open <http://localhost:3000> and pick a user on the first screen.

## What to look at

| User | Role | Where |
|---|---|---|
| Budi Santoso | Contractor (Asia/Jakarta) | `/timesheets` — add/delete entries, 12 h daily cap, submit a week |
| Acme Corp | Client | `/review` — approve or reject with a comment; `/invoices` — generate and download CSV |
| Siti Rahma | Contractor | second engagement, shows per-contractor isolation |
| Globex Inc | Client | sees only its own contractor and invoices |

There is no password prompt: login is a dev-only user switcher, because the
exercise is about permission checks. Try adding an entry as Acme — it is rejected
server-side, not just hidden in the UI.

## Troubleshooting

- **Ports 5098 / 3000 must be free.** If the API prints `Address already in use`,
  stop whatever is on the port or run `dotnet run --urls http://localhost:5099`
  and start the web app with `RYVO_API_URL=http://localhost:5099 npm run dev`.
- **Reset the demo data:** stop the API and delete `ryvo-interview-be/ryvo.db*`; the
  next `dotnet run` recreates and reseeds it.
- **`dotnet: command not found` after the snap install:** the symlink and
  `DOTNET_ROOT` from step 3 are missing.
- **Windows:** run everything inside WSL2, not PowerShell.

# What am I building
## Must haves:
- daily time entries with validation: end after start, no overlapping entries and 12 hour daily cap.
- a weekly views with totals. Submitting a week lock its entries
- Clients can approve a submitted week by contractors or reject with a comment.
- An invoice for one client and date range: approved hours x hourly rate in USD downloadable as CSV
- Test for the money and time logic.
- Login can be dev only user switcher

Login can be a dev-only user switcher, since we're testing permission checks, not password storage. Contractors can't edit other people's entries, and clients can only approve their own contractors' weeks. Enforce both on the server.

# Timezones
- Store entries in UTC
- Show contractor's days in Asia/Jakarta
- Show client's days in their own time zone e.g. America/New_York.

# AI usage in this interview
- Minimal API and Swagger bootstrapping
- Edge cases in unit test
- Setting up EF Core and SQLite for persistence and making sure SQLite supports DateTimeOffset (It does not support it natively, we require workarounds)
- App structure and tidying by putting aggregates in aggregates, valueobjects in valueobjects etc
- Set git .gitignore, making sure no credentials, .envs and setting line endings CLRF / LF issue for linux and windows box
- Bootstrapping the FE using nextjs app router with shadcn

# Week boundary
- Contractor's timezone define the week boundary. `ContractorTimezoneId` captured on the week and used for boundary, daily cap and daily totals.

```csharp
ContractorTimeZoneId = contractor.TimeZoneId;
Boundary = new WeekBoundary(anyDateInWeek, contractor.GetTimeZone());
```

Why contractor, not the client

1. <b>Its the unit contractor submit</b>. They type wall clock times in their own day. If the week followed the client's zone, a Jakarta contractor's Monday moring would land in the client's sunday and the week they fill in would not match the days they actually worked in.
2. <b>The 12h/day cap is about the person's calender day.</b> It must reset at their midnight. Tying it to the client will break the cap boundary whenever say client timezone or daylight saving shifted while contractor day stays still.  
`TimeZoneInfo.FindSystemTimeZoneById(ContractorTimeZoneId)` in `ValidateEntryRules`
3. <b>Determinism after persistence.</b> The zone is stored on the week row, rebuilding the aggregate from SQLite recomputes the indentical boundary even if the server runs in another zone or the contractor later edit their profile. The unique key is `engagementId, Weekstartlocal` 

# Not done
- an admin role
- password reset or email flows
- Docker (no more SQLlite, just docker compose and it runs)
- live updates and charts
---

# Why I designed the project this way
## Why .NET Core ASP Web API instead of Nextjs with Server actions and Prism ORM

I'm more comfortable with C# instead of Typescript or Javascript. Typescript supports domain modelling too, I'm just not comfortable working with it in a time constrainded manner

## Why I designed the app structure this way

I tend to separate frontend project and backend project under a root directory instead of putting them all under one /src folder because this give me the easiest way to take a look at the front end. Why monorepo? simpler. Its a small app with me as developer. Setting up multiple repo just add another friction in development because we have to tend 2 repo for one project.

### Why Domain first

I develop domain first because this is where business rule exist. This is where I make sure each entities, each aggregates acts the way they should act and can test its behaviour. This way, AI agent can accelerate development by reading how the domain is and adjust generated code for the front end from the business rule.

## Why SQLite

SQLite does not require complicated setup like MSSQL, MariaDB, PostgreSQL thus eliminating friction for infra dependency. By using EF Core with `Microsoft.EntityFrameworkCore.SQlite` its already bundled. SQLite does come with quirks and we have to "trick" it because it does not support `DateTime`, `DateTimeOffset` it treats it either as `TEXT`, `INTEGER` or `REAL`.

## The weird trick: making EF Core "support" `DateTimeOffset` on SQLite

SQLite has no date/time type at all, and EF Core's SQLite provider will not
translate a `DateTimeOffset` into SQL for ordering, comparison or aggregation.
It will throws when the query is compiled:

```
System.NotSupportedException: SQLite does not support expressions of type
'DateTimeOffset' in ORDER BY clauses. Convert the values to a supported type,
or use LINQ to Objects to order the results on the client side.
```

So this innocent line, which is fine on SQL Server or PostgreSQL, blows up:

```csharp
.OrderByDescending(i => i.GeneratedAtUtc)   // NotSupportedException on SQLite
```

The provider stores `DateTimeOffset` as TEXT, and because that TEXT form is not
guaranteed to sort chronologically it refuses to use it in `ORDER BY`. You can
work around it per query with `AsEnumerable()` and sort in memory, but then you
pull the whole table over and lose any index on the column.

### The trick

`DateTimeOffset` is only unsupported *as a stored type*. Store it as a `long`
of UTC ticks and EF has nothing left to complain about — integers sort,
compare and aggregate natively in SQL. The mapping is a value converter, so the
domain and entity models still use `DateTimeOffset` and never see a `long`.

`Data/DateTimeOffsetToUtcTicksConverter.cs`:

```csharp
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public sealed class DateTimeOffsetToUtcTicksConverter : ValueConverter<DateTimeOffset, long>
{
    public DateTimeOffsetToUtcTicksConverter()
        : base(
            value => value.UtcTicks,
            ticks => new DateTimeOffset(ticks, TimeSpan.Zero))
    {
    }
}
```

Registering it once for **every** `DateTimeOffset` property, rather than
annotating each column, is what makes it feel like native support:

`Data/RyvoDbContext.cs`:

```csharp
protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
{
    // SQLite cannot translate DateTimeOffset ordering; ticks are exact and sortable.
    configurationBuilder
        .Properties<DateTimeOffset>()
        .HaveConversion<DateTimeOffsetToUtcTicksConverter>();
}
```

That is the whole trick. There is no provider patch, no reflection into EF's
query pipeline, no custom `IMethodCallTranslator`. We just changed what EF sees:
it stores and sorts a `long`, and the conversion happens on the way in and out.

### What EF actually emits now

The column is an `INTEGER`:

```sql
CREATE TABLE "Invoices" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Invoices" PRIMARY KEY,
    "ClientId" TEXT NOT NULL,
    "RangeStart" TEXT NOT NULL,
    "RangeEnd" TEXT NOT NULL,
    "GeneratedAtUtc" INTEGER NOT NULL,
    "TotalApprovedHours" TEXT NOT NULL,
    "TotalAmountUsd" TEXT NOT NULL,
    CONSTRAINT "FK_Invoices_Users_ClientId" FOREIGN KEY ("ClientId")
        REFERENCES "Users" ("Id") ON DELETE RESTRICT
)
```

and the query that used to throw is now plain, indexable SQL:

```sql
SELECT "i"."Id", "i"."ClientId", "i"."GeneratedAtUtc", ...
FROM "Invoices" AS "i"
LEFT JOIN "InvoiceLines" AS "i0" ON "i"."Id" = "i0"."InvoiceId"
WHERE "i"."ClientId" = @clientId
ORDER BY "i"."GeneratedAtUtc" DESC, "i"."Id"
```

There is a matching index on `(ClientId, GeneratedAtUtc)`, and because the
column is an integer the index is actually usable.

### The cost: the database is lying to you

This is the part to remember when someone opens the file with `sqlite3`:

```
sqlite> SELECT Description, StartUtc, EndUtc FROM TimeEntries LIMIT 2;
Design review|639304056000000000|639304344000000000
API implementation|639304920000000000|639305208000000000
```

Those are .NET ticks since `0001-01-01`, not Unix epoch and not ISO timestamps.
Anyone reading the table outside EF must apply the same conversion. The epoch
offset between the two is constant, so plain SQLite can still decode it:

```sql
-- ticks -> ISO 8601 (621355968000000000 = 1970-01-01T00:00:00Z in .NET ticks)
SELECT Description,
       strftime('%Y-%m-%dT%H:%M:%SZ',
                (StartUtc - 621355968000000000) / 10000000, 'unixepoch') AS StartsAt
FROM TimeEntries;

-- 2026-11-16T06:00:00Z
```

That divisor is 10,000,000 because a tick is 100 ns. It is read-only surgery for
debugging — writes should keep going through EF so the converter stays the only
thing that knows the format.

### Why .NET ticks and not Unix time?

Worth being explicit, because this is the obvious objection: yes, this is a
.NET-specific encoding. `DateTime` and `DateTimeOffset` count 100-nanosecond
intervals from `0001-01-01T00:00:00` in the proleptic Gregorian calendar, so the
epoch is year 1, not 1970. SQLite does not require that — it requires *some*
integer. Ticks is simply the integer that is already exact.

Unix time would work, and is far more portable:

```csharp
: base(
    value => value.ToUnixTimeSeconds(),
    seconds => DateTimeOffset.FromUnixTimeSeconds(seconds))
```

…but it truncates. Seconds discard up to 999,999,999 ns per value, and even
`ToUnixTimeMilliseconds()` discards everything below a millisecond. Round-trips
stop being identity, which is visible in this repo's own test:

```csharp
Assert.Equal(period, entry.Period);   // exact only while the encoding is lossless
```

| Encoding | Epoch | Precision | Readable outside .NET |
|---|---|---|---|
| `.UtcTicks` | 0001-01-01 | 100 ns, lossless | only by applying the same offset |
| `ToUnixTimeMilliseconds()` | 1970-01-01 | 1 ms, lossy | anything that speaks Unix time |
| `ToUnixTimeSeconds()` | 1970-01-01 | 1 s, lossy | anything, incl. `datetime(n, 'unixepoch')` in SQLite |

Ticks also stay inside `long` for the entire `DateTime` range:
`DateTime.MaxValue.Ticks` ≈ 3.16e18 against `long.MaxValue` ≈ 9.22e18, so there
is no overflow to guard against.

For a timesheet API whose entries are minute-granular, Unix seconds would have
been defensible and much friendlier to external tooling. Ticks was chosen for
exactness because the domain compares periods for equality and the tests rely on
it. If interoperability matters more than precision, swap the converter — just
do it before there is data to migrate.

Other trade-offs:

- **The offset is discarded.** Only the UTC instant round-trips; values come
  back as `+00:00`. That is deliberate here — `TimeRange` rejects any timestamp
  that is not already UTC, and `InvoiceGenerator` converts to a client-local
  *date* before persisting, so there is no offset worth keeping. If a future
  feature needs the original offset, add a separate column; don't try to pack it
  into the ticks.
- **`UtcTicks` is .NET 8+.** Fine on `net10.0`. The older equivalent is
  `value.UtcDateTime.Ticks`.
- **It is a storage-format decision, not a switch.** Changing it later means a
  data migration that rewrites every such column, so decide once per project.

### Where it lives

| File | Role |
|---|---|
| `Data/DateTimeOffsetToUtcTicksConverter.cs` | The converter |
| `Data/RyvoDbContext.cs` | `ConfigureConventions` applies it to all `DateTimeOffset` properties |
| `Data/Migrations/20261121143737_InitialCreate.cs` | Columns created as `INTEGER`; seed rows use tick literals |

---

## Smaller SQLite workarounds in this codebase

Not as sneaky, but they come up when porting from a "real" database:

- **`DateOnly` is fine.** EF stores it as `TEXT` in `yyyy-MM-dd`, which sorts
  and compares lexicographically, so `WHERE "WeekStartLocal" = @weekStart` works
  with no converter.
- **`decimal` is stored as `TEXT`.** Exact (good for money), but don't `ORDER BY`
  or `SUM` it in SQL; aggregate in memory, as the invoice generator does.
- **Enums are stored as strings** (`.HasConversion<string>()`) so the tables stay
  readable and reordering an enum can't silently change meaning.
- **Entities with a preset `Guid` key must be added explicitly.** Building a
  domain object that already has an id and hanging it off a tracked collection
  makes EF's change detection treat it as an existing row, so `SaveChanges`
  issues an `UPDATE` that matches nothing and throws
  `DbUpdateConcurrencyException: expected to affect 1 row(s), but actually
  affected 0`. `TimesheetStore.ApplyChanges` therefore calls
  `db.TimeEntries.Add(...)` for new rows and `Remove(...)` for deleted ones
  instead of relying on navigation fixup.
