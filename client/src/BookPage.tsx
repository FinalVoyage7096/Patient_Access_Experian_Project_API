import { useEffect, useMemo, useState } from "react";
import { getProviders, getProviderSlots } from "./api/providers";
import { createAppointment } from "./api/appointments";
import { getClinics, type ClinicSummaryDto } from "./api/clinics";
import { getPatients, type PatientSummaryDto }  from "./api/patients";
import type { ProviderSummaryDto, AvailableSlotDto } from "./api/types";

const DEFAULT_SLOT_MINUTES = 30;

function iso(d: Date) {
    return d.toISOString();
}

export default function BookPage() {
    const [clinics, setClinics] = useState<ClinicSummaryDto[]>([]);
    const [patients, setPatients] = useState<PatientSummaryDto[]>([]);
    const [providers, setProviders] = useState<ProviderSummaryDto[]>([]);
    const [slots, setSlots] = useState<AvailableSlotDto[]>([]);

    const [clinicId, setClinicId] = useState<string>("");
    const [patientId, setPatientId] = useState<string>("");
    const [providerId, setProviderId] = useState<string>("");

    const [loading, setLoading] = useState(false);
    const [msg, setMsg] = useState<string | null>(null);
    const [err, setErr] = useState<string | null>(null);

    const { fromUtc, toUtc } = useMemo(() => {
        const from = new Date();
        from.setUTCHours(0, 0, 0, 0);
        const to = new Date(from);
        to.setUTCDate(to.getUTCDate() + 7);
        return { fromUtc: from, toUtc: to };
    }, []);

    useEffect(() => {
        Promise.all([getClinics(), getPatients(), getProviders()])
            .then(([c, pat, p]) => {
                setClinics(c);
                setPatients(pat);
                setProviders(p);

                if (c.length) setClinicId(c[0].id);
                if (pat.length) setPatientId(pat[0].id);
                if (p.length) setProviderId(p[0].id);
            })
            .catch((e) => setErr(String(e)));
    }, []);

    async function loadSlots() {
        if (!providerId) return;
        setErr(null);
        setMsg(null);
        setLoading(true);

        try {
            const data = await getProviderSlots({
                providerId,
                clinicId: clinicId || undefined,
                fromUtc: iso(fromUtc),
                toUtc: iso(toUtc),
                slotMinutes: DEFAULT_SLOT_MINUTES,
            });
            setSlots(data);
        }
        catch (e) {
            setErr(String(e));
        }
        finally {
            setLoading(false);
        }
    }

    async function book(slot: AvailableSlotDto) {
        if (!clinicId || !providerId || !patientId) {
            setErr("Clinic, provider, and patient are required.");
        }

        setErr(null);
        setMsg(null);
        setLoading(true);

        try {
            const appt = await createAppointment({
                clinicId,
                providerId,
                patientId,
                startUtc: slot.startUtc,
                durationMinutes: DEFAULT_SLOT_MINUTES,
            });

            setMsg(`Booked! Appointment ${appt.id} (${new Date(appt.startUtc).toUTCString()} → ${new Date(appt.endUtc).toUTCString()})`);

            await loadSlots(); // refresh slots so the booked appt disappears

        }
        catch (e) {
            setErr(String(e));
        }
        finally {
            setLoading(false);
        }
    }

    return (
    <div className="min-h-screen bg-slate-950 text-slate-100 p-8">
      <div className="mx-auto max-w-5xl">
        <h1 className="text-3xl font-bold">Book an Appointment</h1>
        <p className="mt-1 text-slate-300">
          Select a clinic, patient, and provider. Load slots, then book.
        </p>

        {(err || msg) && (
          <div
            className={[
              "mt-4 rounded-xl border p-3",
              err
                ? "border-red-500/40 bg-red-500/10 text-red-200"
                : "border-emerald-500/40 bg-emerald-500/10 text-emerald-200",
            ].join(" ")}
          >
            {err ?? msg}
          </div>
        )}

        <div className="mt-6 grid gap-4 md:grid-cols-3">
          <div className="rounded-xl border border-white/10 bg-white/5 p-4">
            <label className="text-sm text-slate-300">Clinic</label>
            <select
              className="mt-2 w-full rounded-lg bg-slate-900 p-2"
              value={clinicId}
              onChange={(e) => setClinicId(e.target.value)}
            >
              {clinics.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name} ({c.timeZone})
                </option>
              ))}
            </select>

            <label className="mt-4 block text-sm text-slate-300">Patient</label>
            <select
              className="mt-2 w-full rounded-lg bg-slate-900 p-2"
              value={patientId}
              onChange={(e) => setPatientId(e.target.value)}
            >
              {patients.map((p) => (
                <option key={p.id} value={p.id}>
                  {p.lastName}, {p.firstName}
                </option>
              ))}
            </select>

            <label className="mt-4 block text-sm text-slate-300">Provider</label>
            <select
              className="mt-2 w-full rounded-lg bg-slate-900 p-2"
              value={providerId}
              onChange={(e) => setProviderId(e.target.value)}
            >
              {providers.map((p) => (
                <option key={p.id} value={p.id}>
                  {p.name} — {p.specialty}
                </option>
              ))}
            </select>

            <button
              onClick={loadSlots}
              disabled={!providerId || loading}
              className="mt-4 w-full rounded-lg bg-blue-600 px-4 py-2 font-semibold disabled:opacity-50"
            >
              {loading ? "Loading..." : "Load Slots (Next 7 days)"}
            </button>
          </div>

          <div className="md:col-span-2 rounded-xl border border-white/10 bg-white/5 p-4">
            <div className="flex items-center justify-between">
              <h2 className="text-xl font-semibold">Available Slots</h2>
              <div className="text-sm text-slate-300">
                {fromUtc.toISOString().slice(0, 10)} → {toUtc.toISOString().slice(0, 10)} (UTC)
              </div>
            </div>

            {slots.length === 0 ? (
              <div className="mt-4 text-slate-300">
                No slots loaded yet (or none available). Click “Load Slots”.
              </div>
            ) : (
              <div className="mt-4 grid gap-2 sm:grid-cols-2 lg:grid-cols-3">
                {slots.map((s) => (
                  <button
                    key={s.startUtc}
                    onClick={() => book(s)}
                    disabled={loading}
                    className="rounded-lg border border-white/10 bg-slate-900 p-3 text-left hover:bg-slate-800 disabled:opacity-50"
                  >
                    <div className="text-sm text-slate-300">Start</div>
                    <div className="font-semibold">{new Date(s.startUtc).toUTCString()}</div>
                    <div className="mt-1 text-xs text-slate-400">
                      {DEFAULT_SLOT_MINUTES} min
                    </div>
                  </button>
                ))}
              </div>
            )}
          </div>
        </div>

        <div className="mt-6 text-xs text-slate-400">
        </div>
      </div>
    </div>
  );
}