import React, { useEffect, useRef, useState } from "react";
import { VerifyPage } from "./components/VerifyPage";
import { ScanQR } from "./components/ScanQR";
import { DigitalPass } from "./components/DigitalPass";
import { useTelegramUser } from "./hooks/useTelegramUser";
import { AdminDashboard } from "./components/AdminDashboard";
import { EmployeeDashboard } from "./components/EmployeeDashboard";
import { GuardDashboard } from "./components/GuardDashboard";
import { OnboardingForm } from "./components/forms/OnboardingForm";
import { RoleSelect } from "./components/RoleSelect";
import api from "./api";
import * as signalR from "@microsoft/signalr";

type Role = "employee" | "hr" | "guard";

type RealtimeNotification = {
    requestType?: string;
    requestId?: number;
    status?: string;
    message: string;
    reason?: string | null;
    changedAt?: string;
};

type EmployeeProfile = {
    id: number;
    nameRu: string;
    department?: string | null;
    position?: string | null;
    hiredAt?: string | null;
    isHrAdmin?: boolean;
};

const formatDateRu = (value?: string | null) => {
    if (!value) return "Не указано";
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return "Не указано";
    return date.toLocaleDateString("ru-RU", {
        day: "numeric",
        month: "long",
        year: "numeric",
    });
};

const getAvatarStorageKey = (employeeId: number) => `hrops-avatar-${employeeId}`;

const resizeAvatarFile = (file: File): Promise<string> =>
    new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onerror = () => reject(new Error("Не удалось прочитать файл"));
        reader.onload = () => {
            const img = new Image();
            img.onerror = () => reject(new Error("Не удалось загрузить изображение"));
            img.onload = () => {
                const size = 360;
                const canvas = document.createElement("canvas");
                canvas.width = size;
                canvas.height = size;
                const ctx = canvas.getContext("2d");

                if (!ctx) {
                    reject(new Error("Canvas недоступен"));
                    return;
                }

                const side = Math.min(img.width, img.height);
                const sx = (img.width - side) / 2;
                const sy = (img.height - side) / 2;

                ctx.drawImage(img, sx, sy, side, side, 0, 0, size, size);
                resolve(canvas.toDataURL("image/jpeg", 0.86));
            };
            img.src = String(reader.result);
        };
        reader.readAsDataURL(file);
    });

const EmployeeProfilePage: React.FC<{
    employee: EmployeeProfile;
    avatarUrl: string | null;
    onBack: () => void;
    onShowPass: () => void;
    onAvatarChange: (file: File) => void;
    onRemoveAvatar: () => void;
    onViewAvatar: () => void;
}> = ({ employee, avatarUrl, onBack, onShowPass, onAvatarChange, onRemoveAvatar, onViewAvatar }) => {
    const initial = employee.nameRu?.trim().charAt(0).toUpperCase() || "?";
    const fileInputRef = useRef<HTMLInputElement>(null);

    return (
        <div className="profile-page animate-fade-in delay-100">
            <button className="profile-back-btn" onClick={onBack}>
                ← Назад
            </button>

            <div className="profile-card glass-card no-hover">
                <button
                    type="button"
                    className="profile-avatar profile-avatar-action"
                    onClick={() => {
                        if (avatarUrl) onViewAvatar();
                        else fileInputRef.current?.click();
                    }}
                    title={avatarUrl ? "Посмотреть аватарку" : "Поставить аватарку"}
                    aria-label={avatarUrl ? "Посмотреть аватарку" : "Поставить аватарку"}
                >
                    {avatarUrl ? <img src={avatarUrl} alt="" /> : initial}
                </button>
                <div className="profile-name">{employee.nameRu}</div>
                <div className="profile-role">
                    {employee.position || "Должность не указана"}
                </div>
                <div className="profile-department">
                    {employee.department || "Отдел не указан"}
                </div>

                <input
                    ref={fileInputRef}
                    type="file"
                    accept="image/*"
                    className="profile-file-input"
                    onChange={(event) => {
                        const file = event.target.files?.[0];
                        if (file) onAvatarChange(file);
                        event.currentTarget.value = "";
                    }}
                />

                <div className="profile-avatar-controls">
                    <button
                        type="button"
                        className="btn btn-secondary btn-sm"
                        onClick={() => fileInputRef.current?.click()}
                    >
                        Поставить
                    </button>
                    <button
                        type="button"
                        className="btn btn-ghost btn-sm"
                        onClick={onViewAvatar}
                        disabled={!avatarUrl}
                    >
                        Посмотреть
                    </button>
                    <button
                        type="button"
                        className="btn btn-danger btn-sm"
                        onClick={onRemoveAvatar}
                        disabled={!avatarUrl}
                    >
                        Убрать
                    </button>
                </div>

                <div className="profile-details">
                    <div className="profile-detail-row">
                        <span>ID сотрудника</span>
                        <strong>{employee.id}</strong>
                    </div>
                    <div className="profile-detail-row">
                        <span>Дата найма</span>
                        <strong>{formatDateRu(employee.hiredAt)}</strong>
                    </div>
                    <div className="profile-detail-row">
                        <span>Роль</span>
                        <strong>{employee.isHrAdmin ? "HR-админ" : "Сотрудник"}</strong>
                    </div>
                </div>

                <button className="btn btn-primary btn-full" onClick={onShowPass}>
                    🪪 Открыть пропуск
                </button>
            </div>
        </div>
    );
};

