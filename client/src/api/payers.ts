import { http } from "./http";

export type PayerDto = { id: string; name: string };

export function getPayers() {
    return http.request<PayerDto[]>("/api/payers");
}