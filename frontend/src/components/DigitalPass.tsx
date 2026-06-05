import React, { useEffect, useState, useCallback } from "react";
import api from "../api";

interface DigitalPassProps {
    employeeId: number;
    employee: {
        nameRu: string;
        department: string;
        position: string;
        hiredAt: string;
        isHrAdmin: boolean;
    };
    onClose: () => void;
}

interface PassData {
    token: string;
    expiresAt: number; // unix ms
    employee: { nameRu: string; department: string; position: string };
}

export const DigitalPass: React.FC<DigitalPassProps> = ({ employeeId, employee, onClose }) => {
    const [passData, setPassData] = useState<PassData | null>(null);
    const [loading, setLoading] = useState(true);
    const [secondsLeft, setSecondsLeft] = useState(300);
    const [qrError, setQrError] = useState(false);

    const loadPass = useCallback(async () => {
        setLoading(true);
        setQrError(false);
        try {
            const res = await api.get(`/pass/generate/${employeeId}`);
            setPassData(res.data);
            const left = Math.round((res.data.expiresAt - Date.now()) / 1000);
            setSecondsLeft(Math.max(0, left));
        } catch {
            setQrError(true);
        } finally {
            setLoading(false);
        }
    }, [employeeId]);

    useEffect(() => {
        loadPass();
    }, [loadPass]);

    // Countdown timer
    useEffect(() => {
        if (!passData) return;
        const interval = setInterval(() => {
            const left = Math.round((passData.expiresAt - Date.now()) / 1000);
            if (left <= 0) {
                setSecondsLeft(0);
                loadPass(); // auto-refresh
            } else {
                setSecondsLeft(left);
            }
        }, 1000);
        return () => clearInterval(interval);
    }, [passData, loadPass]);

    const verifyUrl = passData
        ? `${window.location.origin}/?verify=${passData.token}`
        : "";

    const qrUrl = verifyUrl
        ? `https://api.qrserver.com/v1/create-qr-code/?size=200x200&qzone=2&color=1a1a2e&bgcolor=ffffff&data=${encodeURIComponent(verifyUrl)}`
        : "";

    const formatSeconds = (s: number) => {
        const m = Math.floor(s / 60);
        const sec = s % 60;
        return `${m}:${sec.toString().padStart(2, "0")}`;
    };

    const pct = (secondsLeft / 300) * 100;
    const timerColor = secondsLeft > 120 ? "#22c55e" : secondsLeft > 30 ? "#f59e0b" : "#ef4444";

    const hiredYear = new Date(employee.hiredAt).getFullYear();

    return (
        <div
            style={{
                position: "fixed",
                inset: 0,
                background: "rgba(0,0,0,0.75)",
                zIndex: 1000,
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
                padding: "16px",
            }}
            onClick={onClose}
        >
            <div
                style={{ maxWidth: 360, width: "100%" }}
                onClick={(e) => e.stopPropagation()}
            >
                {/* === CARD === */}
                <div
                    style={{
                        borderRadius: "20px",
                        overflow: "hidden",
                        boxShadow: "0 25px 60px rgba(0,0,0,0.5)",
                        background: "#0f172a",
                        position: "relative",
                    }}
                >
                    {/* Header gradient */}
                    <div
                        style={{
                            background: "linear-gradient(135deg, #1e3a5f 0%, #0f4c81 50%, #1a1a3e 100%)",
                            padding: "20px 20px 16px",
                            position: "relative",
                            overflow: "hidden",
                        }}
                    >
                        {/* Decorative circles */}
                        <div style={{
                            position: "absolute", right: -30, top: -30,
                            width: 120, height: 120, borderRadius: "50%",
                            background: "rgba(255,255,255,0.05)"
                        }} />
                        <div style={{
                            position: "absolute", right: 10, top: 10,
                            width: 60, height: 60, borderRadius: "50%",
                            background: "rgba(255,255,255,0.07)"
                        }} />

                        <div style={{ display: "flex", alignItems: "center", gap: 12, position: "relative" }}>
                            {/* Avatar */}
                            <div style={{
                                width: 52, height: 52, borderRadius: "50%",
                                background: "linear-gradient(135deg, #3b82f6, #8b5cf6)",
                                display: "flex", alignItems: "center", justifyContent: "center",
                                fontSize: "1.4rem", fontWeight: 700, color: "#fff",
                                boxShadow: "0 4px 12px rgba(59,130,246,0.4)",
                                flexShrink: 0,
                            }}>
                                {employee.nameRu.charAt(0)}
                            </div>
                            <div>
                                <div style={{ fontSize: "0.6rem", color: "rgba(255,255,255,0.6)", textTransform: "uppercase", letterSpacing: "0.1em" }}>
                                    HROps · Цифровой пропуск
                                </div>
                                <div style={{ fontWeight: 700, fontSize: "1rem", color: "#fff", marginTop: 2 }}>
                                    {employee.nameRu}
                                </div>
                                <div style={{ fontSize: "0.78rem", color: "rgba(255,255,255,0.75)", marginTop: 1 }}>
                                    {employee.position}
                                </div>
                            </div>
                            {employee.isHrAdmin && (
                                <div style={{
                                    marginLeft: "auto",
                                    background: "rgba(234,179,8,0.2)",
                                    border: "1px solid rgba(234,179,8,0.5)",
                                    color: "#fbbf24",
                                    fontSize: "0.65rem",
                                    fontWeight: 600,
                                    borderRadius: 6,
                                    padding: "3px 8px",
                                    flexShrink: 0,
                                }}>
                                    HR ADMIN
                                </div>
                            )}
                        </div>

                        {/* Department / hire year */}
                        <div style={{
                            marginTop: 12, display: "flex", gap: 8, position: "relative"
                        }}>
                            <div style={{
                                background: "rgba(255,255,255,0.1)",
                                borderRadius: 8, padding: "4px 10px",
                                fontSize: "0.72rem", color: "rgba(255,255,255,0.8)",
                            }}>
                                🏢 {employee.department}
                            </div>
                            <div style={{
                                background: "rgba(255,255,255,0.1)",
                                borderRadius: 8, padding: "4px 10px",
                                fontSize: "0.72rem", color: "rgba(255,255,255,0.8)",
                            }}>
                                📅 с {hiredYear} г.
                            </div>
                        </div>
                    </div>

                    {/* QR section */}
                    <div style={{
                        background: "#fff",
                        margin: "0 20px",
                        borderRadius: 16,
                        padding: "16px",
                        marginTop: -8,
                        textAlign: "center",
                        position: "relative",
                        boxShadow: "0 4px 20px rgba(0,0,0,0.2)",
                    }}>
                        {loading ? (
                            <div style={{ height: 200, display: "flex", alignItems: "center", justifyContent: "center" }}>
                                <div style={{ width: 32, height: 32, border: "3px solid #e2e8f0", borderTopColor: "#3b82f6", borderRadius: "50%", animation: "spin 0.8s linear infinite" }} />
                            </div>
                        ) : qrError ? (
                            <div style={{ height: 200, display: "flex", flexDirection: "column", alignItems: "center", justifyContent: "center", gap: 8 }}>
                                <div style={{ fontSize: "2rem" }}>⚠️</div>
                                <div style={{ fontSize: "0.8rem", color: "#ef4444" }}>Ошибка генерации</div>
                                <button onClick={loadPass} style={{ fontSize: "0.75rem", color: "#3b82f6", background: "none", border: "none", cursor: "pointer", textDecoration: "underline" }}>
                                    Повторить
                                </button>
                            </div>
                        ) : (
                            <>
                                <img
                                    src={qrUrl}
                                    alt="QR пропуск"
                                    width={180}
                                    height={180}
                                    style={{ display: "block", margin: "0 auto" }}
                                />
                                <div style={{ marginTop: 8, fontSize: "0.65rem", color: "#94a3b8" }}>
                                    Покажите охраннику для сканирования
                                </div>
                            </>
                        )}

                        {/* Timer bar */}
                        {!loading && !qrError && (
                            <div style={{ marginTop: 10 }}>
                                <div style={{
                                    height: 4, background: "#e2e8f0", borderRadius: 2, overflow: "hidden"
                                }}>
                                    <div style={{
                                        height: "100%", width: `${pct}%`,
                                        background: timerColor,
                                        borderRadius: 2,
                                        transition: "width 1s linear, background 0.3s"
                                    }} />
                                </div>
                                <div style={{
                                    marginTop: 4, fontSize: "0.7rem",
                                    color: timerColor, fontWeight: 600, fontVariantNumeric: "tabular-nums"
                                }}>
                                    {secondsLeft > 0
                                        ? `⏱ QR действителен ещё ${formatSeconds(secondsLeft)}`
                                        : "🔄 Обновление..."}
                                </div>
                            </div>
                        )}
                    </div>

                    {/* Footer */}
                    <div style={{ padding: "12px 20px 16px", display: "flex", alignItems: "center", gap: 8 }}>
                        <div style={{ flex: 1 }}>
                            <div style={{ fontSize: "0.6rem", color: "rgba(255,255,255,0.4)", textTransform: "uppercase", letterSpacing: "0.08em" }}>
                                Зоны доступа
                            </div>
                            <div style={{ marginTop: 4, display: "flex", gap: 4, flexWrap: "wrap" }}>
                                {["🚪 Главный вход", "🏢 Офис"].map(z => (
                                    <span key={z} style={{
                                        fontSize: "0.65rem", color: "rgba(255,255,255,0.7)",
                                        background: "rgba(255,255,255,0.08)",
                                        borderRadius: 4, padding: "2px 6px"
                                    }}>{z}</span>
                                ))}
                            </div>
                        </div>
                        <button
                            onClick={loadPass}
                            disabled={loading}
                            style={{
                                background: "rgba(59,130,246,0.2)",
                                border: "1px solid rgba(59,130,246,0.4)",
                                color: "#60a5fa", borderRadius: 10, padding: "6px 12px",
                                fontSize: "0.72rem", cursor: "pointer", fontWeight: 600,
                            }}
                        >
                            🔄 Обновить
                        </button>
                    </div>
                </div>

                {/* Close */}
                <button
                    onClick={onClose}
                    style={{
                        display: "block", width: "100%", marginTop: 12,
                        background: "rgba(255,255,255,0.1)",
                        border: "none", color: "rgba(255,255,255,0.7)",
                        borderRadius: 12, padding: "12px",
                        fontSize: "0.9rem", cursor: "pointer",
                        backdropFilter: "blur(10px)",
                    }}
                >
                    Закрыть
                </button>
            </div>
        </div>
    );
};
