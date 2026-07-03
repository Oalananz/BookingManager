using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingManager.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingOverlapExclusionConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS btree_gist;");

            migrationBuilder.Sql(
                """
                ALTER TABLE "Bookings" ADD CONSTRAINT "EX_Bookings_ResourceId_TimeRange"
                EXCLUDE USING gist (
                    "ResourceId" WITH =,
                    tstzrange("StartDateTime", "EndDateTime") WITH &&
                ) WHERE ("Status" = 'Confirmed');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE \"Bookings\" DROP CONSTRAINT \"EX_Bookings_ResourceId_TimeRange\";");
            migrationBuilder.Sql("DROP EXTENSION IF EXISTS btree_gist;");
        }
    }
}
