using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BookingManager.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    OldValue = table.Column<string>(type: "jsonb", nullable: true),
                    NewValue = table.Column<string>(type: "jsonb", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Resources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Capacity = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Resources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Bookings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookings", x => x.Id);
                    table.CheckConstraint("CK_Bookings_Status_Persisted", "\"Status\" IN ('Active', 'Cancelled')");
                    table.ForeignKey(
                        name: "FK_Bookings_Resources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "Resources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bookings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Resources",
                columns: new[] { "Id", "Capacity", "CreatedAt", "Description", "Name", "Status", "Type", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), 12, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Large conference room on the 1st floor with video conferencing.", "Conference Room A", "Available", "Room", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("22222222-2222-2222-2222-222222222222"), 6, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Small meeting room next to the kitchen.", "Conference Room B", "Available", "Room", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("33333333-3333-3333-3333-333333333333"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Portable 4K projector with HDMI and USB-C.", "Projector", "Available", "Equipment", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("44444444-4444-4444-4444-444444444444"), 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Soundproof pod for quick 1:1s and calls.", "Meeting Pod 1", "Available", "Room", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("55555555-5555-5555-5555-555555555555"), 20, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Shared lab space with oscilloscopes and soldering stations.", "Electronics Lab", "Available", "Lab", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("66666666-6666-6666-6666-666666666666"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "16-inch loaner laptop for travel and demos.", "MacBook Pro Loaner", "Available", "Equipment", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("77777777-7777-7777-7777-777777777777"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Prusa MK4 — currently being serviced.", "3D Printer", "Maintenance", "Equipment", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("88888888-8888-8888-8888-888888888888"), 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Standing desk by the window, dual monitors.", "Hot Desk 42", "Available", "Workspace", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CreatedAt",
                table: "AuditLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityType_EntityId",
                table: "AuditLogs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_Active_ResourceId_TimeRange",
                table: "Bookings",
                columns: new[] { "ResourceId", "StartDateTime", "EndDateTime" },
                filter: "\"Status\" = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_ResourceId_TimeRange",
                table: "Bookings",
                columns: new[] { "ResourceId", "StartDateTime", "EndDateTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_UserId_StartDateTime",
                table: "Bookings",
                columns: new[] { "UserId", "StartDateTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            // Database-level concurrency guard (hand-written: EF Core cannot model
            // exclusion constraints). Two Active bookings for the same resource can
            // never have overlapping [start, end) ranges — Postgres enforces this
            // atomically even for concurrent transactions, closing the
            // check-then-insert race that application-level validation alone leaves open.
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS btree_gist;");

            migrationBuilder.Sql("""
                ALTER TABLE "Bookings"
                ADD CONSTRAINT "EX_Bookings_ResourceId_TimeRange"
                EXCLUDE USING gist (
                    "ResourceId" WITH =,
                    tstzrange("StartDateTime", "EndDateTime") WITH &&
                )
                WHERE ("Status" = 'Active');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """ALTER TABLE "Bookings" DROP CONSTRAINT IF EXISTS "EX_Bookings_ResourceId_TimeRange";""");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "Bookings");

            migrationBuilder.DropTable(
                name: "Resources");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
