using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Patient_Access_Experian_Project_API.Migrations
{
    /// <inheritdoc />
    public partial class AddCoverageEligibilityAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CoverageCheckLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ScheduledStartUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequestJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResponseJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Eligible = table.Column<bool>(type: "bit", nullable: false),
                    CoverageStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Copay = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DeductibleRemaining = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EstimatedPatientResponsibility = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoverageCheckLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CoverageCheckLogs_CreatedUtc",
                table: "CoverageCheckLogs",
                column: "CreatedUtc");

            migrationBuilder.CreateIndex(
                name: "IX_CoverageCheckLogs_PatientId_CreatedUtc",
                table: "CoverageCheckLogs",
                columns: new[] { "PatientId", "CreatedUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CoverageCheckLogs");
        }
    }
}
