import React, { useEffect, useState } from "react";

const Confetti: React.FC<{ count?: number }> = ({ count = 50 }) => {
    const pieces = Array.from({ length: count }).map((_, i) => ({
        id: i,
        left: Math.random() * 100,
        delay: Math.random() * 0.3,
        duration: 2 + Math.random() * 1,
        size: 4 + Math.random() * 8,
    }));
    return (
        <>
            {pieces.map(p => (
                <div
                    key={p.id}
                    style={{
                        position: "fixed",
                        left: `${p.left}%`,
                        top: "-20px",
                        width: p.size,
                        height: p.size,
                        background: ["#22c55e", "#4ade80", "#86efac"][Math.floor(Math.random() * 3)],
                        borderRadius: "50%",
                        pointerEvents: "none",
                        animation: `confetti-fall ${p.duration}s linear ${p.delay}s forwards`,
                        boxShadow: `0 0 ${p.size}px currentColor`,
                    }}
                />
            ))}
            <style>{`
                @keyframes confetti-fall {
                    to {
                        transform: translateY(100vh) rotate(720deg);
                        opacity: 0;
                    }
                }
            `}</style>
        </>
    );
};

interface VerifyPageProps {
    token: string;
}

interface EmployeeData {
    id: number;
    nameRu: string;
    nameKk: string;
    department: string;
    position: string;
    hiredAt: string;
    isHrAdmin: boolean;
}

interface VerifyResult {
    valid: boolean;
    verifiedAt?: string;
    employee?: EmployeeData;
    error?: string;
}

