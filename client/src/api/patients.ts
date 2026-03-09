import { http } from "./http";

export type PatientSummaryDto = {
  id: string;
  firstName: string;
  lastName: string;
};

export function getPatients() {
  return http.request<PatientSummaryDto[]>("/api/patients");
}