using System;
using System.Collections.Generic;
using System.Text;

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
    }
}
