using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Patient_Access_Experian_Project_API.Data;
using Patient_Access_Experian_Project_API.Models;
using Patient_Access_Experian_Project_API.Tests.Infrastructure;
using Xunit;

namespace Patient_Access_Experian_Project_API.Tests
{
    public class ClaimsApiTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public ClaimsApiTests(CustomWebApplicationFactory factory)
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

        public record CreateClaimServiceLineDto(string ServiceCode, int Units, decimal ChargeAmount);

        public record CreateClaimRequest(
            Guid ClinicId,
            Guid PatientId,
            Guid ProviderId,
            Guid PayerId,
            Guid? AppointmentId,
            List<CreateClaimServiceLineDto> ServiceLines
        );

        public record ClaimCreateResponse(Guid Id);

        [Fact]
        public async Task Submit_IsIdempotent_WritesSingleSubmitTransaction()
        {
            var client = _factory.CreateClient();

            // Reset claim tables (shared in-memory DB across tests)
            await WithDbAsync(ResetClaimsAsync);

            // Seed basic entities + a payer
            var ids = await WithDbAsync(db => TestDataSeeder.SeedBasicAsync(db));
            var payerId = await WithDbAsync(async db =>
            {
                var payer = new Payer { Id = Guid.NewGuid(), Name = "Test Payer" };
                db.Payers.Add(payer);
                await db.SaveChangesAsync();
                return payer.Id;
            });

            // Create a claim via API
            var createReq = new CreateClaimRequest(
                ClinicId: ids.ClinicId,
                PatientId: ids.PatientId,
                ProviderId: ids.ProviderId,
                PayerId: payerId,
                AppointmentId: null,
                ServiceLines: new List<CreateClaimServiceLineDto>
                {
                    new("99213", 1, 100m)
                }
            );

            var createResp = await client.PostAsJsonAsync("/api/claims", createReq);
            createResp.StatusCode.Should().Be(HttpStatusCode.Created);

            var created = await createResp.Content.ReadFromJsonAsync<ClaimCreateResponse>();
            created.Should().NotBeNull();
            var claimId = created!.Id;

            // Submit twice with the same idempotency key
            const string idemKey = "idem-test-123";
            var msg1 = new HttpRequestMessage(HttpMethod.Post, $"/api/claims/{claimId}/submit");
            msg1.Headers.Add("Idempotency-Key", idemKey);

            var msg2 = new HttpRequestMessage(HttpMethod.Post, $"/api/claims/{claimId}/submit");
            msg2.Headers.Add("Idempotency-Key", idemKey);

            var submit1 = await client.SendAsync(msg1);
            var submit2 = await client.SendAsync(msg2);

            submit1.StatusCode.Should().Be(HttpStatusCode.OK);
            submit2.StatusCode.Should().Be(HttpStatusCode.OK);

            // Assert DB: claim is submitted and only one Submit transaction exists
            await WithDbAsync(async db =>
            {
                var claim = await db.Claims
                    .Include(c => c.Transactions)
                    .FirstAsync(c => c.Id == claimId);

                claim.Status.Should().Be(ClaimStatus.Submitted);
                claim.SubmissionIdempotencyKey.Should().Be(idemKey);

                var submitTxCount = claim.Transactions.Count(t => t.Type == ClaimTransactionType.Submit);
                submitTxCount.Should().Be(1);
            });
        }

        [Fact]
        public async Task Adjudicate_PaysClaim_AndWritesPayAndAdjustTransactions()
        {
            var client = _factory.CreateClient();

            await WithDbAsync(ResetClaimsAsync);

            var ids = await WithDbAsync(db => TestDataSeeder.SeedBasicAsync(db));
            var payerId = await WithDbAsync(async db =>
            {
                var payer = new Payer { Id = Guid.NewGuid(), Name = "Test Payer 2" };
                db.Payers.Add(payer);
                await db.SaveChangesAsync();
                return payer.Id;
            });

            var createReq = new CreateClaimRequest(
                ClinicId: ids.ClinicId,
                PatientId: ids.PatientId,
                ProviderId: ids.ProviderId,
                PayerId: payerId,
                AppointmentId: null,
                ServiceLines: new List<CreateClaimServiceLineDto>
                {
                    new("99213", 1, 200m)
                }
            );

            var createResp = await client.PostAsJsonAsync("/api/claims", createReq);
            createResp.StatusCode.Should().Be(HttpStatusCode.Created);

            var created = await createResp.Content.ReadFromJsonAsync<ClaimCreateResponse>();
            created.Should().NotBeNull();
            var claimId = created!.Id;

            // Submit first
            var submitMsg = new HttpRequestMessage(HttpMethod.Post, $"/api/claims/{claimId}/submit");
            submitMsg.Headers.Add("Idempotency-Key", "idem-adj-pay-1");
            var submitResp = await client.SendAsync(submitMsg);
            submitResp.StatusCode.Should().Be(HttpStatusCode.OK);

            // Adjudicate
            var adjudicateResp = await client.PostAsync($"/api/claims/{claimId}/adjudicate", null);
            adjudicateResp.StatusCode.Should().Be(HttpStatusCode.OK);

            // Assert DB: Paid + has Adjust + Pay transactions (and no Deny)
            await WithDbAsync(async db =>
            {
                var claim = await db.Claims
                    .Include(c => c.Transactions)
                    .FirstAsync(c => c.Id == claimId);

                claim.Status.Should().Be(ClaimStatus.Paid);
                claim.DenialReasonCode.Should().BeNull();

                claim.AllowedAmount.Should().NotBeNull();
                claim.PayerPaid.Should().NotBeNull();
                claim.PatientResponsibility.Should().NotBeNull();

                claim.Transactions.Any(t => t.Type == ClaimTransactionType.Pay).Should().BeTrue();
                claim.Transactions.Any(t => t.Type == ClaimTransactionType.Adjust).Should().BeTrue();
                claim.Transactions.Any(t => t.Type == ClaimTransactionType.Deny).Should().BeFalse();
            });
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