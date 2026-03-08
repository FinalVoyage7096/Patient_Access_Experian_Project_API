using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Patient_Access_Experian_Project_API.Data;
using Patient_Access_Experian_Project_API.Data_Transfer_Objects;
using Patient_Access_Experian_Project_API.Models;
using Patient_Access_Experian_Project_API.Tests.Infrastructure;
using Xunit;

namespace Patient_Access_Experian_Project_API.Tests
{
    public class ProviderSlotsApiTest : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public ProviderSlotsApiTest(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private async Task<T> WithDbAsync<T>(Func<PatientAccessDbContext, Task<T>> action)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PatientAccessDbContext>();
            return await action(db);
        }

        private async Task WithDbAsync(Func<PatientAccessDbContext, Task> action)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PatientAccessDbContext>();
            await action(db);
        }

        [Fact]
        public async Task ProviderSlots_ReturnsSlots_AndSkipsConflictingAppointments()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Seed base entities
            var ids = await WithDbAsync(db => TestDataSeeder.SeedBasicAsync(db));

            // Pick a known weekday (Monday) in UTC
            var fromUtc = NextWeekdayUtc(DateTime.UtcNow, DayOfWeek.Monday).Date.AddHours(9); // 9:00 AM UTC
            var toUtc = fromUtc.Date.AddHours(12); // 12:00 PM UTC
            var slotMinutes = 30;

            // Availability: Monday 9:00-12:00
            await WithDbAsync(async db =>
            {
                db.AvailabilityWindows.Add(new AvailabilityWindow
                {
                    Id = Guid.NewGuid(),
                    ProviderId = ids.ProviderId,
                    DayOfWeek = (int)DayOfWeek.Monday,     // IMPORTANT: your model uses int
                    StartMinuteOfDay = 9 * 60,             // 540
                    EndMinuteOfDay = 12 * 60               // 720
                });

                // Block the 10:00–10:30 slot with an appointment
                db.Appointments.Add(new Appointment
                {
                    Id = Guid.NewGuid(),
                    ClinicId = ids.ClinicId,
                    ProviderId = ids.ProviderId,
                    PatientId = ids.PatientId,
                    StartUtc = fromUtc.Date.AddHours(10),  // 10:00
                    EndUtc = fromUtc.Date.AddHours(10).AddMinutes(30),
                    Status = AppointmentStatus.Scheduled
                });

                await db.SaveChangesAsync();
            });

            // Act
            var url =
                $"/api/providers/{ids.ProviderId}/slots" +
                $"?fromUtc={Uri.EscapeDataString(fromUtc.ToString("O"))}" +
                $"&toUtc={Uri.EscapeDataString(toUtc.ToString("O"))}" +
                $"&slotMinutes={slotMinutes}" +
                $"&clinicId={ids.ClinicId}";

            var resp = await client.GetAsync(url);

            // Assert http
            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            var slots = await resp.Content.ReadFromJsonAsync<List<AvailableSlotDto>>();
            slots.Should().NotBeNull();

            // Should not incluide the blocked slot starting at 10:00
            var blockedStart = fromUtc.Date.AddHours(10);
            slots!.Any(s => s.StartUtc == blockedStart).Should().BeFalse();

            // But should include nearby valid slots like 9:00 and 9:30
            slots.Any(s => s.StartUtc == fromUtc.Date.AddHours(9)).Should().BeTrue();
            slots.Any(s => s.StartUtc == fromUtc.Date.AddHours(9).AddMinutes(30)).Should().BeTrue();
        }

        private static DateTime NextWeekdayUtc(DateTime utcNow, DayOfWeek day)
        {
            var daysAhead = ((int)day - (int)utcNow.DayOfWeek + 7) % 7;
            if (daysAhead == 0) daysAhead = 7; // always the next occurence
            return utcNow.Date.AddDays(daysAhead);
        }
    }
}
