import React, { useState } from "react";
import api from "../api";

interface VacationRequestFormProps {
    employeeId: number;
    vacationBalance: { total: number; used: number; remaining: number };
    onSuccess?: () => void;
}

export const VacationRequestForm: React.FC<VacationRequestFormProps> = ({
    employeeId,
    vacationBalance,
    onSuccess,
}) => {
    const today = new Date();
    const minDate = new Date(
        today.getFullYear(),
        today.getMonth(),
        today.getDate() + 1,
    )
        .toISOString()
        .split("T")[0];

    const [startDate, setStartDate] = useState("");
    const [endDate, setEndDate] = useState("");
    const [loading, setLoading] = useState(false);
    const [success, setSuccess] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const daysCount =
        startDate && endDate
            ? Math.max(
                  0,
                  Math.round(
                      (new Date(endDate).getTime() -
                          new Date(startDate).getTime()) /
                          86400000,
                  ) + 1,
              )
            : 0;

    const isOverBalance = daysCount > vacationBalance.remaining;

    const handleSubmit = async () => {
        if (!startDate || !endDate) {
            setError("Выберите даты начала и окончания");
            return;
        }
        if (new Date(endDate) <= new Date(startDate)) {
            setError("Дата окончания должна быть позже даты начала");
            return;
        }
        if (isOverBalance) {
            setError(
                `Превышен баланс. Доступно: ${vacationBalance.remaining} дн.`,
            );
            return;
        }

        setLoading(true);
        setError(null);
        try {
            await api.post("/tma/vacation", {
                employeeId,
                startDate: new Date(startDate).toISOString(),
                endDate: new Date(endDate).toISOString(),
            });
            setSuccess(true);
            setTimeout(() => {
                setSuccess(false);
                setStartDate("");
                setEndDate("");
                onSuccess?.();
            }, 2500);
        } catch (e: any) {
            setError(e.response?.data?.error || "Ошибка при создании заявки");
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
                <div style={{ fontSize: "2rem", marginBottom: "8px" }}>🌴</div>
                <div style={{ fontWeight: 700, fontSize: "1rem" }}>
                    Заявка подана!
                </div>
                <div style={{ fontSize: "0.85rem", marginTop: "4px" }}>
                    HR рассмотрит заявку в течение 2 рабочих дней.
                </div>
            </div>
        );
    }

    return (
        <div style={{ display: "flex", flexDirection: "column", gap: "16px" }}>
            {/* Balance info */}
            <div
                style={{
                    display: "flex",
                    gap: "8px",
                }}
            >
                <div className="stat-card" style={{ flex: 1 }}>
                    <div className="stat-value">
                        {vacationBalance.remaining}
                    </div>
                    <div className="stat-label">Доступно дн.</div>
                </div>
                <div className="stat-card" style={{ flex: 1 }}>
                    <div className="stat-value" style={{ color: "#f59e0b" }}>
                        {vacationBalance.used}
                    </div>
                    <div className="stat-label">Использовано</div>
                </div>
                <div className="stat-card" style={{ flex: 1 }}>
                    <div
                        className="stat-value"
                        style={{ color: "var(--text-secondary)" }}
                    >
                        {vacationBalance.total}
                    </div>
                    <div className="stat-label">Всего дн.</div>
                </div>
            </div>

            <div>
                <div className="section-label">Дата начала</div>
                <input
                    className="input-field"
                    type="date"
                    min={minDate}
                    value={startDate}
                    onChange={(e) => {
                        setStartDate(e.target.value);
                        if (endDate && e.target.value >= endDate)
                            setEndDate("");
                    }}
                />
            </div>

            <div>
                <div className="section-label">Дата окончания</div>
                <input
                    className="input-field"
                    type="date"
                    min={startDate || minDate}
                    value={endDate}
                    onChange={(e) => setEndDate(e.target.value)}
                    disabled={!startDate}
                />
            </div>

            {daysCount > 0 && (
                <div
                    style={{
                        padding: "12px 16px",
                        borderRadius: "var(--radius-sm)",
                        background: isOverBalance
                            ? "#fee2e2"
                            : "var(--green-100)",
                        border: `1px solid ${isOverBalance ? "#fca5a5" : "var(--green-200)"}`,
                        display: "flex",
                        justifyContent: "space-between",
                        alignItems: "center",
                    }}
                >
                    <span
                        style={{
                            fontWeight: 600,
                            color: isOverBalance
                                ? "#dc2626"
                                : "var(--green-700)",
                        }}
                    >
                        {isOverBalance ? "⚠️ Превышение" : "✅ Итого дней:"}
                    </span>
                    <span
                        style={{
                            fontWeight: 800,
                            fontSize: "1.2rem",
                            color: isOverBalance
                                ? "#dc2626"
                                : "var(--accent-primary)",
                        }}
                    >
                        {daysCount}
                    </span>
                </div>
            )}

            {error && <div className="toast error">{error}</div>}

            <button
                className="btn btn-primary btn-full"
                onClick={handleSubmit}
                disabled={loading || !startDate || !endDate || isOverBalance}
            >
                {loading ? "Отправка..." : "🌴 Подать заявку на отпуск"}
            </button>
        </div>
    );
};
