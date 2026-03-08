import { useEffect, useState } from "react";
import { getProviders } from "./api/providers";
import type { ProviderSummaryDto } from "./api/types";

export default function App() {
    const [providers, setProviders] = useState<ProviderSummaryDto[]>([]);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        getProviders()
            .then(setProviders)
            .catch((e) => setError(String(e)));
    }, []);

    return (
        <div className="min-h-screen bg-slate-950 text-slate-100 p-8">
            <h1 className="text-3xl font-bold">Patient Access — Providers</h1>

            {error && (
                <div className="mt-4 rounded border border-red-500/40 bg-red-500/10 p-3 text-red-200">
                    {error}
                </div>
            )}

            <div className="mt-6 grid gap-4 md:grid-cols-2 lg:grid-cols-3">
                {providers.map((p) => (
                    <div key={p.id} className="rounded-xl border border-white/10 bg-white/5 p-4">
                        <div className="text-lg font-semibold">{p.name}</div>
                        <div className="text-sm text-slate-300">{p.specialty}</div>
                        <div className="mt-2 text-xs text-slate-400">{p.id}</div>
                    </div>
                ))}
            </div>
        </div>
    );
}