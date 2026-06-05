import React, { useState } from "react";
import { ScanQR } from "./ScanQR";
import { VerifyPage } from "./VerifyPage";

interface GuardDashboardProps {
    guardName: string;
    onSwitchRole: () => void;
}

export const GuardDashboard: React.FC<GuardDashboardProps> = ({ guardName, onSwitchRole }) => {
    const [scanning, setScanning] = useState(false);
    const [verifyToken, setVerifyToken] = useState<string | null>(null);

    if (verifyToken) {
        return (
            <VerifyPage
                token={verifyToken}
                onClose={() => {
                    setVerifyToken(null);
                    setScanning(true);
                }}
            />
        );
    }

    if (scanning) {
        return (
            <ScanQR
                onTokenFound={(raw) => {
                    // QR может содержать полный URL вида https://.../?verify=TOKEN
                    // или просто сырой токен — извлекаем нужную часть
                    let token = raw;
                    try {
                        const url = new URL(raw);
                        const fromParam = url.searchParams.get("verify");
                        if (fromParam) token = fromParam;
                    } catch {
                        // не URL — используем как есть
                    }
                    setScanning(false);
                    setVerifyToken(token);
                }}
                onClose={() => setScanning(false)}
            />
        );
    }

    const now = new Date();
    const timeStr = now.toLocaleTimeString("ru-RU", { hour: "2-digit", minute: "2-digit" });
    const dateStr = now.toLocaleDateString("ru-RU", { weekday: "long", day: "numeric", month: "long" });

    return (
        <div style={{ paddingBottom: "24px" }}>
            {/* HEADER */}
            <div className="app-header animate-fade-in" style={{ marginBottom: "16px", background: "linear-gradient(135deg, #1e3a5f 0%, #2563eb 60%, #3b82f6 100%)" }}>
                <div className="flex-between" style={{ position: "relative", zIndex: 1 }}>
                    <div>
                        <div style={{ fontSize: "0.75rem", opacity: 0.8, fontWeight: 500, textTransform: "uppercase", letterSpacing: "0.06em" }}>
                            🛡️ Охранник
                        </div>
                        <div style={{ fontWeight: 700, fontSize: "1.15rem", marginTop: "2px" }}>
                            {guardName}
                        </div>
                        <div style={{ fontSize: "0.82rem", opacity: 0.85, marginTop: "2px" }}>
                            Пост контроля доступа
                        </div>
                    </div>
                    <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
                        <button
                            id="guard-btn-switch-role"
                            onClick={onSwitchRole}
                            title="Сменить роль"
                            style={{
                                background: "rgba(255,255,255,0.18)",
                                border: "1px solid rgba(255,255,255,0.35)",
                                color: "#fff",
                                borderRadius: 12,
                                padding: "7px 14px",
                                fontSize: "0.82rem",
                                fontWeight: 600,
                                cursor: "pointer",
                                display: "flex",
                                alignItems: "center",
                                gap: 6,
                            }}
                        >
                            ⇄ Роль
                        </button>
                        <div className="avatar" style={{ background: "rgba(255,255,255,0.25)", border: "2px solid rgba(255,255,255,0.5)" }}>
                            {guardName.charAt(0)}
                        </div>
                    </div>
                </div>
            </div>

            {/* STATUS CARD */}
            <div className="glass-card no-hover animate-fade-in" style={{ marginBottom: "16px", textAlign: "center", padding: "28px 20px" }}>
                <div style={{ fontSize: "0.75rem", fontWeight: 700, textTransform: "uppercase", letterSpacing: "0.08em", color: "var(--text-muted)", marginBottom: "12px" }}>
                    Текущее время
                </div>
                <div style={{ fontFamily: "'Outfit', sans-serif", fontSize: "3.2rem", fontWeight: 800, color: "#2563eb", letterSpacing: "-0.03em", lineHeight: 1 }}>
                    {timeStr}
                </div>
                <div style={{ fontSize: "0.85rem", color: "var(--text-secondary)", marginTop: "8px", textTransform: "capitalize" }}>
                    {dateStr}
                </div>

                <div style={{ marginTop: "20px", display: "flex", alignItems: "center", justifyContent: "center", gap: 8 }}>
                    <div style={{ width: 10, height: 10, borderRadius: "50%", background: "#22c55e", boxShadow: "0 0 8px #22c55e", animation: "pulse 2s infinite" }} />
                    <span style={{ fontSize: "0.82rem", color: "var(--text-secondary)", fontWeight: 600 }}>
                        Пост активен
                    </span>
                </div>
            </div>

            {/* MAIN SCAN BUTTON */}
            <button
                id="guard-btn-scan"
                onClick={() => setScanning(true)}
                style={{
                    width: "100%",
                    background: "linear-gradient(135deg, #1d4ed8 0%, #2563eb 50%, #3b82f6 100%)",
                    border: "none",
                    borderRadius: "20px",
                    padding: "28px 20px",
                    cursor: "pointer",
                    color: "#fff",
                    display: "flex",
                    flexDirection: "column",
                    alignItems: "center",
                    gap: "12px",
                    boxShadow: "0 8px 32px rgba(37,99,235,0.4)",
                    transition: "all 0.25s cubic-bezier(0.4, 0, 0.2, 1)",
                    marginBottom: "16px",
                    position: "relative",
                    overflow: "hidden",
                }}
                onMouseEnter={e => {
                    (e.currentTarget as HTMLButtonElement).style.transform = "translateY(-3px)";
                    (e.currentTarget as HTMLButtonElement).style.boxShadow = "0 12px 40px rgba(37,99,235,0.55)";
                }}
                onMouseLeave={e => {
                    (e.currentTarget as HTMLButtonElement).style.transform = "translateY(0)";
                    (e.currentTarget as HTMLButtonElement).style.boxShadow = "0 8px 32px rgba(37,99,235,0.4)";
                }}
                onMouseDown={e => (e.currentTarget as HTMLButtonElement).style.transform = "scale(0.97)"}
                onMouseUp={e => (e.currentTarget as HTMLButtonElement).style.transform = "translateY(-3px)"}
            >
                {/* Decorative glow */}
                <div style={{
                    position: "absolute", top: "-40px", right: "-40px",
                    width: "140px", height: "140px",
                    background: "rgba(255,255,255,0.07)", borderRadius: "50%",
                }} />
                <div style={{ fontSize: "3.5rem", filter: "drop-shadow(0 4px 12px rgba(0,0,0,0.2))" }}>📱</div>
                <div style={{ fontFamily: "'Outfit', sans-serif", fontSize: "1.4rem", fontWeight: 800, letterSpacing: "-0.02em" }}>
                    Сканировать пропуск
                </div>
                <div style={{ fontSize: "0.85rem", opacity: 0.8 }}>
                    Нажмите для открытия камеры
                </div>
            </button>

            {/* INSTRUCTIONS */}
            <div className="glass-card no-hover animate-fade-in" style={{ padding: "16px 20px" }}>
                <div className="section-label" style={{ color: "#2563eb", marginBottom: "12px" }}>📋 Инструкция</div>
                <div style={{ display: "flex", flexDirection: "column", gap: "10px" }}>
                    {[
                        { icon: "1️⃣", text: "Нажмите кнопку «Сканировать пропуск»" },
                        { icon: "2️⃣", text: "Разрешите доступ к камере" },
                        { icon: "3️⃣", text: "Направьте камеру на QR-код сотрудника" },
                        { icon: "4️⃣", text: "Дождитесь результата проверки" },
                    ].map((step, i) => (
                        <div key={i} style={{ display: "flex", alignItems: "center", gap: "10px" }}>
                            <span style={{ fontSize: "1.1rem", flexShrink: 0 }}>{step.icon}</span>
                            <span style={{ fontSize: "0.85rem", color: "var(--text-secondary)" }}>{step.text}</span>
                        </div>
                    ))}
                </div>
            </div>
        </div>
    );
};
