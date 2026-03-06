using System;
using System.Collections.Generic;
using System.Text;

using FluentAssertions;
using Patient_Access_Experian_Project_API.Models;
using Patient_Access_Experian_Project_API.Services;
using Patient_Access_Experian_Project_API.Tests.TestUtilities;


namespace Patient_Access_Experian_Project_API.Tests
{
    public class AppointmentServiceTests
    {
        [Fact]
        public async Task CreateAsync_ShouldCreateAppointment_WhenNoConflict_AndWithinAvailability()
        {
            var (conn, db) = DatabaseFactory.CreateSqliteInMemoryDb();
            await using var _ = conn;
            await using var __ = db;

            // Arrange: minimal data
            var clinic = new Clinic { Id = Guid.NewGuid(), Name = "Test Clinic", TimeZone = "UTC" };
            var provider = new Provider { Id = Guid.NewGuid(), Name = "Dr Test", Specialty = "Primary Care" };
            var patient = new Patient { Id = Guid.NewGuid(), FirstName = "Alex", LastName = "J", Email = "alexj@gmail.com", Phone = "123-456-7890" };

            db.Clinics.Add(clinic);
            db.Providers.Add(provider);
            db.Patients.Add(patient);

            // Availability: Tuesday 09:00-17:00 UTC (Tuesday = 2)

            db.AvailabilityWindows.Add(new AvailabilityWindow
            {
                Id = Guid.NewGuid(),
                ProviderId = provider.Id,
                DayOfWeek = 2,
                StartMinuteOfDay = 9 * 60,
                EndMinuteOfDay = 17 * 60
            });

            await db.SaveChangesAsync();

            var service = new AppointmentService(db);

            var startUtc = new DateTime(2026, 3, 3, 15, 0, 0, DateTimeKind.Utc); // Tuesday 15:00 UTC

            // Act

            var (success, error, appt) = await service.CreateAsync(
                clinic.Id, provider.Id, patient.Id, startUtc, durationMinutes: 30);

            // Assert
            success.Should().BeTrue();
            error.Should().BeNull();
            appt.Should().NotBeNull();
            appt!.EndUtc.Should().Be(startUtc.AddMinutes(30));
            appt.Status.Should().Be(AppointmentStatus.Scheduled);
        }

