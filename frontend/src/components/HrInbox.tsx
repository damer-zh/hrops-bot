import React, { useEffect, useState, useCallback } from "react";
import api from "../api";

interface HrInboxProps {
    onCountChange?: (count: number) => void;
}

type TabType = "vacations" | "certificates" | "equipment" | "itRequests";

const CERT_TYPE: Record<string, string> = {
    "0": "С места работы",
    "1": "О доходах",
    "2": "ИПН/КПН",
    "3": "Стаж",
    EmploymentConfirmation: "С места работы",
    SalaryStatement: "О доходах",
    IncomeTax: "ИПН/КПН",
    WorkExperience: "Стаж",
};
const IT_TYPE: Record<number, string> = {
    1: "🖥 Система",
    2: "📁 Папка",
    3: "👥 Группа",
    4: "📧 Почта",
    5: "🔒 VPN",
    6: "🔧 Прочее",
};
const EQUIP_TYPE = [
    "💻 Ноутбук",
    "🖥 Монитор",
    "⌨️ Клавиатура",
    "🖱 Мышь",
    "🎧 Гарнитура",
    "📱 Телефон",
    "🪑 Кресло",
    "🪞 Стол",
];

export const HrInbox: React.FC<HrInboxProps> = ({ onCountChange }) => {
    const [tab, setTab] = useState<TabType>("vacations");
    const [data, setData] = useState<any>(null);
    const [loading, setLoading] = useState(true);
    const [processing, setProcessing] = useState<string | null>(null);

    const fetchData = useCallback(async () => {
        setLoading(true);
        try {
            const res = await api.get("/tma/admin/requests");
            setData(res.data);
            const total =
                (res.data.vacations?.length ?? 0) +
                (res.data.certificates?.length ?? 0) +
                (res.data.itRequests?.length ?? 0) +
                (res.data.equipment?.length ?? 0);
            onCountChange?.(total);
        } catch (e) {
            console.error(e);
        } finally {
            setLoading(false);
        }
    }, [onCountChange]);

    useEffect(() => {
        fetchData();
    }, [fetchData]);

    const approve = async (type: string, id: number) => {
        setProcessing(`${type}-${id}`);
        try {
            if (type === "vacation")
                await api.post(`/tma/admin/vacation/${id}/approve`);
            if (type === "certificate")
                await api.post(`/tma/admin/certificate/${id}/approve`);
            if (type === "equipment")
                await api.post(`/tma/admin/equipment/${id}/approve`);
            if (type === "it")
                await api.post(`/tma/admin/it-request/${id}/status`, {
                    status: 3,
                });
            await fetchData();
        } finally {
            setProcessing(null);
        }
    };

    const reject = async (type: string, id: number) => {
        // Для отпуска и IT-заявок требуется причина, для справок и оборудования опционально
        const needsReason = type === "vacation" || type === "it";
        let reason: string | null = null;
        if (needsReason) {
            reason = window
                .prompt("Укажите причину отказа")
                ?.trim() ?? null;
            if (!reason) {
                if (window.Telegram?.WebApp) {
                    window.Telegram.WebApp.showAlert("Причина отказа обязательна");
                }
                return;
            }
        } else {
            // Для справок и оборудования предложить опционально
            reason = window.prompt(
                "Укажите причину отказа (опционально)"
            );
        }

        setProcessing(`${type}-reject-${id}`);
        try {
            if (type === "vacation")
                await api.post(`/tma/admin/vacation/${id}/reject`, {
                    reason,
                });
            if (type === "certificate")
                await api.post(`/tma/admin/certificate/${id}/reject`, {
                    reason,
                });
            if (type === "equipment")
                await api.post(`/tma/admin/equipment/${id}/reject`, {
                    reason,
                });
            if (type === "it")
                await api.post(`/tma/admin/it-request/${id}/status`, {
                    status: 4,
                    note: reason,
                });
            await fetchData();
        } finally {
            setProcessing(null);
        }
    };

    const formatDate = (d: string) =>
        new Date(d).toLocaleDateString("ru-RU", {
            day: "2-digit",
            month: "2-digit",
            year: "2-digit",
        });

    const tabs: { key: TabType; label: string; count: number }[] = [
        {
            key: "vacations",
            label: "🌴 Отпуска",
            count: data?.vacations?.length ?? 0,
        },
        {
            key: "certificates",
            label: "📄 Справки",
            count: data?.certificates?.length ?? 0,
        },
        {
            key: "equipment",
            label: "💻 Техника",
            count: data?.equipment?.length ?? 0,
        },
        {
            key: "itRequests",
            label: "🔧 IT",
            count: data?.itRequests?.length ?? 0,
        },
    ];

    const ActionRow = ({
        type,
        id,
        approveLabel = "✓ Одобрить",
        rejectLabel = "✕ Отклонить",
    }: {
        type: string;
        id: number;
        approveLabel?: string;
        rejectLabel?: string;
    }) => (
        <div style={{ display: "flex", gap: "8px", width: "100%" }}>
            <button
                className="btn btn-primary btn-sm"
                style={{ flex: 1 }}
                disabled={!!processing}
                onClick={() => approve(type, id)}
            >
                {processing === `${type}-${id}` ? "..." : approveLabel}
            </button>
            <button
                className="btn btn-danger btn-sm"
                style={{ flex: 1 }}
                disabled={!!processing}
                onClick={() => reject(type, id)}
            >
                {processing === `${type}-reject-${id}` ? "..." : rejectLabel}
            </button>
        </div>
    );

    return (
        <div className="glass-card no-hover animate-fade-in">
            <div className="section-header">
                <div className="section-title">📥 Входящие заявки</div>
                <button onClick={fetchData} className="btn btn-ghost btn-sm">
                    🔄
                </button>
            </div>

            {/* Tab bar */}
            <div className="tab-bar" style={{ marginBottom: "16px" }}>
                {tabs.map((t) => (
                    <button
                        key={t.key}
                        className={`tab-btn ${tab === t.key ? "active" : ""}`}
                        onClick={() => setTab(t.key)}
                    >
                        {t.label}
                        {t.count > 0 && (
                            <span
                                style={{
                                    marginLeft: "6px",
                                    background: "var(--accent-primary)",
                                    color: "#fff",
                                    borderRadius: "999px",
                                    padding: "1px 7px",
                                    fontSize: "0.7rem",
                                    fontWeight: 700,
                                }}
                            >
                                {t.count}
                            </span>
                        )}
                    </button>
                ))}
            </div>

            {loading ? (
                <div className="flex-center" style={{ padding: "32px" }}>
                    <div className="spinner" />
                </div>
            ) : (
                <>
                    {/* ===== VACATIONS ===== */}
                    {tab === "vacations" && (
                        <div>
                            {!data?.vacations || data.vacations.length === 0 ? (
                                <div className="empty-state">
                                    <div className="empty-state-icon">✅</div>
                                    <div className="empty-state-text">
                                        Нет заявок на отпуск
                                    </div>
                                </div>
                            ) : (
                                data.vacations.map((v: any) => (
                                    <div
                                        key={v.id}
                                        className="list-item"
                                        style={{
                                            flexDirection: "column",
                                            alignItems: "flex-start",
                                            gap: "10px",
                                        }}
                                    >
                                        <div style={{ width: "100%" }}>
                                            <div className="flex-between">
                                                <span
                                                    style={{
                                                        fontWeight: 600,
                                                        fontSize: "0.9rem",
                                                    }}
                                                >
                                                    {v.employee.nameRu}
                                                </span>
                                                <span className="badge warning">
                                                    {v.daysCount} дн.
                                                </span>
                                            </div>
                                            <div
                                                className="text-subtitle"
                                                style={{
                                                    fontSize: "0.8rem",
                                                    marginTop: "4px",
                                                }}
                                            >
                                                {v.employee.department} ·{" "}
                                                {formatDate(v.startDate)} —{" "}
                                                {formatDate(v.endDate)}
                                            </div>
                                        </div>
                                        <ActionRow type="vacation" id={v.id} />
                                    </div>
                                ))
                            )}
                        </div>
                    )}

                    {/* ===== CERTIFICATES ===== */}
                    {tab === "certificates" && (
                        <div>
                            {!data?.certificates ||
                            data.certificates.length === 0 ? (
                                <div className="empty-state">
                                    <div className="empty-state-icon">✅</div>
                                    <div className="empty-state-text">
                                        Нет заявок на справки
                                    </div>
                                </div>
                            ) : (
                                data.certificates.map((c: any) => (
                                    <div
                                        key={c.id}
                                        className="list-item"
                                        style={{
                                            flexDirection: "column",
                                            alignItems: "flex-start",
                                            gap: "10px",
                                        }}
                                    >
                                        <div style={{ width: "100%" }}>
                                            <div className="flex-between">
                                                <span
                                                    style={{
                                                        fontWeight: 600,
                                                        fontSize: "0.9rem",
                                                    }}
                                                >
                                                    {c.employee.nameRu}
                                                </span>
                                                <span className="badge info">
                                                    {CERT_TYPE[c.type] ??
                                                        "Справка"}
                                                </span>
                                            </div>
                                            <div
                                                className="text-subtitle"
                                                style={{
                                                    fontSize: "0.8rem",
                                                    marginTop: "4px",
                                                }}
                                            >
                                                {c.employee.department} ·{" "}
                                                {c.deliveryMethod === "digital"
                                                    ? "📱 Электронно"
                                                    : "📋 Бумажный"}{" "}
                                                · {formatDate(c.createdAt)}
                                            </div>
                                        </div>
                                        <ActionRow
                                            type="certificate"
                                            id={c.id}
                                            approveLabel="✓ Выполнено"
                                        />
                                    </div>
                                ))
                            )}
                        </div>
                    )}

                    {/* ===== EQUIPMENT ===== */}
                    {tab === "equipment" && (
                        <div>
                            {!data?.equipment || data.equipment.length === 0 ? (
                                <div className="empty-state">
                                    <div className="empty-state-icon">✅</div>
                                    <div className="empty-state-text">
                                        Нет заявок на оборудование
                                    </div>
                                </div>
                            ) : (
                                data.equipment.map((e: any) => (
                                    <div
                                        key={e.id}
                                        className="list-item"
                                        style={{
                                            flexDirection: "column",
                                            alignItems: "flex-start",
                                            gap: "10px",
                                        }}
                                    >
                                        <div style={{ width: "100%" }}>
                                            <div className="flex-between">
                                                <span
                                                    style={{
                                                        fontWeight: 600,
                                                        fontSize: "0.9rem",
                                                    }}
                                                >
                                                    {e.employee.nameRu}
                                                </span>
                                                <span className="badge purple">
                                                    {EQUIP_TYPE[e.type] ??
                                                        "🖥 Техника"}
                                                </span>
                                            </div>
                                            <div
                                                className="text-subtitle"
                                                style={{
                                                    fontSize: "0.8rem",
                                                    marginTop: "4px",
                                                }}
                                            >
                                                {e.employee.department} · Тикет:{" "}
                                                {e.ticketNumber} ·{" "}
                                                {formatDate(e.createdAt)}
                                            </div>
                                        </div>
                                        <ActionRow
                                            type="equipment"
                                            id={e.id}
                                            approveLabel="🔄 В работу"
                                            rejectLabel="✕ Отклонить"
                                        />
                                    </div>
                                ))
                            )}
                        </div>
                    )}

                    {/* ===== IT REQUESTS ===== */}
                    {tab === "itRequests" && (
                        <div>
                            {!data?.itRequests ||
                            data.itRequests.length === 0 ? (
                                <div className="empty-state">
                                    <div className="empty-state-icon">✅</div>
                                    <div className="empty-state-text">
                                        Нет IT-заявок
                                    </div>
                                </div>
                            ) : (
                                data.itRequests.map((r: any) => (
                                    <div
                                        key={r.id}
                                        className="list-item"
                                        style={{
                                            flexDirection: "column",
                                            alignItems: "flex-start",
                                            gap: "10px",
                                        }}
                                    >
                                        <div style={{ width: "100%" }}>
                                            <div className="flex-between">
                                                <span
                                                    style={{
                                                        fontWeight: 600,
                                                        fontSize: "0.9rem",
                                                    }}
                                                >
                                                    {r.employee.nameRu}
                                                </span>
                                                <div
                                                    style={{
                                                        display: "flex",
                                                        gap: "4px",
                                                    }}
                                                >
                                                    {r.priority === 1 && (
                                                        <span className="badge danger">
                                                            Срочно
                                                        </span>
                                                    )}
                                                    <span className="badge purple">
                                                        {IT_TYPE[r.type] ??
                                                            "IT"}
                                                    </span>
                                                </div>
                                            </div>
                                            <div
                                                style={{
                                                    fontWeight: 500,
                                                    fontSize: "0.85rem",
                                                    marginTop: "4px",
                                                }}
                                            >
                                                {r.systemName}
                                            </div>
                                            {r.description && (
                                                <div
                                                    className="text-subtitle"
                                                    style={{
                                                        fontSize: "0.78rem",
                                                    }}
                                                >
                                                    {r.description}
                                                </div>
                                            )}
                                        </div>
                                        <ActionRow
                                            type="it"
                                            id={r.id}
                                            approveLabel="✓ Выполнено"
                                        />
                                    </div>
                                ))
                            )}
                        </div>
                    )}
                </>
            )}
        </div>
    );
};
