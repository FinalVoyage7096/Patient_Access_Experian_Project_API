import { http } from "./http";

export function seedDemoClaims(params: {
  clinicId: string;
  payerId: string;
  count?: number;
  daysBack?: number;
}) {
  const q = new URLSearchParams();
  q.set("clinicId", params.clinicId);
  q.set("payerId", params.payerId);
  if (params.count != null) q.set("count", String(params.count));
  if (params.daysBack != null) q.set("daysBack", String(params.daysBack));

  return http.request(`/api/demo/seed-claims?${q.toString()}`, { method: "POST" });
}