import { http } from "./http";

export type ClaimSummaryDto = {
    id: string;
    clinicId: string;
    patientId: string;
    providerId: string;
    payerId: string;
    status: string;
    totalCharge: number;
    createdUtc: string;
};

export type ClaimTransactionDto = {
    id: string;
    claimId: string;
    type: string;
    amount: number;
    currency: string;
    reference?: string | null;
    createdUtc: string;
};

export type ClaimsSummaryDto = {
    clinicId?: string | null;
    payerId?: string | null;
    fromUtc?: string | null;
    toUtc?: string | null;
    claimsCreated: number;
    claimsSubmitted: number;
    claimsPaid: number;
    claimsDenied: number;
    denialRate: number;
    totalCharge: number;
    totalAllowed: number;
    totalPayerPaid: number;
    totalPatientResponsibility: number;
    avgDaysToPay?: number | null;
};

export function getClaims(params: {
    clinicId?: string;
    payerId?: string;
    status?: string;
    fromUtc?: string;
    toUtc?: string;
    take?: number;
    skip?: number;
}) {
    const q = new URLSearchParams();
    if (params.clinicId) q.set("clinicId", params.clinicId);
    if (params.payerId) q.set("payerId", params.payerId);
    if (params.status) q.set("status", params.status);
    if (params.fromUtc) q.set("fromUtc", params.fromUtc);
    if (params.toUtc) q.set("toUtc", params.toUtc);
    if (params.take != null) q.set("take", String(params.take));
    if (params.skip != null) q.set("skip", String(params.skip));

    return http.request<ClaimSummaryDto[]>(`/api/claims?${q.toString()}`);
}

export function getClaimsSummary(params: {
    clinicId?: string;
    payerId?: string;
    fromUtc?: string;
    toUtc?: string;
}) {
    const q = new URLSearchParams();
    if (params.clinicId) q.set("clinicId", params.clinicId);
    if (params.payerId) q.set("payerId", params.payerId);
    if (params.fromUtc) q.set("fromUtc", params.fromUtc);
    if (params.toUtc) q.set("toUtc", params.toUtc);

    return http.request<ClaimsSummaryDto>(`/api/reconciliation/claims-summary?${q.toString()}`);
}

export function getClaimTransactions(claimId: string) {
    return http.request<ClaimTransactionDto[]>(`/api/claims/${claimId}/transactions`);
}