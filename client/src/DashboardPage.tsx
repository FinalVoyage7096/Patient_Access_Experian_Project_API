import { useEffect, useMemo, useState } from "react";
import { getClinics, type ClinicSummaryDto } from "./api/clinics";
import { getPayers, type PayerDto } from "./api/payers";
import {
  getClaims,
  getClaimsSummary,
  getClaimTransactions,
  type ClaimSummaryDto,
  type ClaimsSummaryDto,
  type ClaimTransactionDto,
} from "./api/claims";
import { seedDemoClaims } from "./api/demo";

function isoDaysAgo(days: number) {
  const d = new Date();
  d.setUTCHours(0, 0, 0, 0);
  d.setUTCDate(d.getUTCDate() - days);
  return d.toISOString();
}

function money(n: number) {
  return n.toLocaleString(undefined, { style: "currency", currency: "USD" });
}

export default function DashboardPage() {
  const [clinics, setClinics] = useState<ClinicSummaryDto[]>([]);
  const [payers, setPayers] = useState<PayerDto[]>([]);

  const [clinicId, setClinicId] = useState<string>("");
  const [payerId, setPayerId] = useState<string>("");
  const [status, setStatus] = useState<string>("");

  const [summary, setSummary] = useState<ClaimsSummaryDto | null>(null);
  const [claims, setClaims] = useState<ClaimSummaryDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  const [selectedClaim, setSelectedClaim] = useState<ClaimSummaryDto | null>(
    null
  );
  const [ledger, setLedger] = useState<ClaimTransactionDto[]>([]);
  const [ledgerOpen, setLedgerOpen] = useState(false);

  const fromUtc = useMemo(() => isoDaysAgo(30), []);
  const toUtc = useMemo(() => new Date().toISOString(), []);

  useEffect(() => {
    Promise.all([getClinics(), getPayers()])
      .then(([c, p]) => {
        setClinics(c);
        setPayers(p);
        if (c.length) setClinicId(c[0].id);
      })
      .catch((e) => setErr(String(e)));
  }, []);

  async function refresh() {
    setLoading(true);
    setErr(null);
    try {
      const [s, list] = await Promise.all([
        getClaimsSummary({
          clinicId: clinicId || undefined,
          payerId: payerId || undefined,
          fromUtc,
          toUtc,
        }),
        getClaims({
          clinicId: clinicId || undefined,
          payerId: payerId || undefined,
          status: status || undefined,
          fromUtc,
          toUtc,
          take: 25,
          skip: 0,
        }),
      ]);

      setSummary(s);
      setClaims(list);
    } catch (e) {
      setErr(String(e));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    if (clinics.length === 0) return;
    refresh();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [clinicId, payerId, status]);

  async function openLedger(c: ClaimSummaryDto) {
    setSelectedClaim(c);
    setLedgerOpen(true);
    setLedger([]);
    try {
      const tx = await getClaimTransactions(c.id);
      setLedger(tx);
    } catch (e) {
      setErr(String(e));
    }
  }

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100 p-8">
      <div className="mx-auto max-w-6xl">
        <div className="flex items-end justify-between gap-4">
          <div>
            <h1 className="text-3xl font-bold">Revenue Cycle Dashboard</h1>
            <p className="mt-1 text-slate-300">
              Claims + ledger transactions (last 30 days)
            </p>
          </div>

          {/* Buttons */}
          <div className="flex gap-2">
            <button
              onClick={async () => {
                if (!clinicId) {
                  setErr("Select a clinic before seeding demo data.");
                  return;
                }

                const resolvedPayerId = payerId || payers[0]?.id;
                if (!resolvedPayerId) {
                  setErr("No payers found. Create/seed payers first.");
                  return;
                }

                setLoading(true);
                setErr(null);
                try {
                  await seedDemoClaims({
                    clinicId,
                    payerId: resolvedPayerId,
                    count: 12,
                    daysBack: 30,
                  });

                  if (!payerId) setPayerId(resolvedPayerId);

                  await refresh();
                } catch (e) {
                  setErr(String(e));
                } finally {
                  setLoading(false);
                }
              }}
              className="rounded-lg border border-white/10 bg-slate-900 px-4 py-2 font-semibold hover:bg-slate-800 disabled:opacity-50"
              disabled={loading || !clinicId || payers.length === 0}
            >
              Seed Demo Data
            </button>

            <button
              onClick={refresh}
              className="rounded-lg bg-blue-600 px-4 py-2 font-semibold disabled:opacity-50"
              disabled={loading}
            >
              {loading ? "Refreshing..." : "Refresh"}
            </button>
          </div>
        </div>

        {err && (
          <div className="mt-4 rounded-xl border border-red-500/40 bg-red-500/10 p-3 text-red-200">
            {err}
          </div>
        )}

        {/* Filters */}
        <div className="mt-6 grid gap-4 md:grid-cols-3">
          <div className="rounded-xl border border-white/10 bg-white/5 p-4">
            <label className="text-sm text-slate-300">Clinic (Hospital)</label>
            <select
              className="mt-2 w-full rounded-lg bg-slate-900 p-2"
              value={clinicId}
              onChange={(e) => setClinicId(e.target.value)}
            >
              <option value="">All clinics</option>
              {clinics.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name}
                </option>
              ))}
            </select>

            <label className="mt-4 block text-sm text-slate-300">Payer</label>
            <select
              className="mt-2 w-full rounded-lg bg-slate-900 p-2"
              value={payerId}
              onChange={(e) => setPayerId(e.target.value)}
            >
              <option value="">All payers</option>
              {payers.map((p) => (
                <option key={p.id} value={p.id}>
                  {p.name}
                </option>
              ))}
            </select>

            <label className="mt-4 block text-sm text-slate-300">Status</label>
            <select
              className="mt-2 w-full rounded-lg bg-slate-900 p-2"
              value={status}
              onChange={(e) => setStatus(e.target.value)}
            >
              <option value="">All</option>
              <option value="Draft">Draft</option>
              <option value="Submitted">Submitted</option>
              <option value="Paid">Paid</option>
              <option value="Denied">Denied</option>
            </select>

            <div className="mt-4 text-xs text-slate-400">
              Date range: {fromUtc.slice(0, 10)} → {toUtc.slice(0, 10)} (UTC)
            </div>
          </div>

          {/* KPI cards */}
          <div className="md:col-span-2 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            <Kpi title="Created" value={summary?.claimsCreated ?? 0} />
            <Kpi title="Submitted" value={summary?.claimsSubmitted ?? 0} />
            <Kpi title="Paid" value={summary?.claimsPaid ?? 0} />
            <Kpi title="Denied" value={summary?.claimsDenied ?? 0} />

            <Kpi
              title="Denial Rate"
              value={`${(((summary?.denialRate ?? 0) as number) * 100).toFixed(
                1
              )}%`}
            />
            <Kpi title="Total Charge" value={money(summary?.totalCharge ?? 0)} />
            <Kpi title="Payer Paid" value={money(summary?.totalPayerPaid ?? 0)} />
            <Kpi
              title="Avg Days to Pay"
              value={
                summary?.avgDaysToPay == null ? "—" : summary.avgDaysToPay.toFixed(1)
              }
            />
          </div>
        </div>

        {/* Claims table */}
        <div className="mt-6 rounded-xl border border-white/10 bg-white/5 p-4">
          <div className="flex items-center justify-between">
            <h2 className="text-xl font-semibold">Recent Claims</h2>
            <div className="text-sm text-slate-300">Showing up to 25</div>
          </div>

          <div className="mt-4 overflow-x-auto">
            <table className="w-full text-left text-sm">
              <thead className="text-slate-300">
                <tr>
                  <th className="py-2">Created</th>
                  <th className="py-2">Claim ID</th>
                  <th className="py-2">Status</th>
                  <th className="py-2">Total Charge</th>
                  <th className="py-2">Actions</th>
                </tr>
              </thead>
              <tbody>
                {claims.map((c) => (
                  <tr key={c.id} className="border-t border-white/10">
                    <td className="py-2">
                      {new Date(c.createdUtc).toISOString().slice(0, 10)}
                    </td>
                    <td className="py-2 font-mono text-xs">{c.id}</td>
                    <td className="py-2">{StatusPill(c.status)}</td>
                    <td className="py-2">{money(c.totalCharge)}</td>
                    <td className="py-2">
                      <button
                        onClick={() => openLedger(c)}
                        className="rounded-md border border-white/10 bg-slate-900 px-3 py-1 hover:bg-slate-800"
                      >
                        View ledger
                      </button>
                    </td>
                  </tr>
                ))}
                {claims.length === 0 && (
                  <tr>
                    <td className="py-6 text-slate-300" colSpan={5}>
                      No claims found for this filter.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>

        {/* Ledger modal */}
        {ledgerOpen && selectedClaim && (
          <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4">
            <div className="w-full max-w-3xl rounded-2xl border border-white/10 bg-slate-950 p-4">
              <div className="flex items-start justify-between gap-4">
                <div>
                  <div className="text-lg font-semibold">Ledger</div>
                  <div className="mt-1 font-mono text-xs text-slate-300">
                    {selectedClaim.id}
                  </div>
                </div>
                <button
                  className="rounded-lg border border-white/10 bg-slate-900 px-3 py-1 hover:bg-slate-800"
                  onClick={() => setLedgerOpen(false)}
                >
                  Close
                </button>
              </div>

              <div className="mt-4 overflow-x-auto">
                <table className="w-full text-left text-sm">
                  <thead className="text-slate-300">
                    <tr>
                      <th className="py-2">Time</th>
                      <th className="py-2">Type</th>
                      <th className="py-2">Amount</th>
                      <th className="py-2">Ref</th>
                    </tr>
                  </thead>
                  <tbody>
                    {ledger.map((t) => (
                      <tr key={t.id} className="border-t border-white/10">
                        <td className="py-2">{new Date(t.createdUtc).toISOString()}</td>
                        <td className="py-2">{t.type}</td>
                        <td className="py-2">{money(t.amount)}</td>
                        <td className="py-2 font-mono text-xs">{t.reference ?? "—"}</td>
                      </tr>
                    ))}
                    {ledger.length === 0 && (
                      <tr>
                        <td className="py-6 text-slate-300" colSpan={4}>
                          Loading ledger...
                        </td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </div>

              <div className="mt-3 text-xs text-slate-400">
                Ledger rows are immutable transactions (Submit/Adjust/Pay/Deny).
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}

function Kpi({ title, value }: { title: string; value: any }) {
  return (
    <div className="rounded-xl border border-white/10 bg-white/5 p-4">
      <div className="text-sm text-slate-300">{title}</div>
      <div className="mt-2 text-2xl font-bold">{value}</div>
    </div>
  );
}

function StatusPill(status: string) {
  const cls =
    status === "Paid"
      ? "bg-emerald-500/15 text-emerald-200 border-emerald-500/30"
      : status === "Denied"
      ? "bg-red-500/15 text-red-200 border-red-500/30"
      : status === "Submitted"
      ? "bg-blue-500/15 text-blue-200 border-blue-500/30"
      : "bg-slate-500/15 text-slate-200 border-slate-500/30";

  return (
    <span
      className={`inline-flex items-center rounded-full border px-2 py-0.5 text-xs ${cls}`}
    >
      {status}
    </span>
  );
}