        [Fact]
        public async Task CreateAsync_ShouldReturnConflict_WhenOverlapsExistingScheduled()
        {
            var (conn, db) = DatabaseFactory.CreateSqliteInMemoryDb();
            await using var _ = conn;
            await using var __ = db;

            var clinic = new Clinic { Id = Guid.NewGuid(), Name = "Test Clinic", TimeZone = "UTC" };
            var provider = new Provider { Id = Guid.NewGuid(), Name = "Dr Test", Specialty = "Primary Care" };
            var patient = new Patient { Id = Guid.NewGuid(), FirstName = "Alex", LastName = "J", Email = "alexj@gmail.com", Phone = "123-456-7890" };

            db.Clinics.Add(clinic);
            db.Providers.Add(provider);
            db.Patients.Add(patient);

            // Tuesday Availability
            db.AvailabilityWindows.Add(new AvailabilityWindow
            {
                Id = Guid.NewGuid(),
                ProviderId = provider.Id,
                DayOfWeek = 2,
                StartMinuteOfDay = 9 * 60,
                EndMinuteOfDay = 17 * 60
            });

            var existingStart = new DateTime(2026, 3, 3, 15, 0, 0, DateTimeKind.Utc);
            db.Appointments.Add(new Appointment
            {
                Id = Guid.NewGuid(),
                ClinicId = clinic.Id,
                ProviderId = provider.Id,
                PatientId = patient.Id,
                StartUtc = existingStart,
                EndUtc = existingStart.AddMinutes(30),
                Status = AppointmentStatus.Scheduled,
                CreatedUtc = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
            var service = new AppointmentService(db);

            // Overlaps: start inside existing
            var newStart = new DateTime(2026, 3, 3, 15, 15, 0, DateTimeKind.Utc);

            var (success, error, appt) = await service.CreateAsync(
                clinic.Id, provider.Id, patient.Id, newStart, durationMinutes: 30);

            success.Should().BeFalse();
            appt.Should().BeNull();
            error.Should().NotBeNull();
            error!.ToLower().Should().Contain("conflict");
        }

        [Fact]
        public async Task CreateAsync_ShouldAllow_WhenExistingIsCancelled()
        {
            var (conn, db) = DatabaseFactory.CreateSqliteInMemoryDb();
            await using var _ = conn;
            await using var __ = db;

            var clinic = new Clinic { Id = Guid.NewGuid(), Name = "Test Clinic", TimeZone = "UTC" };
            var provider = new Provider { Id = Guid.NewGuid(), Name = "Dr Test", Specialty = "Primary Care" };
            var patient = new Patient { Id = Guid.NewGuid(), FirstName = "Alex", LastName = "J", Email = "alexj@gmail.com", Phone = "123-456-7890" };

            db.Clinics.Add(clinic);
            db.Providers.Add(provider);
            db.Patients.Add(patient);

            db.AvailabilityWindows.Add(new AvailabilityWindow
            {
                Id = Guid.NewGuid(),
                ProviderId = provider.Id,
                DayOfWeek = 2,
                StartMinuteOfDay = 9 * 60,
                EndMinuteOfDay = 17 * 60
            });

            var start = new DateTime(2026, 3, 3, 15, 0, 0, DateTimeKind.Utc);

            db.Appointments.Add(new Appointment
            {
                ClinicId = clinic.Id,
                ProviderId = provider.Id,
                PatientId = patient.Id,
                StartUtc = start,
                EndUtc = start.AddMinutes(30),
                Status = AppointmentStatus.Cancelled,
                CreatedUtc = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
            var service = new AppointmentService(db);

            var (success, error, appt) = await service.CreateAsync(
                clinic.Id, provider.Id, patient.Id, start, durationMinutes: 30);

            success.Should().BeTrue();
            error.Should().BeNull();
            appt.Should().NotBeNull();
        }

        [Fact]
        public async Task CreateAsync_ShouldFail_WhenOutsideAvailability()
        {
            var (conn, db) = DatabaseFactory.CreateSqliteInMemoryDb();
            await using var _ = conn;
            await using var __ = db;

            var clinic = new Clinic { Id = Guid.NewGuid(), Name = "Test Clinic", TimeZone = "UTC" };
            var provider = new Provider { Id = Guid.NewGuid(), Name = "Dr Test", Specialty = "Primary Care" };
            var patient = new Patient { Id = Guid.NewGuid(), FirstName = "Alex", LastName = "J", Email = "alexj@gmail.com", Phone = "123-456-7890" };

            db.Clinics.Add(clinic);
            db.Providers.Add(provider);
            db.Patients.Add(patient);

            // Only 09:00–10:00 Tuesday
            db.AvailabilityWindows.Add(new AvailabilityWindow
            {
                Id = Guid.NewGuid(),
                ProviderId = provider.Id,
                DayOfWeek = 2,
                StartMinuteOfDay = 9 * 60,
                EndMinuteOfDay = 10 * 60
            });

            await db.SaveChangesAsync();
            var service = new AppointmentService(db);

            var startUtc = new DateTime(2026, 3, 3, 11, 0, 0, DateTimeKind.Utc); // outside availability window

            var (success, error, appt) = await service.CreateAsync(
                clinic.Id, provider.Id, patient.Id, startUtc, durationMinutes: 30);

            success.Should().BeFalse();
            appt.Should().BeNull();
            error!.ToLower().Should().Contain("outside");
        }

        [Fact]
        public async Task CreateAsync_ShouldFail_WhenStartUtcNotUtcKind()
        {
            var (conn, db) = DatabaseFactory.CreateSqliteInMemoryDb();
            await using var _ = conn;
            await using var __ = db;

            var clinic = new Clinic { Id = Guid.NewGuid(), Name = "Test Clinic", TimeZone = "UTC" };
            var provider = new Provider { Id = Guid.NewGuid(), Name = "Dr Test", Specialty = "Primary Care" };
            var patient = new Patient { Id = Guid.NewGuid(), FirstName = "Alex", LastName = "J", Email = "a@b.com", Phone = "123" };

            db.Clinics.Add(clinic);
            db.Providers.Add(provider);
            db.Patients.Add(patient);

            // give availability for that day/time, but it should fail earlier due to Kind
            db.AvailabilityWindows.Add(new AvailabilityWindow
            {
                Id = Guid.NewGuid(),
                ProviderId = provider.Id,
                DayOfWeek = 2,
                StartMinuteOfDay = 9 * 60,
                EndMinuteOfDay = 17 * 60
            });

            await db.SaveChangesAsync();
            var service = new AppointmentService(db);

            var startNotUtc = new DateTime(2026, 3, 3, 15, 0, 0, DateTimeKind.Local);

            var (success, error, appt) = await service.CreateAsync(
                clinic.Id, provider.Id, patient.Id, startNotUtc, durationMinutes: 30);

            success.Should().BeFalse();
            appt.Should().BeNull();
            error!.ToLower().Should().Contain("utc");
        }


    }
}
