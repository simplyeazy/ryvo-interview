using Microsoft.EntityFrameworkCore;
using ryvo_interview_be.Data.Entities;
using ryvo_interview_be.Domain;

namespace ryvo_interview_be.Data;

/// <summary>
/// Fixed ids and fake data used to seed a fresh database. The ids are stable so
/// they can be pasted into Swagger / the .http file.
/// </summary>
public static class SeedData
{
    public static readonly Guid AcmeId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid GlobexId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    public static readonly Guid BudiId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid SitiId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public static readonly Guid AcmeBudiEngagementId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid AcmeSitiEngagementId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    public static readonly Guid GlobexSitiEngagementId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    public static readonly Guid BudiApprovedWeekId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    public static readonly Guid SitiSubmittedWeekId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    public static readonly Guid GlobexDraftWeekId = Guid.Parse("10000000-0000-0000-0000-000000000003");
    public static readonly Guid BudiDraftWeekId = Guid.Parse("10000000-0000-0000-0000-000000000004");

    public static void Apply(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserEntity>().HasData(
            new UserEntity
            {
                Id = AcmeId,
                Name = "Acme Corp",
                Role = UserRole.Client,
                TimeZoneId = "America/New_York"
            },
            new UserEntity
            {
                Id = GlobexId,
                Name = "Globex Inc",
                Role = UserRole.Client,
                TimeZoneId = "America/New_York"
            },
            new UserEntity
            {
                Id = BudiId,
                Name = "Budi Santoso",
                Role = UserRole.Contractor,
                TimeZoneId = "Asia/Jakarta"
            },
            new UserEntity
            {
                Id = SitiId,
                Name = "Siti Rahma",
                Role = UserRole.Contractor,
                TimeZoneId = "Asia/Jakarta"
            });

        modelBuilder.Entity<EngagementEntity>().HasData(
            new EngagementEntity
            {
                Id = AcmeBudiEngagementId,
                ClientId = AcmeId,
                ContractorId = BudiId,
                HourlyRateUsd = 75m
            },
            new EngagementEntity
            {
                Id = AcmeSitiEngagementId,
                ClientId = AcmeId,
                ContractorId = SitiId,
                HourlyRateUsd = 60m
            },
            new EngagementEntity
            {
                Id = GlobexSitiEngagementId,
                ClientId = GlobexId,
                ContractorId = SitiId,
                HourlyRateUsd = 80m
            });

        modelBuilder.Entity<TimesheetWeekEntity>().HasData(
            // Budi / Acme, 20h, approved -> billable.
            new TimesheetWeekEntity
            {
                Id = BudiApprovedWeekId,
                EngagementId = AcmeBudiEngagementId,
                ContractorId = BudiId,
                ClientId = AcmeId,
                ContractorTimeZoneId = "Asia/Jakarta",
                WeekStartLocal = new DateOnly(2026, 11, 16),
                Status = TimesheetStatus.Approved
            },
            // Siti / Acme, 12h, submitted -> awaiting review.
            new TimesheetWeekEntity
            {
                Id = SitiSubmittedWeekId,
                EngagementId = AcmeSitiEngagementId,
                ContractorId = SitiId,
                ClientId = AcmeId,
                ContractorTimeZoneId = "Asia/Jakarta",
                WeekStartLocal = new DateOnly(2026, 11, 16),
                Status = TimesheetStatus.Submitted
            },
            // Siti / Globex, draft -> proves client isolation.
            new TimesheetWeekEntity
            {
                Id = GlobexDraftWeekId,
                EngagementId = GlobexSitiEngagementId,
                ContractorId = SitiId,
                ClientId = GlobexId,
                ContractorTimeZoneId = "Asia/Jakarta",
                WeekStartLocal = new DateOnly(2026, 11, 16),
                Status = TimesheetStatus.Draft
            },
            // Budi / Acme, next week left in draft so the API can be exercised.
            new TimesheetWeekEntity
            {
                Id = BudiDraftWeekId,
                EngagementId = AcmeBudiEngagementId,
                ContractorId = BudiId,
                ClientId = AcmeId,
                ContractorTimeZoneId = "Asia/Jakarta",
                WeekStartLocal = new DateOnly(2026, 11, 23),
                Status = TimesheetStatus.Draft
            });

        modelBuilder.Entity<TimeEntryEntity>().HasData(
            Entry("20000000-0000-0000-0000-000000000001", BudiApprovedWeekId, 16, 6, 14, "Design review"),
            Entry("20000000-0000-0000-0000-000000000002", BudiApprovedWeekId, 17, 6, 14, "API implementation"),
            Entry("20000000-0000-0000-0000-000000000003", BudiApprovedWeekId, 18, 6, 10, "Sprint planning"),

            Entry("20000000-0000-0000-0000-000000000004", SitiSubmittedWeekId, 16, 7, 15, "QA pass"),
            Entry("20000000-0000-0000-0000-000000000005", SitiSubmittedWeekId, 17, 7, 11, "Bug triage"),

            Entry("20000000-0000-0000-0000-000000000006", GlobexDraftWeekId, 19, 7, 12, "Globex onboarding"),

            Entry("20000000-0000-0000-0000-000000000007", BudiDraftWeekId, 23, 6, 12, "Draft week work"));
    }

    private static TimeEntryEntity Entry(
        string id, Guid weekId, int day, int startHour, int endHour, string description) =>
        new()
        {
            Id = Guid.Parse(id),
            WeekId = weekId,
            StartUtc = Utc(2026, 11, day, startHour),
            EndUtc = Utc(2026, 11, day, endHour),
            Description = description
        };

    private static DateTimeOffset Utc(int year, int month, int day, int hour) =>
        new(year, month, day, hour, 0, 0, TimeSpan.Zero);
}
