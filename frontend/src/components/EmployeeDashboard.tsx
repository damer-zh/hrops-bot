import React, { useEffect, useState, useCallback } from "react";
import api from "../api";
import { OnboardingChecklist } from "./OnboardingChecklist";
import { ServiceActions } from "./ServiceActions";

interface EmployeeDashboardProps {
    employeeId: number;
    employeeName: string;
    isNewEmployee: boolean;
}

const IT_TYPE_LABEL: Record<number, string> = {
    1: "Система",
    2: "Папка",
    3: "Группа",
    4: "Почта",
    5: "VPN",
    6: "Прочее",
};
const IT_STATUS_BADGE: Record<number, { cls: string; label: string }> = {
    1: { cls: "warning", label: "В очереди" },
    2: { cls: "info", label: "В работе" },
    3: { cls: "success", label: "Готово" },
    4: { cls: "danger", label: "Отклонено" },
};
const VAC_STATUS_BADGE: Record<number, { cls: string; label: string }> = {
    0: { cls: "warning", label: "Ожидает" },
    1: { cls: "success", label: "Одобрено" },
    2: { cls: "danger", label: "Отклонено" },
    3: { cls: "neutral", label: "Отменено" },
};

export const EmployeeDashboard: React.FC<EmployeeDashboardProps> = ({
    employeeId,
    employeeName,
    isNewEmployee,
}) => {
    const [data, setData] = useState<any>(null);
    const [loading, setLoading] = useState<boolean>(true);
    const [error, setError] = useState<string | null>(null);

    const loadData = useCallback(() => {
        setLoading(true);
        setError(null);
        api.get(`/tma/dashboard/${employeeId}`)
            .then((res) => {
                setData(res.data);
                setLoading(false);
            })
            .catch((err) => {
                console.error(err);
                setError(
                    err.response?.data?.message ||
                        err.message ||
                        "Ошибка загрузки дашборда",
                );
                setLoading(false);
            });
    }, [employeeId]);

    useEffect(() => {
        loadData();
    }, [loadData]);

    useEffect(() => {
        const onStatusUpdated = () => loadData();
        window.addEventListener("hrops:status-updated", onStatusUpdated);
        return () => {
            window.removeEventListener("hrops:status-updated", onStatusUpdated);
        };
    }, [loadData]);

    if (loading) {
        return (
            <div
                className="flex-center"
                style={{ height: "50vh", flexDirection: "column", gap: "12px" }}
            >
                <div className="spinner" />
                <div className="text-subtitle" style={{ fontSize: "0.88rem" }}>
                    Загружаем панель сотрудника...
                </div>
            </div>
        );
    }

    if (error) {
        return (
            <div
                className="glass-card animate-fade-in"
                style={{
                    padding: "24px",
                    textAlign: "center",
                    margin: "16px auto",
                    maxWidth: "360px",
                }}
            >
                <div style={{ fontSize: "2.5rem", marginBottom: "12px" }}>
                    ⚠️
                </div>
                <h3
                    style={{
                        color: "#dc2626",
                        marginBottom: "8px",
                        fontWeight: 600,
                    }}
                >
                    Не удалось загрузить данные
                </h3>
                <p
                    className="text-subtitle"
                    style={{
                        fontSize: "0.82rem",
                        marginBottom: "18px",
                        color: "var(--text-secondary)",
                    }}
                >
                    {error}
                </p>
                <button
                    className="btn-primary"
                    onClick={loadData}
                    style={{ padding: "10px 20px", fontSize: "0.85rem" }}
                >
                    🔄 Повторить попытку
                </button>
            </div>
        );
    }

    if (!data) return null;

    const { vacation, tasks, equipment, vacations, itRequests } = data;
    const overdueTasks = tasks.filter((t: any) => t.isOverdue).length;
    const pct =
        vacation.total > 0
            ? Math.round((vacation.used / vacation.total) * 100)
            : 0;

    return (
        <div
            className="animate-fade-in delay-100"
            style={{ display: "flex", flexDirection: "column", gap: "16px" }}
        >
            {/* ===== ONBOARDING CHECKLIST (only new employees <90 days) ===== */}
            {isNewEmployee && (
                <OnboardingChecklist
                    employeeId={employeeId}
                    employeeName={employeeName}
                />
            )}

            {/* ===== VACATION CARD ===== */}
            <div className="glass-card">
                <div className="section-header">
                    <div className="section-title">🌴 Отпуск</div>
                    <span
                        className={`badge ${vacation.remaining > 10 ? "success" : vacation.remaining > 5 ? "warning" : "danger"}`}
                    >
                        {vacation.remaining} дн. доступно
                    </span>
                </div>
                <div
                    className="progress-bar-bg"
                    style={{ marginBottom: "8px" }}
                >
                    <div
                        className="progress-bar-fill"
                        style={{
                            width: `${pct}%`,
                            background:
                                pct > 80
                                    ? "linear-gradient(135deg,#f59e0b,#d97706)"
                                    : undefined,
                        }}
                    />
                </div>
                <div
                    className="flex-between text-subtitle"
                    style={{ fontSize: "0.78rem" }}
                >
                    <span>Использовано: {vacation.used} дн.</span>
                    <span>Всего: {vacation.total} дн.</span>
                </div>

                {/* Upcoming vacation requests */}
                {vacations && vacations.length > 0 && (
                    <div
                        style={{
                            marginTop: "12px",
                            borderTop: "1px solid var(--border-light)",
                            paddingTop: "12px",
                        }}
                    >
                        <div
                            className="section-label"
                            style={{ marginBottom: "8px" }}
                        >
                            Мои заявки
                        </div>
                        {vacations.slice(0, 3).map((v: any) => {
                            const st = VAC_STATUS_BADGE[v.status] ?? {
                                cls: "neutral",
                                label: v.status,
                            };
                            return (
                                <div
                                    key={v.id}
                                    className="list-item"
                                    style={{ padding: "8px 0", flexDirection: "column", alignItems: "flex-start", gap: 4 }}
                                >
                                    <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", width: "100%" }}>
                                        <div style={{ flex: 1 }}>
                                            <div
                                                style={{
                                                    fontSize: "0.85rem",
                                                    fontWeight: 500,
                                                }}
                                            >
                                                {new Date(
                                                    v.startDate,
                                                ).toLocaleDateString("ru-RU")}{" "}
                                                —{" "}
                                                {new Date(
                                                    v.endDate,
                                                ).toLocaleDateString("ru-RU")}
                                            </div>
                                            <div
                                                className="text-subtitle"
                                                style={{ fontSize: "0.78rem" }}
                                            >
                                                {v.daysCount} дней
                                            </div>
                                        </div>
                                        <span className={`badge ${st.cls}`}>
                                            {st.label}
                                        </span>
                                    </div>
                                    {v.status === 2 && v.reason && (
                                        <div style={{
                                            fontSize: "0.75rem",
                                            color: "#dc2626",
                                            fontStyle: "italic",
                                            background: "#fee2e2",
                                            border: "1px solid #fca5a5",
                                            borderRadius: 8,
                                            padding: "4px 10px",
                                            width: "100%",
                                        }}>
                                            ❌ Причина: {v.reason}
                                        </div>
                                    )}
                                </div>
                            );
                        })}
                    </div>
                )}
            </div>

            {/* ===== TASKS CARD ===== */}
            <div className="glass-card">
                <div className="section-header">
                    <div className="section-title">✅ Задачи</div>
                    {overdueTasks > 0 && (
                        <span className="badge danger">
                            ⚠ {overdueTasks} просрочено
                        </span>
                    )}
                    {overdueTasks === 0 && tasks.length > 0 && (
                        <span className="badge success">
                            {tasks.length} активных
                        </span>
                    )}
                </div>
                {tasks.length === 0 ? (
                    <div className="empty-state">
                        <div className="empty-state-icon">🎉</div>
                        <div className="empty-state-text">
                            Нет активных задач!
                        </div>
                    </div>
                ) : (
                    tasks.slice(0, 4).map((t: any) => (
                        <div key={t.id} className="list-item">
                            <div style={{ flex: 1 }}>
                                <div
                                    style={{
                                        fontWeight: 500,
                                        fontSize: "0.88rem",
                                        color: t.isOverdue
                                            ? "#dc2626"
                                            : "var(--text-primary)",
                                    }}
                                >
                                    {t.titleRu}
                                </div>
                                <div
                                    className="text-subtitle"
                                    style={{
                                        fontSize: "0.78rem",
                                        marginTop: "3px",
                                    }}
                                >
                                    {t.priority === 1
                                        ? "🔴 Критично"
                                        : t.priority === 2
                                          ? "🟠 Высокий"
                                          : "🟢 Обычный"}
                                </div>
                            </div>
                            {t.isOverdue && (
                                <span className="badge danger">Просрочено</span>
                            )}
                        </div>
                    ))
                )}
            </div>

            {/* ===== EQUIPMENT CARD ===== */}
            {equipment.length > 0 && (
                <div className="glass-card">
                    <div className="section-header">
                        <div className="section-title">💻 Оборудование</div>
                        <span className="badge info">
                            {equipment.length} заявок
                        </span>
                    </div>
                    {equipment.map((e: any) => (
                        <div key={e.id} className="list-item">
                            <div style={{ flex: 1 }}>
                                <div
                                    style={{
                                        fontWeight: 500,
                                        fontSize: "0.88rem",
                                    }}
                                >
                                    Заявка {e.ticketNumber}
                                </div>
                                <div
                                    className="text-subtitle"
                                    style={{ fontSize: "0.78rem" }}
                                >
                                    {[
                                        "Ноутбук",
                                        "Монитор",
                                        "Клавиатура",
                                        "Мышь",
                                        "Гарнитура",
                                        "Телефон",
                                        "Кресло",
                                        "Стол",
                                    ][e.type] ?? "Оборудование"}
                                </div>
                            </div>
                            <span className="badge warning">В обработке</span>
                        </div>
                    ))}
                </div>
            )}

            {/* ===== IT REQUESTS CARD ===== */}
            {itRequests && itRequests.length > 0 && (
                <div className="glass-card">
                    <div className="section-header">
                        <div className="section-title">🔧 IT-заявки</div>
                        <span className="badge purple">
                            {itRequests.length}
                        </span>
                    </div>
                    {itRequests.map((r: any) => {
                        const st = IT_STATUS_BADGE[r.status] ?? {
                            cls: "neutral",
                            label: "Неизвестно",
                        };
                        return (
                            <div key={r.id} className="list-item" style={{ flexDirection: "column", alignItems: "flex-start", gap: 4 }}>
                                <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", width: "100%" }}>
                                    <div style={{ flex: 1 }}>
                                        <div
                                            style={{
                                                fontWeight: 500,
                                                fontSize: "0.88rem",
                                            }}
                                        >
                                            {r.systemName}
                                        </div>
                                        <div
                                            className="text-subtitle"
                                            style={{ fontSize: "0.78rem" }}
                                        >
                                            {IT_TYPE_LABEL[r.type] ?? "IT"}
                                        </div>
                                    </div>
                                    <span className={`badge ${st.cls}`}>
                                        {st.label}
                                    </span>
                                </div>
                                {r.status === 4 && r.reason && (
                                    <div style={{
                                        fontSize: "0.75rem",
                                        color: "#dc2626",
                                        fontStyle: "italic",
                                        background: "#fee2e2",
                                        border: "1px solid #fca5a5",
                                        borderRadius: 8,
                                        padding: "4px 10px",
                                        width: "100%",
                                    }}>
                                        ❌ Причина: {r.reason}
                                    </div>
                                )}
                            </div>
                        );
                    })}
                </div>
            )}

            {/* ===== SERVICE ACTIONS ===== */}
            <ServiceActions
                employeeId={employeeId}
                vacationBalance={vacation}
                onRequestCreated={loadData}
            />
        </div>
    );
};
