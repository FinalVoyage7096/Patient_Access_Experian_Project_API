import { Link, Route, Routes } from "react-router-dom";
import BookPage from "./BookPage";
import DashboardPage from "./DashboardPage";

export default function App() {
    return (
        <div className="min-h-screen bg-slate-950 text-slate-100">
            <nav className="border-b border-white/10 bg-black/20">
                <div className="mx-auto max-w-6xl px-8 py-3 flex items-center gap-4">
                    <div className="font-bold">Patient Access</div>
                    <Link className="text-slate-300 hover:text-white" to="/book">Book</Link>
                    <Link className="text-slate-300 hover:text-white" to="/dashboard">Dashboard</Link>
                </div>
            </nav>

            <Routes>
                <Route path="/" element={<BookPage />} />
                <Route path="/book" element={<BookPage />} />
                <Route path="/dashboard" element={<DashboardPage />} />
            </Routes>
        </div>
    );
}