import React, { useEffect, useState } from "react";
import { VerifyPage } from "./components/VerifyPage";
import { ScanQR } from "./components/ScanQR";
import { DigitalPass } from "./components/DigitalPass";
import { useTelegramUser } from "./hooks/useTelegramUser";
import { AdminDashboard } from "./components/AdminDashboard";
import { EmployeeDashboard } from "./components/EmployeeDashboard";
import { OnboardingForm } from "./components/forms/OnboardingForm";
import { RoleSelect } from "./components/RoleSelect";
import api from "./api";
import * as signalR from "@microsoft/signalr";

type Role = "employee" | "hr";

type RealtimeNotification = {
    requestType?: string;
    requestId?: number;
    status?: string;
    message: string;
    reason?: string | null;
    changedAt?: string;
};

export const App: React.FC = () => {
    const { user, isReady } = useTelegramUser();
    const [employee, setEmployee] = useState<any>(null);
    const [notifications, setNotifications] = useState<RealtimeNotification[]>([]);
    const [errorMsg, setErrorMsg] = useState<string | null>(null);
    const [selectedRole, setSelectedRole] = useState<Role | null>(null);
    const [showPass, setShowPass] = useState(false);

    useEffect(() => {
        if (!user?.id) return;
        api.post("tma/auth", user)
            .then((res) => setEmployee(res.data))
            .catch((err) => {
                console.error(err);
                setErrorMsg(err.message + " | URL: " + err.config?.url);
            });
    }, [user]);

    useEffect(() => {
        if (!user?.id || !employee) return;
        const isAdminParam = employee.isHrAdmin ? "true" : "false";
        const connection = new signalR.HubConnectionBuilder()
            .withUrl(
                `${import.meta.env.VITE_API_URL || ""}/notifications?telegramId=${user.id}&isAdmin=${isAdminParam}`,
            )
            .withAutomaticReconnect()
            .build();

        connection.on("ReceiveNotification", (payload: unknown) => {
            const notification: RealtimeNotification =
                typeof payload === "string"
                    ? { message: payload }
                    : (payload as RealtimeNotification);
            setNotifications((prev) => [notification, ...prev.slice(0, 4)]);
            window.dispatchEvent(new CustomEvent("hrops:status-updated"));
            if (window.Telegram?.WebApp) {
                window.Telegram.WebApp.showAlert(notification.message);
            }
        });

        connection.start().catch(console.error);
        return () => {
            connection.stop();
        };
    }, [user?.id, employee]);

    // ---------- VERIFY PAGE (guard scans QR) ----------
    const verifyToken = new URLSearchParams(window.location.search).get("verify");
    if (verifyToken) return <VerifyPage token={verifyToken} />;

    // ---------- QR SCANNER (guard or security staff) ----------
    const shouldScan = new URLSearchParams(window.location.search).get("scan") === "true";
    if (shouldScan) {
        return (
            <ScanQR
                onTokenFound={(token) => {
                    window.location.href = `/?verify=${encodeURIComponent(token)}`;
                }}
                onClose={() => {
                    window.location.href = "/";
                }}
            />
        );
    }

    // ---------- ERRORS ----------
    if (errorMsg) {
        return (
            <div className="loading-page">
                <div style={{ fontSize: "2.5rem" }}>⚠️</div>
                <div style={{ color: "#dc2626", fontWeight: 700 }}>Ошибка загрузки</div>
                <div style={{ wordBreak: "break-all", fontSize: "0.8rem", color: "var(--text-secondary)", textAlign: "center" }}>
                    {errorMsg}
                </div>
            </div>
        );
    }

    if (!isReady || (user && !employee)) {
        return (
            <div className="loading-page">
                <div className="spinner" />
                <div className="text-subtitle animate-fade-in">Загружаем профиль...</div>
            </div>
        );
    }

    if (!user || !employee) {
        return (
            <div className="loading-page animate-fade-in">
                <div style={{ fontSize: "3rem" }}>🤖</div>
                <h2 style={{ color: "var(--accent-primary)" }}>HROps Bot</h2>
                <p className="text-subtitle" style={{ textAlign: "center" }}>
                    Пожалуйста, откройте приложение внутри Telegram.
                </p>
            </div>
        );
    }

    // ---------- ONBOARDING ----------
    if (!employee.department || employee.department.trim() === "") {
        return (
            <OnboardingForm
                employeeId={employee.id}
                onComplete={() => window.location.reload()}
            />
        );
    }

    // ---------- ROLE SELECTION SCREEN ----------
    if (!selectedRole) {
        return (
            <RoleSelect
                employeeName={employee.nameRu}
                isHrAdmin={employee.isHrAdmin}
                onSelect={(role) => setSelectedRole(role)}
            />
        );
    }

    // ---------- RENDER ----------
    const effectiveRole: Role = selectedRole;
    const isNewEmployee =
        Date.now() - new Date(employee.hiredAt).getTime() < 90 * 24 * 60 * 60 * 1000;

    return (
        <div style={{ paddingBottom: "24px" }}>
            {/* HEADER */}
            <div className="app-header animate-fade-in" style={{ marginBottom: "16px" }}>
                <div className="flex-between" style={{ position: "relative", zIndex: 1 }}>
                    <div>
                        <div style={{ fontSize: "0.75rem", opacity: 0.8, fontWeight: 500, textTransform: "uppercase", letterSpacing: "0.06em" }}>
                            {effectiveRole === "hr" ? "🏢 HR-Панель" : "👤 Сотрудник"}
                        </div>
                        <div style={{ fontWeight: 700, fontSize: "1.15rem", marginTop: "2px" }}>
                            {employee.nameRu}
                        </div>
                        <div style={{ fontSize: "0.82rem", opacity: 0.85, marginTop: "2px" }}>
                            {employee.position} · {employee.department}
                        </div>
                    </div>
                    <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
                        <button
                            id="btn-switch-role"
                            onClick={() => setSelectedRole(null)}
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
                        <button
                            id="btn-show-pass"
                            onClick={() => setShowPass(true)}
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
                            🪪 Пропуск
                        </button>
                        <div className="avatar">{employee.nameRu.charAt(0)}</div>
                    </div>
                </div>
            </div>

            {/* NOTIFICATIONS */}
            {notifications.length > 0 && (
                <div className="animate-fade-in delay-100" style={{ marginBottom: "16px" }}>
                    {notifications.slice(0, 2).map((item, i) => (
                        <div key={i} className="toast success" style={{ marginBottom: "8px" }}>
                            <span>🔔</span>
                            <span>
                                {item.message}
                                {item.reason ? ` Причина: ${item.reason}` : ""}
                            </span>
                            <button
                                onClick={() => setNotifications((prev) => prev.filter((_, idx) => idx !== i))}
                                style={{ marginLeft: "auto", background: "none", border: "none", cursor: "pointer", color: "var(--green-700)" }}
                            >
                                ✕
                            </button>
                        </div>
                    ))}
                </div>
            )}

            {/* DASHBOARD */}
            {effectiveRole === "hr" ? (
                <AdminDashboard />
            ) : (
                <EmployeeDashboard
                    employeeId={employee.id}
                    employeeName={employee.nameRu}
                    isNewEmployee={isNewEmployee}
                />
            )}

            {/* DIGITAL PASS MODAL */}
            {showPass && (
                <DigitalPass
                    employeeId={employee.id}
                    employee={{
                        nameRu: employee.nameRu,
                        department: employee.department,
                        position: employee.position,
                        hiredAt: employee.hiredAt,
                        isHrAdmin: employee.isHrAdmin,
                    }}
                    onClose={() => setShowPass(false)}
                />
            )}
        </div>
    );
};

export default App;
