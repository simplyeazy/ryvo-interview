using System.Globalization;
using ryvo_interview_be.Data;
using ryvo_interview_be.Domain;
using ryvo_interview_be.Domain.Exceptions;

namespace ryvo_interview_be.Api;

public static class Endpoints
{
    public static void MapRyvoApi(this WebApplication app)
    {
        MapDev(app);
        MapTimesheets(app);
        MapInvoices(app);
        MapExports(app);
    }

    private static void MapDev(WebApplication app)
    {
        var dev = app.MapGroup("/api/dev").WithTags("Dev");

        dev.MapGet("/users", async (TimesheetStore store, CancellationToken ct) =>
            {
                var users = await store.ListUsersAsync(ct);
                return users.Select(u => u.ToResponse());
            })
            .WithName("ListDevUsers")
            .Produces<IEnumerable<UserResponse>>();

        dev.MapGet("/engagements", async (TimesheetStore store, CancellationToken ct) =>
            {
                var names = await GetUserNamesAsync(store, ct);
                var engagements = await store.ListEngagementsAsync(ct);
                return engagements.Select(e => e.ToResponse(names));
            })
            .WithName("ListEngagements")
            .Produces<IEnumerable<EngagementResponse>>();

        dev.MapPost("/login/{userId:guid}", async (
                Guid userId,
                HttpContext ctx,
                TimesheetStore store,
                CancellationToken ct) =>
            {
                var user = await store.RequireUserAsync(userId, ct);
                ctx.Response.Cookies.Append("dev_user", user.Id.ToString(), new CookieOptions
                {
                    HttpOnly = true,
                    SameSite = SameSiteMode.Lax,
                    Secure = false
                });

                return Results.Ok(user.ToResponse());
            })
            .WithName("DevLogin")
            .Produces<UserResponse>();

        dev.MapPost("/logout", (HttpContext ctx) =>
            {
                ctx.Response.Cookies.Delete("dev_user");
                return Results.NoContent();
            })
            .WithName("DevLogout")
            .Produces(StatusCodes.Status204NoContent);

        app.MapGet("/api/me", async (HttpContext ctx, TimesheetStore store, CancellationToken ct) =>
                (await store.RequireActorAsync(ctx, ct)).ToResponse())
            .WithTags("Dev")
            .WithName("GetCurrentUser")
            .Produces<UserResponse>();

        app.MapGet("/api/me/engagements", async (
                HttpContext ctx,
                TimesheetStore store,
                CancellationToken ct) =>
            {
                var actor = await store.RequireActorAsync(ctx, ct);
                var names = await GetUserNamesAsync(store, ct);
                var engagements = await store.ListEngagementsAsync(ct);

                return engagements
                    .Where(e => e.ClientId == actor.Id || e.ContractorId == actor.Id)
                    .Select(e => e.ToResponse(names));
            })
            .WithTags("Dev")
            .WithName("ListMyEngagements")
            .Produces<IEnumerable<EngagementResponse>>();
    }

