import React, { useState } from "react";
import api from "../api";

interface ItRequestFormProps {
    employeeId: number;
    onSuccess?: () => void;
}

const IT_TYPES = [
    {
        value: 1,
        label: "🖥 Доступ к системе",
        placeholder: "Jira, GitHub, 1C, Confluence...",
    },
    {
        value: 2,
        label: "📁 Доступ к папке",
        placeholder: "Путь к папке или названию диска",
    },
    {
        value: 3,
        label: "👥 Добавить в группу",
        placeholder: "Название команды или рассылки",
    },
    {
        value: 4,
        label: "📧 Корпоративная почта",
        placeholder: "Формат: имя@company.kz",
    },
    {
        value: 5,
        label: "🔒 VPN доступ",
        placeholder: "Офис, ЦОД, или конкретная сеть",
    },
    { value: 6, label: "🔧 Прочее", placeholder: "Опишите, что нужно" },
];

const PRIORITIES = [
    { value: 1, label: "🔴 Срочно", desc: "Блокирует работу" },
    { value: 2, label: "🟡 Обычный", desc: "В рабочем порядке" },
    { value: 3, label: "🟢 Не срочно", desc: "При возможности" },
];

export const ItRequestForm: React.FC<ItRequestFormProps> = ({
    employeeId,
    onSuccess,
}) => {
    const [type, setType] = useState(1);
    const [systemName, setSystemName] = useState("");
    const [description, setDescription] = useState("");
    const [priority, setPriority] = useState(2);
    const [loading, setLoading] = useState(false);
    const [success, setSuccess] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const selectedType = IT_TYPES.find((t) => t.value === type)!;

    const handleSubmit = async () => {
        if (!systemName.trim()) {
            setError("Укажите название системы или ресурса");
            return;
        }
        setLoading(true);
        setError(null);
        try {
            await api.post("/tma/it-request", {
                employeeId,
                type,
                systemName: systemName.trim(),
                description: description.trim(),
                priority,
            });
            setSuccess(true);
            setTimeout(() => {
                setSuccess(false);
                setSystemName("");
                setDescription("");
                setPriority(2);
                setType(1);
                onSuccess?.();
            }, 2500);
        } catch (e: any) {
            setError("Ошибка: " + (e.response?.data?.error || e.message));
        } finally {
            setLoading(false);
        }
    };

    if (success) {
        return (
            <div
                className="toast success"
                style={{
                    flexDirection: "column",
                    alignItems: "center",
                    padding: "24px",
                    textAlign: "center",
                }}
            >
                <div style={{ fontSize: "2rem", marginBottom: "8px" }}>✅</div>
                <div style={{ fontWeight: 700, fontSize: "1rem" }}>
                    Заявка отправлена!
                </div>
                <div style={{ fontSize: "0.85rem", marginTop: "4px" }}>
                    IT-отдел получил ваш запрос. Ожидайте 1-3 рабочих дня.
                </div>
            </div>
        );
    }

    return (
        <div style={{ display: "flex", flexDirection: "column", gap: "16px" }}>
            <div>
                <div className="section-label">Тип запроса</div>
                <div
                    style={{
                        display: "flex",
                        flexDirection: "column",
                        gap: "8px",
                    }}
                >
                    {IT_TYPES.map((t) => (
                        <button
                            key={t.value}
                            onClick={() => {
                                setType(t.value);
                                setSystemName("");
                            }}
                            style={{
                                display: "flex",
                                alignItems: "center",
                                gap: "10px",
                                padding: "10px 14px",
                                borderRadius: "var(--radius-sm)",
                                border: `2px solid ${type === t.value ? "var(--accent-primary)" : "var(--border-light)"}`,
                                background:
                                    type === t.value
                                        ? "var(--green-50)"
                                        : "var(--bg-surface)",
                                cursor: "pointer",
                                transition: "all 0.2s",
                                fontFamily: "Inter, sans-serif",
                                fontSize: "0.88rem",
                                fontWeight: type === t.value ? 600 : 400,
                                color: "var(--text-primary)",
                                textAlign: "left",
                            }}
                        >
                            <span>{t.label}</span>
                        </button>
                    ))}
                </div>
            </div>

            <div>
                <div className="section-label">Название / Ресурс</div>
                <input
                    className="input-field"
                    type="text"
                    placeholder={selectedType.placeholder}
                    value={systemName}
                    onChange={(e) => setSystemName(e.target.value)}
                />
            </div>

            <div>
                <div className="section-label">Описание (необязательно)</div>
                <textarea
                    className="input-field"
                    rows={3}
                    placeholder="Дополнительные детали или причина запроса..."
                    value={description}
                    onChange={(e) => setDescription(e.target.value)}
                    style={{ resize: "none" }}
                />
            </div>

            <div>
                <div className="section-label">Приоритет</div>
                <div style={{ display: "flex", gap: "8px" }}>
                    {PRIORITIES.map((p) => (
                        <button
                            key={p.value}
                            onClick={() => setPriority(p.value)}
                            style={{
                                flex: 1,
                                padding: "10px 6px",
                                borderRadius: "var(--radius-sm)",
                                border: `2px solid ${priority === p.value ? "var(--accent-primary)" : "var(--border-light)"}`,
                                background:
                                    priority === p.value
                                        ? "var(--green-50)"
                                        : "var(--bg-surface)",
                                cursor: "pointer",
                                fontFamily: "Inter, sans-serif",
                                fontSize: "0.78rem",
                                fontWeight: 600,
                                color: "var(--text-primary)",
                                transition: "all 0.2s",
                                textAlign: "center",
                            }}
                        >
                            <div>{p.label}</div>
                            <div
                                style={{
                                    fontSize: "0.68rem",
                                    color: "var(--text-muted)",
                                    marginTop: "2px",
                                }}
                            >
                                {p.desc}
                            </div>
                        </button>
                    ))}
                </div>
            </div>

            {error && <div className="toast error">{error}</div>}

            <button
                className="btn btn-primary btn-full"
                onClick={handleSubmit}
                disabled={loading || !systemName.trim()}
            >
                {loading ? "Отправка..." : "📤 Отправить заявку"}
            </button>
        </div>
    );
};
