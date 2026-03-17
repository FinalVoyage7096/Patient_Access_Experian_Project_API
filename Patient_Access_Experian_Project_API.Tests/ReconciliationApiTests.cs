using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Patient_Access_Experian_Project_API.Data;
using Patient_Access_Experian_Project_API.Models;
using Patient_Access_Experian_Project_API.Tests.Infrastructure;
using Xunit;

namespace Patient_Access_Experian_Project_API.Tests
{
    public class ReconciliationApiTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public ReconciliationApiTests(CustomWebApplicationFactory factory)
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

        public record ClaimsSummaryDto(
            Guid? ClinicId,
            Guid? PayerId,
            DateTime? FromUtc,
            DateTime? ToUtc,
            int ClaimsCreated,
            int ClaimsSubmitted,
            int ClaimsPaid,
            int ClaimsDenied,
            decimal DenialRate,
            decimal TotalCharge,
            decimal TotalAllowed,
            decimal TotalPayerPaid,
            decimal TotalPatientResponsibility,
            double? AvgDaysToPay
        );

        public record ClaimTransactionDto(
            Guid Id,
            Guid ClaimId,
            string Type,
            decimal Amount,
            string Currency,
            string? Reference,
            DateTime CreatedUtc
        );

        [Fact]
        public async Task ClaimsSummary_ReturnsExpectedKpis()
        {
            var client = _factory.CreateClient();

            await WithDbAsync(ResetClaimsAsync);

            var ids = await WithDbAsync(db => TestDataSeeder.SeedBasicAsync(db));
            var payerId = await WithDbAsync(async db =>
            {
                var payer = new Payer { Id = Guid.NewGuid(), Name = "Payer KPI" };
                db.Payers.Add(payer);
                await db.SaveChangesAsync();
                return payer.Id;
            });

            var now = DateTime.UtcNow;

            // Create 4 claims:
            // 1 Draft, 1 Submitted, 1 Paid, 1 Denied
            await WithDbAsync(async db =>
            {
                db.Claims.AddRange(
                    new Claim
                    {
                        Id = Guid.NewGuid(),
                        ClinicId = ids.ClinicId,
                        PatientId = ids.PatientId,
                        ProviderId = ids.ProviderId,
                        PayerId = payerId,
                        Status = ClaimStatus.Draft,
                        TotalCharge = 100m,
                        CreatedUtc = now.AddDays(-3),
                        UpdatedUtc = now.AddDays(-3),
                    },
                    new Claim
                    {
                        Id = Guid.NewGuid(),
                        ClinicId = ids.ClinicId,
                        PatientId = ids.PatientId,
                        ProviderId = ids.ProviderId,
                        PayerId = payerId,
                        Status = ClaimStatus.Submitted,
                        TotalCharge = 200m,
                        CreatedUtc = now.AddDays(-2),
                        UpdatedUtc = now.AddDays(-2),
                    },
                    new Claim
                    {
                        Id = Guid.NewGuid(),
                        ClinicId = ids.ClinicId,
                        PatientId = ids.PatientId,
                        ProviderId = ids.ProviderId,
                        PayerId = payerId,
                        Status = ClaimStatus.Paid,
                        TotalCharge = 300m,
                        AllowedAmount = 240m,
                        PayerPaid = 192m,
                        PatientResponsibility = 48m,
                        CreatedUtc = now.AddDays(-10),
                        UpdatedUtc = now.AddDays(-5) // 5 days to pay
                    },
                    new Claim
                    {
                        Id = Guid.NewGuid(),
                        ClinicId = ids.ClinicId,
                        PatientId = ids.PatientId,
                        ProviderId = ids.ProviderId,
                        PayerId = payerId,
                        Status = ClaimStatus.Denied,
                        TotalCharge = 150m,
                        AllowedAmount = 0m,
                        PayerPaid = 0m,
                        PatientResponsibility = 150m,
                        DenialReasonCode = "DEMO",
                        CreatedUtc = now.AddDays(-1),
                        UpdatedUtc = now.AddDays(-1)
                    }
                );

                await db.SaveChangesAsync();
            });

            var fromUtc = now.AddDays(-30).ToString("O");
            var toUtc = now.AddDays(1).ToString("O");

            var url = $"/api/reconciliation/claims-summary?clinicId={ids.ClinicId}&payerId={payerId}&fromUtc={Uri.EscapeDataString(fromUtc)}&toUtc={Uri.EscapeDataString(toUtc)}";

            var resp = await client.GetAsync(url);
            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            var summary = await resp.Content.ReadFromJsonAsync<ClaimsSummaryDto>();
            summary.Should().NotBeNull();

            summary!.ClaimsCreated.Should().Be(4);
            summary.ClaimsSubmitted.Should().Be(1);
            summary.ClaimsPaid.Should().Be(1);
            summary.ClaimsDenied.Should().Be(1);

            summary.TotalCharge.Should().Be(100m + 200m + 300m + 150m);
            summary.TotalAllowed.Should().Be(240m); // only paid had allowed set here
            summary.TotalPayerPaid.Should().Be(192m);
            summary.TotalPatientResponsibility.Should().Be(48m + 150m);

            // denialRate = denied / created = 1/4 = 0.25
            summary.DenialRate.Should().Be(0.25m);

            // AvgDaysToPay should be ~5 (only 1 paid claim)
            summary.AvgDaysToPay.Should().NotBeNull();
            summary.AvgDaysToPay!.Value.Should().Be(5);
        }

