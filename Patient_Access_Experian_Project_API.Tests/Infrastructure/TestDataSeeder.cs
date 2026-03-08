using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Patient_Access_Experian_Project_API.Data;
using Patient_Access_Experian_Project_API.Models;

namespace Patient_Access_Experian_Project_API.Tests.Infrastructure
{
    public static class TestDataSeeder
    {
        // IDs for deterministic integration tests
        public static readonly Guid ClinicId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid ProviderId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static readonly Guid PatientId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        public static async Task<(Guid PatientId, Guid ProviderId, Guid ClinicId)> SeedBasicAsync(PatientAccessDbContext db)
        {
            // Clinic
            if (!await db.Clinics.AnyAsync(c => c.Id == ClinicId))
            {
                db.Clinics.Add(new Clinic
                {
                    Id = ClinicId,
                    Name = "Test Clinic",
                    // Keep whatever your app expects here
                    TimeZone = "America/NewYork"
                });
            }

            // Provider
            if (!await db.Providers.AnyAsync(p => p.Id == ProviderId))
            {
                db.Providers.Add(new Provider
                {
                    Id = ProviderId,
                    Name = "Dr Test",
                    Specialty = "Primary Care"
                });
            }

            // Patient
            if (!await db.Patients.AnyAsync(p => p.Id == PatientId))
            {
                db.Patients.Add(new Patient
                {
                    Id = PatientId,
                    FirstName = "Test",
                    LastName = "Patient"
                });
            }

            await db.SaveChangesAsync();

            return (PatientId, ProviderId, ClinicId);
        }


        public static async Task ResetCoverageLogsAsync(PatientAccessDbContext db)
        {
            db.CoverageCheckLogs.RemoveRange(db.CoverageCheckLogs);
            await db.SaveChangesAsync();
        }

}
}