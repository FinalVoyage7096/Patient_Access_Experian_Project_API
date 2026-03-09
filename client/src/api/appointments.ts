import { http } from "./http";

export type CreateAppointmentRequest = {
    clinicId: string;
    providerId: string;
    patientId: string;
    startUtc: string;
    durationMinutes: number;
};

export type AppointmentResponse = {
    id: string;
    clinicId: string;
    providerId: string;
    patientId: string;
    startUtc: string;
    endUtc: string;
    status: string;
};

export function createAppointment(body: CreateAppointmentRequest) {
    return http.request<AppointmentResponse>("/api/appointments", {
        method: "POST",
        body: JSON.stringify(body),
    });
}