        [Fact]
        public async Task ClaimTransactions_ReturnsOrderedLedgerRows()
        {
            var client = _factory.CreateClient();

            await WithDbAsync(ResetClaimsAsync);

            var ids = await WithDbAsync(db => TestDataSeeder.SeedBasicAsync(db));
            var payerId = await WithDbAsync(async db =>
            {
                var payer = new Payer { Id = Guid.NewGuid(), Name = "Payer Ledger" };
                db.Payers.Add(payer);
                await db.SaveChangesAsync();
                return payer.Id;
            });

            var claimId = Guid.NewGuid();

            await WithDbAsync(async db =>
            {
                db.Claims.Add(new Claim
                {
                    Id = claimId,
                    ClinicId = ids.ClinicId,
                    PatientId = ids.PatientId,
                    ProviderId = ids.ProviderId,
                    PayerId = payerId,
                    Status = ClaimStatus.Submitted,
                    TotalCharge = 100m,
                    CreatedUtc = DateTime.UtcNow.AddDays(-2),
                    UpdatedUtc = DateTime.UtcNow.AddDays(-2),
                });

                db.ClaimTransactions.AddRange(
                    new ClaimTransaction
                    {
                        Id = Guid.NewGuid(),
                        ClaimId = claimId,
                        Type = ClaimTransactionType.Submit,
                        Amount = 0m,
                        Currency = "USD",
                        Reference = "K1",
                        CreatedUtc = DateTime.UtcNow.AddMinutes(-2)
                    },
                    new ClaimTransaction
                    {
                        Id = Guid.NewGuid(),
                        ClaimId = claimId,
                        Type = ClaimTransactionType.Pay,
                        Amount = 80m,
                        Currency = "USD",
                        Reference = "TRACE",
                        CreatedUtc = DateTime.UtcNow.AddMinutes(-1)
                    }
                );

                await db.SaveChangesAsync();
            });

            var resp = await client.GetAsync($"/api/claims/{claimId}/transactions");
            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            var tx = await resp.Content.ReadFromJsonAsync<List<ClaimTransactionDto>>();
            tx.Should().NotBeNull();
            tx!.Count.Should().Be(2);

            tx.Should().BeInAscendingOrder(x => x.CreatedUtc);
            tx[0].Type.Should().Be(nameof(ClaimTransactionType.Submit));
            tx[1].Type.Should().Be(nameof(ClaimTransactionType.Pay));
        }

        private static async Task ResetClaimsAsync(PatientAccessDbContext db)
        {
            db.ClaimTransactions.RemoveRange(db.ClaimTransactions);
            db.ClaimServiceLines.RemoveRange(db.ClaimServiceLines);
            db.Claims.RemoveRange(db.Claims);
            db.Payers.RemoveRange(db.Payers);
            await db.SaveChangesAsync();
        }
    }
}