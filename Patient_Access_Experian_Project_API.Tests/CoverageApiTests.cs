using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Patient_Access_Experian_Project_API.Data;
using Patient_Access_Experian_Project_API.Data_Transfer_Objects;
using Patient_Access_Experian_Project_API.Tests.Infrastructure;
using Xunit;
using Microsoft.EntityFrameworkCore;

namespace Patient_Access_Experian_Project_API.Tests
{
    public class CoverageApiTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public CoverageApiTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private async Task WithDbAsync(Func<PatientAccessDbContext, Task> action)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PatientAccessDbContext>();
            await action(db);
        }

        private async Task<T> WithDbAsync<T>(Func<PatientAccessDbContext, Task<T>> action)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PatientAccessDbContext>();
            return await action(db);
        }

        [Fact]
        public async Task Eligibility_WritesAuditLog_AndReturns200()
        {
            // Arrange
            var client = _factory.CreateClient();

            await WithDbAsync(db => TestDataSeeder.ResetCoverageLogsAsync(db));
            var ids = await WithDbAsync(db => TestDataSeeder.SeedBasicAsync(db));

            // Build request
            var request = new CoverageEligibilityRequest(
                PatientId: ids.PatientId,
                ClinicId: ids.ClinicId,
                ProviderId: ids.ProviderId,
                ServiceCode: "99213",
                ScheduledStartUtc: DateTime.UtcNow.AddDays(1)
            );

            // Act
            var resp = await client.PostAsJsonAsync("/api/coverage/eligibility", request);

            // Assert - http
            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            // Assert - database log written
            var count = await WithDbAsync(db => db.CoverageCheckLogs.CountAsync());
            count.Should().Be(1);
        }

        [Fact]
        public async Task LogsEndpoint_ReturnMostRecent_RespectsTake()
        {
            var client = _factory.CreateClient();
            
            var ids = await WithDbAsync( db => TestDataSeeder.SeedBasicAsync(db));

            // Create 3 logs by calling eligibility 3 times
            for (int i = 0; i < 3; i++)
            {
                var req = new CoverageEligibilityRequest(
                    PatientId: ids.PatientId,
                    ClinicId: ids.ClinicId,
                    ProviderId: ids.ProviderId,
                    ServiceCode: "99213",
                    ScheduledStartUtc: DateTime.UtcNow.AddDays(1+i)

                );

                var resp = await client.PostAsJsonAsync("/api/coverage/eligibility", req);
                resp.EnsureSuccessStatusCode();
            }

            // Act: request only 2 logs
            var logs = await client.GetFromJsonAsync<List<CoverageLogItemDto>>("/api/coverage/logs?take=2");

            // Assert
            logs.Should().NotBeNull();
            logs!.Count.Should().Be(2);
        }

        [Fact]
        public async Task LogsEndpoint_SupportsSkipAndTake_PagesCorrectly()
        {
            // Arrange
            var client = _factory.CreateClient();

            await WithDbAsync(db => TestDataSeeder.ResetCoverageLogsAsync(db));
            var ids = await WithDbAsync(db => TestDataSeeder.SeedBasicAsync(db));

            // Create 3 logs (make sure they have distinct CreatedUtc order by calling endpoint)
            for (int i = 0; i < 3; i++)
            {
                var req = new CoverageEligibilityRequest(
                    PatientId: ids.PatientId,
                    ClinicId: ids.ClinicId,
                    ProviderId: ids.ProviderId,
                    ServiceCode: "99213",
                    ScheduledStartUtc: DateTime.UtcNow.AddDays(1 + i)
                );

                var resp = await client.PostAsJsonAsync("/api/coverage/eligibility", req);
                resp.EnsureSuccessStatusCode();
            }

            // Act: page 1 (2 items)
            var page1 = await client.GetFromJsonAsync<List<CoverageLogItemDto>>("/api/coverage/logs?take=2&skip=0");

            // Act: page 2 (should be 1 item remaining)
            var page2 = await client.GetFromJsonAsync<List<CoverageLogItemDto>>("/api/coverage/logs?take=2&skip=2");

            // Assert
            page1.Should().NotBeNull();
            page2.Should().NotBeNull();

            page1!.Count.Should().Be(2);
            page2!.Count.Should().Be(1);

            // Ensure no overlap between pages (by ReferenceId)
            var page1Ids = page1.Select(x => x.ReferenceId).ToHashSet();
            page1Ids.Contains(page2[0].ReferenceId).Should().BeFalse();

            // Optional: ensure overall ordering is descending by CreatedUtc across concatenated pages
            var all = page1.Concat(page2).ToList();
            all.Should().BeInDescendingOrder(x => x.CreatedUtc);
        }

        [Fact]
        public async Task Eligibility_MissingServiceCode_Returns400()
        {
            // Arrange
            var client = _factory.CreateClient();

            await WithDbAsync(db => TestDataSeeder.ResetCoverageLogsAsync(db));
            var ids = await WithDbAsync(db => TestDataSeeder.SeedBasicAsync(db));

            // ServiceCode intentionally invalid (empty)
            var badRequest = new CoverageEligibilityRequest(
                PatientId: ids.PatientId,
                ClinicId: ids.ClinicId,
                ProviderId: ids.ProviderId,
                ServiceCode: "",
                ScheduledStartUtc: DateTime.UtcNow.AddDays(1)
            );

            // Act
            var resp = await client.PostAsJsonAsync("/api/coverage/eligibility", badRequest);

            // Assert
            resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

    }
}