export const VerifyPage: React.FC<VerifyPageProps> = ({ token }) => {
    const [result, setResult] = useState<VerifyResult | null>(null);
    const [loading, setLoading] = useState(true);

    const apiBase = (import.meta.env.VITE_API_URL as string) || "";

    useEffect(() => {
        const verify = async () => {
            try {
                const res = await fetch(
                    `${apiBase}/api/pass/verify?t=${encodeURIComponent(token)}`,
                    { headers: { Accept: "application/json" } }
                );
                const data: VerifyResult = await res.json();
                setResult(data);
            } catch {
                setResult({ valid: false, error: "Нет связи с сервером" });
            } finally {
                setLoading(false);
            }
        };
        verify();
    }, [token, apiBase]);

    if (loading) {
        return (
            <div style={{
                minHeight: "100dvh", display: "flex", flexDirection: "column",
                alignItems: "center", justifyContent: "center",
                background: "#0f172a", color: "#fff",
            }}>
                <div style={{
                    width: 56, height: 56,
                    border: "4px solid rgba(255,255,255,0.2)",
                    borderTopColor: "#3b82f6",
                    borderRadius: "50%",
                    animation: "spin 0.9s linear infinite",
                    marginBottom: 16,
                }} />
                <div style={{ fontSize: "1rem", color: "rgba(255,255,255,0.7)" }}>
                    Проверяем пропуск…
                </div>
                <style>{`@keyframes spin { to { transform: rotate(360deg); } }`}</style>
            </div>
        );
    }

    const { valid, employee, error, verifiedAt } = result!;

    if (!valid) {
        return (
            <div style={{
                minHeight: "100dvh",
                background: "linear-gradient(160deg, #7f1d1d 0%, #450a0a 60%, #0f172a 100%)",
                display: "flex", flexDirection: "column",
                alignItems: "center", justifyContent: "center",
                padding: 24, textAlign: "center", position: "relative",
            }}>
                <style>{`@keyframes pulse-red { 0%, 100% { box-shadow: 0 0 0 0 rgba(239,68,68,0.7); } 50% { box-shadow: 0 0 0 30px rgba(239,68,68,0); } }`}</style>
                <div style={{
                    width: 140, height: 140, borderRadius: "50%",
                    background: "rgba(239,68,68,0.2)",
                    border: "4px solid rgba(239,68,68,0.8)",
                    display: "flex", alignItems: "center", justifyContent: "center",
                    fontSize: "4rem", marginBottom: 32,
                    animation: "pulse-red 1.5s infinite",
                }}>
                    🚫
                </div>

                <div style={{
                    background: "#ef4444",
                    color: "#fff",
                    fontWeight: 900,
                    fontSize: "2.2rem",
                    letterSpacing: "0.08em",
                    borderRadius: 16,
                    padding: "14px 48px",
                    marginBottom: 24,
                    boxShadow: "0 10px 40px rgba(239,68,68,0.6)",
                    textTransform: "uppercase",
                }}>
                    ДОСТУП ЗАПРЕЩЁН
                </div>

                <div style={{
                    background: "rgba(255,255,255,0.12)",
                    borderRadius: 16,
                    padding: "18px 32px",
                    color: "rgba(255,255,255,0.9)",
                    fontSize: "1.05rem",
                    maxWidth: 380,
                    backdropFilter: "blur(20px)",
                    border: "1px solid rgba(255,255,255,0.2)",
                    lineHeight: 1.6,
                }}>
                    {error || "Пропуск недействителен"}
                </div>

                <div style={{ marginTop: 48, fontSize: "0.75rem", color: "rgba(255,255,255,0.3)" }}>
                    HROps Security System · {new Date().toLocaleString("ru-RU")}
                </div>
            </div>
        );
    }

    const emp = employee!;
    const initials = emp.nameRu.split(" ").slice(0, 2).map(s => s[0]).join("");
    const hiredDate = new Date(emp.hiredAt).toLocaleDateString("ru-RU", { day: "2-digit", month: "long", year: "numeric" });
    const verifiedAtFmt = verifiedAt
        ? new Date(verifiedAt).toLocaleTimeString("ru-RU", { hour: "2-digit", minute: "2-digit", second: "2-digit" })
        : new Date().toLocaleTimeString("ru-RU");

    return (
        <div style={{
            minHeight: "100dvh",
            background: "linear-gradient(160deg, #0f4c81 0%, #0a3660 40%, #051e3e 100%)",
            padding: "24px 16px",
            color: "#fff",
            overflow: "hidden",
            position: "relative",
        }}>
            <style>{`
                @keyframes slide-up { from { opacity:0; transform:translateY(60px); } to { opacity:1; transform:none; } }
                @keyframes scan-line { 0% { top: -100%; opacity: 1; } 100% { top: 100%; opacity: 0; } }
                @keyframes glow-pulse { 0%, 100% { box-shadow: 0 0 20px rgba(34,197,94,0.4), inset 0 0 20px rgba(34,197,94,0.1); } 50% { box-shadow: 0 0 40px rgba(34,197,94,0.8), inset 0 0 40px rgba(34,197,94,0.2); } }
                @keyframes spin { to { transform: rotate(360deg); } }
            `}</style>

            {/* Confetti on success */}
            <Confetti count={60} />

            {/* Top badge */}
            <div style={{
                textAlign: "center",
                marginBottom: 32,
                animation: "slide-up 0.6s cubic-bezier(0.34, 1.56, 0.64, 1)",
            }}>
                <div style={{
                    display: "inline-block",
                    background: "linear-gradient(135deg, #22c55e, #16a34a)",
                    color: "#fff",
                    fontWeight: 900,
                    fontSize: "1.8rem",
                    letterSpacing: "0.1em",
                    borderRadius: 16,
                    padding: "12px 48px",
                    boxShadow: "0 15px 50px rgba(34,197,94,0.5), inset 0 1px 0 rgba(255,255,255,0.2)",
                    textTransform: "uppercase",
                }}>
                    ✅ ДОСТУП РАЗРЕШЁН
                </div>
            </div>

            {/* Main card */}
            <div style={{
                maxWidth: 540,
                margin: "0 auto",
                animation: "slide-up 0.7s cubic-bezier(0.34, 1.56, 0.64, 1) 0.1s both",
            }}>
                {/* Big avatar section */}
                <div style={{
                    background: "linear-gradient(135deg, rgba(34,197,94,0.2), rgba(16,185,129,0.1))",
                    borderRadius: 24,
                    padding: "32px 24px",
                    marginBottom: 20,
                    border: "2px solid rgba(34,197,94,0.3)",
                    textAlign: "center",
                    position: "relative",
                    overflow: "hidden",
                    animation: "glow-pulse 3s ease-in-out infinite",
                }}>
                    {/* Scan effect lines */}
                    <div style={{
                        position: "absolute",
                        inset: 0,
                        background: "linear-gradient(90deg, transparent, rgba(34,197,94,0.3), transparent)",
                        animation: "scan-line 2s ease-in infinite",
                    }} />

                    {/* Large avatar */}
                    <div style={{
                        width: 120,
                        height: 120,
                        borderRadius: "50%",
                        background: "linear-gradient(135deg, #22c55e, #16a34a)",
                        display: "flex",
                        alignItems: "center",
                        justifyContent: "center",
                        fontSize: "2.2rem",
                        fontWeight: 700,
                        color: "#fff",
                        margin: "0 auto 20px",
                        flexShrink: 0,
                        boxShadow: "0 8px 32px rgba(34,197,94,0.5), inset 0 1px 0 rgba(255,255,255,0.3)",
                        border: "3px solid rgba(255,255,255,0.2)",
                        position: "relative",
                        zIndex: 1,
                    }}>
                        {initials}
                    </div>

                    {/* Name section */}
                    <div style={{ position: "relative", zIndex: 1 }}>
                        <div style={{ fontSize: "1.5rem", fontWeight: 800, color: "#fff", marginBottom: 4 }}>
                            {emp.nameRu}
                        </div>
                        {emp.nameKk && emp.nameKk !== emp.nameRu && (
                            <div style={{ fontSize: "0.95rem", color: "rgba(255,255,255,0.7)", marginBottom: 12 }}>
                                {emp.nameKk}
                            </div>
                        )}
                        <div style={{
                            fontSize: "1.1rem",
                            fontWeight: 600,
                            color: "#86efac",
                            marginBottom: 8,
                        }}>
                            {emp.position}
                        </div>
                        <div style={{ fontSize: "0.92rem", color: "rgba(255,255,255,0.75)" }}>
                            🏢 {emp.department}
                        </div>
                    </div>

                    {emp.isHrAdmin && (
                        <div style={{
                            marginTop: 16,
                            display: "inline-block",
                            background: "rgba(234,179,8,0.2)",
                            border: "1px solid rgba(234,179,8,0.6)",
                            color: "#fbbf24",
                            fontSize: "0.75rem",
                            fontWeight: 700,
                            borderRadius: 10,
                            padding: "6px 14px",
                            textTransform: "uppercase",
                            letterSpacing: "0.1em",
                            position: "relative",
                            zIndex: 1,
                        }}>
                            👑 HR АДМИНИСТРАТОР
                        </div>
                    )}
                </div>

                {/* Info grid */}
                <div style={{
                    display: "grid",
                    gridTemplateColumns: "1fr 1fr",
                    gap: 12,
                    marginBottom: 20,
                }}>
                    <InfoCard icon="📅" label="В компании с" value={hiredDate} />
                    <InfoCard icon="🆔" label="ID" value={`#${emp.id}`} />
                </div>

                {/* Scan button for next pass */}
                <button
                    onClick={() => {
                        // Закроем текущую проверку и вернемся на главную с возможностью отсканировать новый QR
                        window.location.href = "/?scan=true";
                    }}
                    style={{
                        width: "100%",
                        background: "linear-gradient(135deg, #3b82f6, #2563eb)",
                        border: "none",
                        color: "#fff",
                        borderRadius: 16,
                        padding: "14px 20px",
                        fontSize: "0.95rem",
                        fontWeight: 700,
                        cursor: "pointer",
                        boxShadow: "0 4px 20px rgba(59,130,246,0.4)",
                        transition: "all 0.3s",
                    }}
                    onMouseOver={(e) => {
                        e.currentTarget.style.boxShadow = "0 8px 30px rgba(59,130,246,0.6)";
                        e.currentTarget.style.transform = "translateY(-2px)";
                    }}
                    onMouseOut={(e) => {
                        e.currentTarget.style.boxShadow = "0 4px 20px rgba(59,130,246,0.4)";
                        e.currentTarget.style.transform = "none";
                    }}
                >
                    📱 Отсканировать следующий пропуск
                </button>
            </div>

            {/* Bottom bar */}
            <div style={{
                position: "fixed",
                bottom: 0,
                left: 0,
                right: 0,
                background: "linear-gradient(to top, rgba(15,76,129,0.8), transparent)",
                padding: "24px 20px 20px",
                textAlign: "center",
                fontSize: "0.7rem",
                color: "rgba(255,255,255,0.3)",
            }}>
                🛡️ HROps Security · Защищённый контроль доступа
            </div>
        </div>
    );
};

/* ──────────────────────────── Helpers ────────────────────────────── */

const InfoCard: React.FC<{ icon: string; label: string; value: string; accent?: string }> = ({ icon, label, value }) => (
    <div style={{
        background: "rgba(255,255,255,0.08)",
        borderRadius: 12,
        padding: "12px 14px",
        border: "1px solid rgba(255,255,255,0.2)",
        textAlign: "center",
    }}>
        <div style={{ fontSize: "1.3rem", marginBottom: 4 }}>{icon}</div>
        <div style={{ fontSize: "0.65rem", color: "rgba(255,255,255,0.4)", textTransform: "uppercase", letterSpacing: "0.08em", marginBottom: 4 }}>
            {label}
        </div>
        <div style={{ fontSize: "0.9rem", color: "rgba(255,255,255,0.9)", fontWeight: 600 }}>
            {value}
        </div>
    </div>
);
