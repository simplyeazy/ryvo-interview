using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ryvo_interview_be.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    TimeZoneId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Engagements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClientId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContractorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    HourlyRateUsd = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Engagements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Engagements_Users_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Engagements_Users_ContractorId",
                        column: x => x.ContractorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClientId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RangeStart = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    RangeEnd = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    GeneratedAtUtc = table.Column<long>(type: "INTEGER", nullable: false),
                    TotalApprovedHours = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    TotalAmountUsd = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invoices_Users_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TimesheetWeeks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EngagementId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContractorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClientId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContractorTimeZoneId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    WeekStartLocal = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    RejectionComment = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimesheetWeeks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TimesheetWeeks_Engagements_EngagementId",
                        column: x => x.EngagementId,
                        principalTable: "Engagements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContractorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContractorName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    WorkDateClientLocal = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ApprovedHours = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    HourlyRateUsd = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    LineTotalUsd = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceLines_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TimeEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    WeekId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StartUtc = table.Column<long>(type: "INTEGER", nullable: false),
                    EndUtc = table.Column<long>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimeEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TimeEntries_TimesheetWeeks_WeekId",
                        column: x => x.WeekId,
                        principalTable: "TimesheetWeeks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Name", "Role", "TimeZoneId" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), "Acme Corp", "Client", "America/New_York" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), "Budi Santoso", "Contractor", "Asia/Jakarta" },
                    { new Guid("33333333-3333-3333-3333-333333333333"), "Siti Rahma", "Contractor", "Asia/Jakarta" },
                    { new Guid("44444444-4444-4444-4444-444444444444"), "Globex Inc", "Client", "America/New_York" }
                });

            migrationBuilder.InsertData(
                table: "Engagements",
                columns: new[] { "Id", "ClientId", "ContractorId", "HourlyRateUsd" },
                values: new object[,]
                {
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), new Guid("11111111-1111-1111-1111-111111111111"), new Guid("22222222-2222-2222-2222-222222222222"), 75m },
                    { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), new Guid("11111111-1111-1111-1111-111111111111"), new Guid("33333333-3333-3333-3333-333333333333"), 60m },
                    { new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), new Guid("44444444-4444-4444-4444-444444444444"), new Guid("33333333-3333-3333-3333-333333333333"), 80m }
                });

            migrationBuilder.InsertData(
                table: "TimesheetWeeks",
                columns: new[] { "Id", "ClientId", "ContractorId", "ContractorTimeZoneId", "EngagementId", "RejectionComment", "Status", "WeekStartLocal" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111"), new Guid("22222222-2222-2222-2222-222222222222"), "Asia/Jakarta", new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), null, "Approved", new DateOnly(2026, 11, 16) },
                    { new Guid("10000000-0000-0000-0000-000000000002"), new Guid("11111111-1111-1111-1111-111111111111"), new Guid("33333333-3333-3333-3333-333333333333"), "Asia/Jakarta", new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), null, "Submitted", new DateOnly(2026, 11, 16) },
                    { new Guid("10000000-0000-0000-0000-000000000003"), new Guid("44444444-4444-4444-4444-444444444444"), new Guid("33333333-3333-3333-3333-333333333333"), "Asia/Jakarta", new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), null, "Draft", new DateOnly(2026, 11, 16) },
                    { new Guid("10000000-0000-0000-0000-000000000004"), new Guid("11111111-1111-1111-1111-111111111111"), new Guid("22222222-2222-2222-2222-222222222222"), "Asia/Jakarta", new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), null, "Draft", new DateOnly(2026, 11, 23) }
                });

            migrationBuilder.InsertData(
                table: "TimeEntries",
                columns: new[] { "Id", "Description", "EndUtc", "StartUtc", "WeekId" },
                values: new object[,]
                {
                    { new Guid("20000000-0000-0000-0000-000000000001"), "Design review", 639304344000000000L, 639304056000000000L, new Guid("10000000-0000-0000-0000-000000000001") },
                    { new Guid("20000000-0000-0000-0000-000000000002"), "API implementation", 639305208000000000L, 639304920000000000L, new Guid("10000000-0000-0000-0000-000000000001") },
                    { new Guid("20000000-0000-0000-0000-000000000003"), "Sprint planning", 639305928000000000L, 639305784000000000L, new Guid("10000000-0000-0000-0000-000000000001") },
                    { new Guid("20000000-0000-0000-0000-000000000004"), "QA pass", 639304380000000000L, 639304092000000000L, new Guid("10000000-0000-0000-0000-000000000002") },
                    { new Guid("20000000-0000-0000-0000-000000000005"), "Bug triage", 639305100000000000L, 639304956000000000L, new Guid("10000000-0000-0000-0000-000000000002") },
                    { new Guid("20000000-0000-0000-0000-000000000006"), "Globex onboarding", 639306864000000000L, 639306684000000000L, new Guid("10000000-0000-0000-0000-000000000003") },
                    { new Guid("20000000-0000-0000-0000-000000000007"), "Draft week work", 639310320000000000L, 639310104000000000L, new Guid("10000000-0000-0000-0000-000000000004") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Engagements_ClientId",
                table: "Engagements",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Engagements_ContractorId",
                table: "Engagements",
                column: "ContractorId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_InvoiceId",
                table: "InvoiceLines",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_ClientId_GeneratedAtUtc",
                table: "Invoices",
                columns: new[] { "ClientId", "GeneratedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TimeEntries_WeekId_StartUtc",
                table: "TimeEntries",
                columns: new[] { "WeekId", "StartUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetWeeks_ClientId_WeekStartLocal",
                table: "TimesheetWeeks",
                columns: new[] { "ClientId", "WeekStartLocal" });

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetWeeks_EngagementId_WeekStartLocal",
                table: "TimesheetWeeks",
                columns: new[] { "EngagementId", "WeekStartLocal" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvoiceLines");

            migrationBuilder.DropTable(
                name: "TimeEntries");

            migrationBuilder.DropTable(
                name: "Invoices");

            migrationBuilder.DropTable(
                name: "TimesheetWeeks");

            migrationBuilder.DropTable(
                name: "Engagements");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
