import { http } from "./http";

export type ClinicSummaryDto = {
    id: string;
    name: string;
    timeZone: string;
};

export function getClinics() {
    return http.request<ClinicSummaryDto[]>("/api/clinics");
}