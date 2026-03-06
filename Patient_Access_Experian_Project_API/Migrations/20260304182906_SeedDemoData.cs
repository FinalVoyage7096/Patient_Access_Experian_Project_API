using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Patient_Access_Experian_Project_API.Migrations
{
    /// <inheritdoc />
    public partial class SeedDemoData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Clinics",
                columns: new[] { "Id", "Name", "TimeZone" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), "Health Clinic A", "America/Chicago" });

            migrationBuilder.InsertData(
                table: "Providers",
                columns: new[] { "Id", "Name", "Specialty" },
                values: new object[,]
                {
                    { new Guid("22222222-2222-2222-2222-222222222222"), "Dr. Maya Patel", "Primary Care" },
                    { new Guid("33333333-3333-3333-3333-333333333333"), "Dr. Jordan Lee", "Cardiology" },
                    { new Guid("44444444-4444-4444-4444-444444444444"), "Dr. Sofia Ramirez", "Dermatology" }
                });

            migrationBuilder.InsertData(
                table: "AvailabilityWindows",
                columns: new[] { "Id", "DayOfWeek", "EndMinuteOfDay", "ProviderId", "StartMinuteOfDay" },
                values: new object[,]
                {
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), 1, 1020, new Guid("22222222-2222-2222-2222-222222222222"), 540 },
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"), 2, 1020, new Guid("22222222-2222-2222-2222-222222222222"), 540 },
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3"), 3, 1020, new Guid("22222222-2222-2222-2222-222222222222"), 540 },
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa4"), 4, 1020, new Guid("22222222-2222-2222-2222-222222222222"), 540 },
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa5"), 5, 1020, new Guid("22222222-2222-2222-2222-222222222222"), 540 },
                    { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1"), 1, 1020, new Guid("33333333-3333-3333-3333-333333333333"), 540 },
                    { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2"), 3, 1020, new Guid("33333333-3333-3333-3333-333333333333"), 540 },
                    { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb3"), 5, 1020, new Guid("33333333-3333-3333-3333-333333333333"), 540 },
                    { new Guid("cccccccc-cccc-cccc-cccc-ccccccccccc1"), 2, 1020, new Guid("44444444-4444-4444-4444-444444444444"), 540 },
                    { new Guid("cccccccc-cccc-cccc-cccc-ccccccccccc2"), 4, 1020, new Guid("44444444-4444-4444-4444-444444444444"), 540 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AvailabilityWindows",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"));

            migrationBuilder.DeleteData(
                table: "AvailabilityWindows",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"));

            migrationBuilder.DeleteData(
                table: "AvailabilityWindows",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3"));

            migrationBuilder.DeleteData(
                table: "AvailabilityWindows",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa4"));

            migrationBuilder.DeleteData(
                table: "AvailabilityWindows",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa5"));

            migrationBuilder.DeleteData(
                table: "AvailabilityWindows",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1"));

            migrationBuilder.DeleteData(
                table: "AvailabilityWindows",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2"));

            migrationBuilder.DeleteData(
                table: "AvailabilityWindows",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb3"));

            migrationBuilder.DeleteData(
                table: "AvailabilityWindows",
                keyColumn: "Id",
                keyValue: new Guid("cccccccc-cccc-cccc-cccc-ccccccccccc1"));

            migrationBuilder.DeleteData(
                table: "AvailabilityWindows",
                keyColumn: "Id",
                keyValue: new Guid("cccccccc-cccc-cccc-cccc-ccccccccccc2"));

            migrationBuilder.DeleteData(
                table: "Clinics",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.DeleteData(
                table: "Providers",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"));

            migrationBuilder.DeleteData(
                table: "Providers",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"));

            migrationBuilder.DeleteData(
                table: "Providers",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"));
        }
    }
}
