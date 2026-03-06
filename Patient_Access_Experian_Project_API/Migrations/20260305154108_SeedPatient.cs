using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Patient_Access_Experian_Project_API.Migrations
{
    /// <inheritdoc />
    public partial class SeedPatient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Patients",
                columns: new[] { "Id", "DateOfBirth", "Email", "FirstName", "LastName", "Phone" },
                values: new object[] { new Guid("55555555-5555-5555-5555-555555555555"), null, "alex.johnson@gmail.com", "Alex", "Johnson", "555-111-2222" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Patients",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"));
        }
    }
}
