using Microsoft.EntityFrameworkCore;

namespace ryvo_interview_be.Data;

public static class DbInitializer
{
    /// <summary>
    /// Creates the SQLite database from migrations on first run. Seed rows for
    /// users, engagements and weeks are part of the initial migration (HasData).
    /// </summary>
    public static void Initialize(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RyvoDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<RyvoDbContext>>();

        var pending = db.Database.GetPendingMigrations().ToList();
        if (pending.Count > 0)
        {
            logger.LogInformation("Applying {Count} pending migration(s): {Migrations}",
                pending.Count, string.Join(", ", pending));
        }

        db.Database.Migrate();
        logger.LogInformation(
            "SQLite ready: {Users} users, {Engagements} engagements, {Weeks} weeks.",
            db.Users.Count(),
            db.Engagements.Count(),
            db.TimesheetWeeks.Count());
    }
}