    private static void MapTimesheets(WebApplication app)
    {
        var weeks = app.MapGroup("/api/engagements/{engagementId:guid}/weeks/{weekOf}")
            .WithTags("Timesheets");

        weeks.MapGet("", async (
                Guid engagementId,
                DateOnly weekOf,
                HttpContext ctx,
                TimesheetStore store,
                CancellationToken ct) =>
            {
                var actor = await store.RequireActorAsync(ctx, ct);
                var engagement = await store.RequireEngagementAsync(engagementId, ct);
                TimesheetStore.EnsureCanAccess(actor, engagement);

                // Contractors get a week created on demand; clients only read existing weeks.
                var createIfMissing = actor.Role == UserRole.Contractor;
                var week = await store.GetWeekAsync(engagementId, weekOf, createIfMissing, ct);
                return week.ToResponse();
            })
            .WithName("GetWeek")
            .Produces<WeekResponse>();

        weeks.MapPost("/entries", async (
                Guid engagementId,
                DateOnly weekOf,
                UpsertEntryRequest body,
                HttpContext ctx,
                TimesheetStore store,
                CancellationToken ct) =>
            {
                var actor = await store.RequireActorAsync(ctx, ct);
                var week = await store.MutateWeekAsync(engagementId, weekOf, true, w =>
                {
                    w.AddEntry(actor.Id, ToTimeRange(body), body.Description);
                    return w.ToResponse();
                }, ct);

                var location = $"/api/engagements/{engagementId}/weeks/" +
                               weekOf.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

                return Results.Created(location, week);
            })
            .WithName("AddEntry")
            .Produces<WeekResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        weeks.MapPut("/entries/{entryId:guid}", async (
                Guid engagementId,
                DateOnly weekOf,
                Guid entryId,
                UpsertEntryRequest body,
                HttpContext ctx,
                TimesheetStore store,
                CancellationToken ct) =>
            {
                var actor = await store.RequireActorAsync(ctx, ct);
                return await store.MutateWeekAsync(engagementId, weekOf, true, w =>
                {
                    w.UpdateEntry(actor.Id, entryId, ToTimeRange(body), body.Description);
                    return w.ToResponse();
                }, ct);
            })
            .WithName("UpdateEntry")
            .Produces<WeekResponse>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        weeks.MapDelete("/entries/{entryId:guid}", async (
                Guid engagementId,
                DateOnly weekOf,
                Guid entryId,
                HttpContext ctx,
                TimesheetStore store,
                CancellationToken ct) =>
            {
                var actor = await store.RequireActorAsync(ctx, ct);
                return await store.MutateWeekAsync(engagementId, weekOf, true, w =>
                {
                    w.RemoveEntry(actor.Id, entryId);
                    return w.ToResponse();
                }, ct);
            })
            .WithName("RemoveEntry")
            .Produces<WeekResponse>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);

