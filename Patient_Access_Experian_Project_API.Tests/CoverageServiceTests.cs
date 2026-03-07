using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

using FluentAssertions;
using Patient_Access_Experian_Project_API.Models;
using Patient_Access_Experian_Project_API.Services;
using Patient_Access_Experian_Project_API.Tests.TestUtilities;
using Xunit;
using Patient_Access_Experian_Project_API.Data;
using Patient_Access_Experian_Project_API.Data_Transfer_Objects;


namespace Patient_Access_Experian_Project_API.Tests
{
    public class CoverageServiceTests
    {
        private static async Task<(Guid PatientId, Guid ClinicId, Guid ProviderId)> SeedBasicsAsync(PatientAccessDbContext db)
        {
            var patientId = Guid.NewGuid();
            var clinicId = Guid.NewGuid();
            var providerId = Guid.NewGuid();

            db.Patients.Add(new Patient
            {
                Id = patientId,
                FirstName = "Test",
                LastName = "Patient"
            });

            db.Clinics.Add(new Clinic
            {
                Id = clinicId,
                Name = "Test Clinic",
                TimeZone = "America/NewYork"
            });

            db.Providers.Add(new Provider
            {
                Id = providerId,
                Name = "Dr Test",
                Specialty = "Primary Care"
            });

            await db.SaveChangesAsync();

            return (patientId, clinicId, providerId);
        }

        [Fact] 
        public async Task CheckEligibilityAsync_Weekday_ReturnsEligible_AndWritesLog()
        {
            var (conn, db) = DatabaseFactory.CreateSqliteInMemoryDb();
            await using var _ = conn;

            var ids = await SeedBasicsAsync(db);
            var svc = new CoverageService(db);

            // Pick a weekday in UTC
            var scheduled = new DateTime(2026, 3, 9, 14, 0, 0, DateTimeKind.Utc); // Monday
            
            var req = new CoverageEligibilityRequest(
                ids.PatientId,
                ids.ClinicId,
                ids.ProviderId,
                "99213",
                scheduled
            );

            var (success, error, response) = await svc.CheckEligibilityAsync(req);

            success.Should().BeTrue();
            error.Should().BeNull();
            response.Should().NotBeNull();

            response!.Eligible.Should().BeTrue();
            response.CoverageStatus.Should().Be("Active");
            response.Copay.Should().Be(35m);

            db.CoverageCheckLogs.Count().Should().Be(1);

            var log = db.CoverageCheckLogs.Single();
            log.PatientId.Should().Be(ids.PatientId);
            log.ServiceCode.Should().Be("99213"); // normalized
            log.Eligible.Should().BeTrue();
            log.CoverageStatus.Should().Be("Active");
            log.CreatedUtc.Should().BeCloseTo(DateTime.UtcNow, precision: TimeSpan.FromSeconds(5));
        }






    }
}