export const App: React.FC = () => {
    const { user, isReady } = useTelegramUser();
    const [employee, setEmployee] = useState<any>(null);
    const [notifications, setNotifications] = useState<RealtimeNotification[]>([]);
    const [errorMsg, setErrorMsg] = useState<string | null>(null);
    const [selectedRole, setSelectedRole] = useState<Role | null>(null);
    const [showPass, setShowPass] = useState(false);
    const [showProfile, setShowProfile] = useState(false);
    const [avatarUrl, setAvatarUrl] = useState<string | null>(null);
    const [showAvatarPreview, setShowAvatarPreview] = useState(false);

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
        if (!employee?.id) {
            setAvatarUrl(null);
            return;
        }
        setAvatarUrl(localStorage.getItem(getAvatarStorageKey(employee.id)));
    }, [employee?.id]);

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

    // ---------- GUARD DASHBOARD ----------
    if (selectedRole === "guard") {
        return (
            <GuardDashboard
                guardName={employee.nameRu}
                onSwitchRole={() => setSelectedRole(null)}
            />
        );
    }

    // ---------- RENDER ----------
    const effectiveRole: Role = selectedRole;
    const isNewEmployee =
        Date.now() - new Date(employee.hiredAt).getTime() < 90 * 24 * 60 * 60 * 1000;
    const openProfile = () => setShowProfile(true);
    const initial = employee.nameRu?.trim().charAt(0).toUpperCase() || "?";

    const handleAvatarChange = (file: File) => {
        if (!file.type.startsWith("image/")) return;

        resizeAvatarFile(file)
            .then((dataUrl) => {
                localStorage.setItem(getAvatarStorageKey(employee.id), dataUrl);
                setAvatarUrl(dataUrl);
            })
            .catch((err) => {
                console.error(err);
                window.Telegram?.WebApp?.showAlert?.("Не удалось поставить аватарку");
            });
    };

    const removeAvatar = () => {
        localStorage.removeItem(getAvatarStorageKey(employee.id));
        setAvatarUrl(null);
        setShowAvatarPreview(false);
    };

    return (
        <div style={{ paddingBottom: "24px" }}>
            {/* HEADER */}
            <div className="app-header animate-fade-in" style={{ marginBottom: "16px" }}>
                <div className="flex-between" style={{ position: "relative", zIndex: 1 }}>
                    <div style={{ minWidth: 0 }}>
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
                    <div style={{ display: "flex", alignItems: "center", gap: 8, position: "relative", zIndex: 2 }}>
                        <button
                            id="btn-switch-role"
                            onClick={() => {
                                setShowProfile(false);
                                setSelectedRole(null);
                            }}
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
                        <button
                            id="btn-open-profile"
                            type="button"
                            className="avatar avatar-button"
                            onClick={openProfile}
                            onPointerUp={openProfile}
                            title="Открыть профиль"
                            aria-label="Открыть профиль"
                        >
                            {avatarUrl ? <img src={avatarUrl} alt="" /> : initial}
                        </button>
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
            {showProfile ? (
                <EmployeeProfilePage
                    employee={employee}
                    avatarUrl={avatarUrl}
                    onBack={() => setShowProfile(false)}
                    onShowPass={() => setShowPass(true)}
                    onAvatarChange={handleAvatarChange}
                    onRemoveAvatar={removeAvatar}
                    onViewAvatar={() => {
                        if (avatarUrl) setShowAvatarPreview(true);
                    }}
                />
            ) : effectiveRole === "hr" ? (
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

            {showAvatarPreview && avatarUrl && (
                <div className="modal-overlay" onClick={() => setShowAvatarPreview(false)}>
                    <div className="avatar-preview-modal" onClick={(event) => event.stopPropagation()}>
                        <button
                            type="button"
                            className="avatar-preview-close"
                            onClick={() => setShowAvatarPreview(false)}
                            aria-label="Закрыть просмотр аватарки"
                        >
                            ✕
                        </button>
                        <img src={avatarUrl} alt="Аватарка сотрудника" />
                    </div>
                </div>
            )}
        </div>
    );
};

export default App;