        weeks.MapPost("/submit", async (
                Guid engagementId,
                DateOnly weekOf,
                HttpContext ctx,
                TimesheetStore store,
                CancellationToken ct) =>
            {
                var actor = await store.RequireActorAsync(ctx, ct);
                return await store.MutateWeekAsync(engagementId, weekOf, true, w =>
                {
                    w.Submit(actor.Id);
                    return w.ToResponse();
                }, ct);
            })
            .WithName("SubmitWeek")
            .Produces<WeekResponse>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);

        weeks.MapPost("/approve", async (
                Guid engagementId,
                DateOnly weekOf,
                HttpContext ctx,
                TimesheetStore store,
                CancellationToken ct) =>
            {
                var actor = await store.RequireActorAsync(ctx, ct);
                return await store.MutateWeekAsync(engagementId, weekOf, false, w =>
                {
                    w.Approve(actor.Id);
                    return w.ToResponse();
                }, ct);
            })
            .WithName("ApproveWeek")
            .Produces<WeekResponse>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);

        weeks.MapPost("/reject", async (
                Guid engagementId,
                DateOnly weekOf,
                RejectRequest body,
                HttpContext ctx,
                TimesheetStore store,
                CancellationToken ct) =>
            {
                var actor = await store.RequireActorAsync(ctx, ct);
                return await store.MutateWeekAsync(engagementId, weekOf, false, w =>
                {
                    w.Reject(actor.Id, body.Comment);
                    return w.ToResponse();
                }, ct);
            })
            .WithName("RejectWeek")
            .Produces<WeekResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static void MapInvoices(WebApplication app)
    {
        var clients = app.MapGroup("/api/clients").WithTags("Invoices");

        clients.MapGet("/weeks", async (
                DateOnly weekOf,
                HttpContext ctx,
                TimesheetStore store,
                CancellationToken ct) =>
            {
                var actor = await store.RequireActorAsync(ctx, ct);
                if (actor.Role != UserRole.Client)
                    throw new UnauthorizedDomainException("Only clients can review weeks.");

                var weeks = await store.WeeksForClientAsync(actor.Id, weekOf, ct);
                return weeks.OrderBy(w => w.ContractorId).Select(w => w.ToResponse());
            })
            .WithName("ListClientWeeks")
            .Produces<IEnumerable<WeekResponse>>();

        clients.MapPost("/invoices", async (
                GenerateInvoiceRequest body,
                HttpContext ctx,
                TimesheetStore store,
                CancellationToken ct) =>
            {
                var actor = await store.RequireActorAsync(ctx, ct);
                var invoice = await store.GenerateInvoiceAsync(
                    actor, body.RangeStart, body.RangeEnd, ct);

                return Results.Created(
                    $"/api/clients/invoices/{invoice.Id}", invoice.ToResponse());
            })
            .WithName("GenerateInvoice")
            .Produces<InvoiceResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        clients.MapGet("/invoices", async (
                HttpContext ctx,
                TimesheetStore store,
                CancellationToken ct) =>
            {
                var actor = await store.RequireActorAsync(ctx, ct);
                var invoices = await store.InvoicesForClientAsync(actor.Id, ct);
                return invoices.Select(i => i.ToResponse());
            })
            .WithName("ListInvoices")
            .Produces<IEnumerable<InvoiceResponse>>();

        clients.MapGet("/invoices/{invoiceId:guid}", async (
                Guid invoiceId,
                HttpContext ctx,
                TimesheetStore store,
                CancellationToken ct) =>
            {
                var actor = await store.RequireActorAsync(ctx, ct);

                // Scoped to the caller: another client's invoice is a 404, not a 403.
                var invoice = await store.RequireInvoiceAsync(invoiceId, actor.Id, ct);
                return invoice.ToResponse();
            })
            .WithName("GetInvoice")
            .Produces<InvoiceResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static void MapExports(WebApplication app)
    {
        var exports = app.MapGroup("/api").WithTags("CSV");

        exports.MapGet("/engagements/{engagementId:guid}/weeks/{weekOf}/entries/csv", async (
                Guid engagementId,
                DateOnly weekOf,
                HttpContext ctx,
                TimesheetStore store,
                CancellationToken ct) =>
            {
                var actor = await store.RequireActorAsync(ctx, ct);
                var engagement = await store.RequireEngagementAsync(engagementId, ct);
                TimesheetStore.EnsureCanAccess(actor, engagement);

                var week = await store.GetWeekAsync(engagementId, weekOf, createIfMissing: false, ct);
                var contractor = await store.RequireUserAsync(week.ContractorId, ct);

                return Results.File(
                    CsvExports.Week(week),
                    CsvExports.ContentType,
                    CsvExports.WeekFileName(week, contractor.Name));
            })
            .WithName("ExportWeekCsv")
            .WithSummary("Download one timesheet week as CSV")
            .WithDescription(
                "One row per entry, chronological, with start and end in the contractor's " +
                "time zone (offset included). RFC 4180; numbers use '.' as the decimal separator.")
            .Produces<string>(StatusCodes.Status200OK, CsvExports.ContentType)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        exports.MapGet("/clients/invoices/{invoiceId:guid}/csv", async (
                Guid invoiceId,
                HttpContext ctx,
                TimesheetStore store,
                CancellationToken ct) =>
            {
                var actor = await store.RequireActorAsync(ctx, ct);

                // Scoped to the caller: another client's invoice is a 404, not a 403.
                var invoice = await store.RequireInvoiceAsync(invoiceId, actor.Id, ct);

                return Results.File(
                    CsvExports.Invoice(invoice),
                    CsvExports.ContentType,
                    CsvExports.InvoiceFileName(invoice, actor.Name));
            })
            .WithName("ExportInvoiceCsv")
            .WithSummary("Download one invoice as CSV")
            .WithDescription(
                "One row per line item, in client-local work dates, followed by a TOTAL row. " +
                "RFC 4180; numbers use '.' as the decimal separator.")
            .Produces<string>(StatusCodes.Status200OK, CsvExports.ContentType)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IReadOnlyDictionary<Guid, string>> GetUserNamesAsync(
        TimesheetStore store,
        CancellationToken ct)
    {
        var users = await store.ListUsersAsync(ct);
        return users.ToDictionary(u => u.Id, u => u.Name);
    }

    private static TimeRange ToTimeRange(UpsertEntryRequest body) =>
        new(body.StartUtc.ToUniversalTime(), body.EndUtc.ToUniversalTime());
}
