import { http } from "./http";
import type { ProviderSummaryDto, AvailableSlotDto} from "./types";

export function getProviders() {
    return http.request<ProviderSummaryDto[]>("/api/providers");
}

export function getProviderSlots(params: {
    providerId: string,
    clinicId?: string,
    fromUtc: string;
    toUtc: string;
    slotMinutes?: number;
}) {
    const q = new URLSearchParams();
    if (params.clinicId) q.set("clinicId", params.clinicId);
    q.set("fromUtc", params.fromUtc);
    q.set("toUtc", params.toUtc);
    if (params.slotMinutes) q.set("slotMinutes", String(params.slotMinutes));

    return http.request<AvailableSlotDto[]>(
        `/api/providers/${params.providerId}/slots?${q.toString()}`
    );
}