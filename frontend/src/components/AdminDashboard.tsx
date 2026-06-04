import React, { useEffect, useState } from "react";
import {
    PieChart,
    Pie,
    Cell,
    ResponsiveContainer,
    Tooltip,
    BarChart,
    Bar,
    XAxis,
    YAxis,
    CartesianGrid,
} from "recharts";
import api from "../api";
import { HrInbox } from "./HrInbox";

const COLORS = [
    "#16a34a",
    "#22c55e",
    "#4ade80",
    "#86efac",
    "#f59e0b",
    "#3b82f6",
];

type Section = "analytics" | "inbox" | "employees";

export const AdminDashboard: React.FC = () => {
    const [stats, setStats] = useState<any>(null);
    const [employees, setEmployees] = useState<any[]>([]);
    const [section, setSection] = useState<Section>("analytics");
    const [pendingCount, setPendingCount] = useState(0);
    const [empSearch, setEmpSearch] = useState("");

    useEffect(() => {
        api.get("/tma/admin/analytics")
            .then((res) => setStats(res.data))
            .catch(console.error);
        api.get("/tma/admin/employees")
            .then((res) => setEmployees(res.data))
            .catch(console.error);
    }, []);

    const sections: { key: Section; label: string; badge?: number }[] = [
        { key: "analytics", label: "📊 Аналитика" },
        { key: "inbox", label: "📥 Заявки", badge: pendingCount },
        { key: "employees", label: "👥 Сотрудники" },
    ];

    const filteredEmp = employees.filter(
        (e) =>
            e.nameRu.toLowerCase().includes(empSearch.toLowerCase()) ||
            (e.department ?? "")
                .toLowerCase()
                .includes(empSearch.toLowerCase()),
    );

    const getStatusColor = (status: string) => {
        if (status === "critical") return "#dc2626";
        if (status === "warning") return "#d97706";
        return "#16a34a";
    };

    return (
        <div
            className="animate-fade-in"
            style={{ display: "flex", flexDirection: "column", gap: "16px" }}
        >
            {/* Section switcher */}
            <div className="tab-bar">
                {sections.map((s) => (
                    <button
                        key={s.key}
                        className={`tab-btn ${section === s.key ? "active" : ""}`}
                        onClick={() => setSection(s.key)}
                    >
                        {s.label}
                        {(s.badge ?? 0) > 0 && (
                            <span
                                style={{
                                    marginLeft: "6px",
                                    background: "#ef4444",
                                    color: "#fff",
                                    borderRadius: "999px",
                                    padding: "1px 7px",
                                    fontSize: "0.7rem",
                                    fontWeight: 700,
                                }}
                            >
                                {s.badge}
                            </span>
                        )}
                    </button>
                ))}
            </div>

            {/* ===== ANALYTICS ===== */}
            {section === "analytics" && (
                <>
                    {!stats ? (
                        <div className="flex-center" style={{ height: "50vh" }}>
                            <div className="spinner" />
                        </div>
                    ) : (
                        <>
                            {/* KPI Row */}
                            <div className="grid-cols-2">
                                <div className="stat-card">
                                    <div className="stat-value">
                                        {stats.totalEmployees}
                                    </div>
                                    <div className="stat-label">
                                        Сотрудников
                                    </div>
                                </div>
                                <div className="stat-card">
                                    <div
                                        className="stat-value"
                                        style={{ color: "#f59e0b" }}
                                    >
                                        +{stats.newEmployees30Days}
                                    </div>
                                    <div className="stat-label">
                                        Новых за 30 дн.
                                    </div>
                                </div>
                                <div className="stat-card">
                                    <div
                                        className="stat-value"
                                        style={{
                                            color:
                                                stats.pendingRequests > 10
                                                    ? "#dc2626"
                                                    : "#16a34a",
                                        }}
                                    >
                                        {stats.pendingRequests}
                                    </div>
                                    <div className="stat-label">
                                        Ожидают решения
                                    </div>
                                </div>
                                <div className="stat-card">
                                    <div
                                        className="stat-value"
                                        style={{ color: "#f59e0b" }}
                                    >
                                        {stats.csatScore > 0
                                            ? stats.csatScore.toFixed(1)
                                            : "—"}
                                    </div>
                                    <div className="stat-label">
                                        CSAT{" "}
                                        {stats.csatTotal > 0
                                            ? `(${stats.csatTotal})`
                                            : ""}
                                    </div>
                                </div>
                            </div>

                            {/* Bottlenecks */}
                            <div className="glass-card">
                                <div
                                    className="section-title"
                                    style={{ marginBottom: "12px" }}
                                >
                                    ⚡ Узкие места
                                </div>
                                {stats.bottlenecks.map((b: any) => (
                                    <div
                                        key={b.category}
                                        className="list-item"
                                        style={{ padding: "10px 0" }}
                                    >
                                        <div style={{ flex: 1 }}>
                                            <div
                                                style={{
                                                    fontWeight: 600,
                                                    fontSize: "0.9rem",
                                                }}
                                            >
                                                {b.category}
                                            </div>
                                        </div>
                                        <div
                                            style={{
                                                display: "flex",
                                                alignItems: "center",
                                                gap: "8px",
                                            }}
                                        >
                                            <span
                                                style={{
                                                    fontWeight: 800,
                                                    fontSize: "1.1rem",
                                                    color: getStatusColor(
                                                        b.status,
                                                    ),
                                                }}
                                            >
                                                {b.pendingCount}
                                            </span>
                                            <span
                                                className={`badge ${b.status === "critical" ? "danger" : b.status === "warning" ? "warning" : "success"}`}
                                            >
                                                {b.status === "critical"
                                                    ? "Критично"
                                                    : b.status === "warning"
                                                      ? "Внимание"
                                                      : "ОК"}
                                            </span>
                                        </div>
                                    </div>
                                ))}
                            </div>

                            {/* Pie Chart */}
                            {stats.topIntents.length > 0 && (
                                <div className="glass-card">
                                    <div
                                        className="section-title"
                                        style={{ marginBottom: "12px" }}
                                    >
                                        📊 Распределение запросов
                                    </div>
                                    <div style={{ height: "220px" }}>
                                        <ResponsiveContainer
                                            width="100%"
                                            height="100%"
                                        >
                                            <PieChart>
                                                <Pie
                                                    data={stats.topIntents}
                                                    cx="50%"
                                                    cy="50%"
                                                    innerRadius={55}
                                                    outerRadius={80}
                                                    paddingAngle={4}
                                                    dataKey="value"
                                                    stroke="none"
                                                >
                                                    {stats.topIntents.map(
                                                        (
                                                            _: any,
                                                            index: number,
                                                        ) => (
                                                            <Cell
                                                                key={`cell-${index}`}
                                                                fill={
                                                                    COLORS[
                                                                        index %
                                                                            COLORS.length
                                                                    ]
                                                                }
                                                            />
                                                        ),
                                                    )}
                                                </Pie>
                                                <Tooltip
                                                    contentStyle={{
                                                        backgroundColor: "#fff",
                                                        border: "1px solid #dcfce7",
                                                        borderRadius: "8px",
                                                        fontSize: "0.85rem",
                                                    }}
                                                    itemStyle={{
                                                        color: "#111827",
                                                    }}
                                                />
                                            </PieChart>
                                        </ResponsiveContainer>
                                    </div>
                                    <div
                                        style={{
                                            display: "flex",
                                            flexWrap: "wrap",
                                            gap: "8px",
                                            justifyContent: "center",
                                            marginTop: "8px",
                                        }}
                                    >
                                        {stats.topIntents.map(
                                            (intent: any, index: number) => (
                                                <div
                                                    key={intent.name}
                                                    style={{
                                                        display: "flex",
                                                        alignItems: "center",
                                                        fontSize: "0.8rem",
                                                        gap: "4px",
                                                    }}
                                                >
                                                    <div
                                                        style={{
                                                            width: "10px",
                                                            height: "10px",
                                                            borderRadius: "50%",
                                                            backgroundColor:
                                                                COLORS[
                                                                    index %
                                                                        COLORS.length
                                                                ],
                                                        }}
                                                    />
                                                    {intent.name} (
                                                    {intent.value})
                                                </div>
                                            ),
                                        )}
                                    </div>
                                </div>
                            )}

                            {/* Bar Chart */}
                            {stats.topIntents.length > 0 && (
                                <div className="glass-card">
                                    <div
                                        className="section-title"
                                        style={{ marginBottom: "12px" }}
                                    >
                                        📈 Топ запросов
                                    </div>
                                    <div style={{ height: "220px" }}>
                                        <ResponsiveContainer
                                            width="100%"
                                            height="100%"
                                        >
                                            <BarChart
                                                data={stats.topIntents}
                                                margin={{
                                                    top: 0,
                                                    right: 0,
                                                    left: -20,
                                                    bottom: 0,
                                                }}
                                            >
                                                <CartesianGrid
                                                    strokeDasharray="3 3"
                                                    stroke="rgba(0,0,0,0.06)"
                                                    vertical={false}
                                                />
                                                <XAxis
                                                    dataKey="name"
                                                    stroke="#9ca3af"
                                                    fontSize={11}
                                                    tickLine={false}
                                                    axisLine={false}
                                                />
                                                <YAxis
                                                    stroke="#9ca3af"
                                                    fontSize={11}
                                                    tickLine={false}
                                                    axisLine={false}
                                                />
                                                <Tooltip
                                                    cursor={{
                                                        fill: "rgba(22,163,74,0.06)",
                                                    }}
                                                    contentStyle={{
                                                        backgroundColor: "#fff",
                                                        border: "1px solid #dcfce7",
                                                        borderRadius: "8px",
                                                        fontSize: "0.85rem",
                                                    }}
                                                />
                                                <Bar
                                                    dataKey="value"
                                                    fill="#16a34a"
                                                    radius={[6, 6, 0, 0]}
                                                />
                                            </BarChart>
                                        </ResponsiveContainer>
                                    </div>
                                </div>
                            )}
                        </>
                    )}
                </>
            )}

            {/* ===== INBOX ===== */}
            {section === "inbox" && <HrInbox onCountChange={setPendingCount} />}

            {/* ===== EMPLOYEES ===== */}
            {section === "employees" && (
                <div className="glass-card no-hover">
                    <div className="section-header">
                        <div className="section-title">👥 Сотрудники</div>
                        <span className="badge info">{employees.length}</span>
                    </div>
                    <input
                        className="input-field"
                        type="text"
                        placeholder="🔍 Поиск по имени или отделу..."
                        value={empSearch}
                        onChange={(e) => setEmpSearch(e.target.value)}
                        style={{ marginBottom: "12px" }}
                    />
                    {filteredEmp.length === 0 ? (
                        <div className="empty-state">
                            <div className="empty-state-icon">🔍</div>
                            <div className="empty-state-text">Не найдено</div>
                        </div>
                    ) : (
                        filteredEmp.map((emp) => (
                            <div key={emp.id} className="list-item">
                                <div
                                    style={{
                                        width: "36px",
                                        height: "36px",
                                        borderRadius: "50%",
                                        background: emp.isHrAdmin
                                            ? "var(--gradient-primary)"
                                            : "var(--green-100)",
                                        display: "flex",
                                        alignItems: "center",
                                        justifyContent: "center",
                                        fontWeight: 700,
                                        fontSize: "0.9rem",
                                        color: emp.isHrAdmin
                                            ? "#fff"
                                            : "var(--green-700)",
                                        flexShrink: 0,
                                    }}
                                >
                                    {emp.nameRu.charAt(0)}
                                </div>
                                <div style={{ flex: 1, minWidth: 0 }}>
                                    <div
                                        style={{
                                            fontWeight: 600,
                                            fontSize: "0.88rem",
                                            whiteSpace: "nowrap",
                                            overflow: "hidden",
                                            textOverflow: "ellipsis",
                                        }}
                                    >
                                        {emp.nameRu}
                                        {emp.isHrAdmin && (
                                            <span
                                                className="badge success"
                                                style={{
                                                    marginLeft: "6px",
                                                    fontSize: "0.65rem",
                                                }}
                                            >
                                                HR
                                            </span>
                                        )}
                                    </div>
                                    <div
                                        className="text-subtitle"
                                        style={{ fontSize: "0.78rem" }}
                                    >
                                        {emp.position || "Должность не указана"}{" "}
                                        · {emp.department || "Отдел не указан"}
                                    </div>
                                </div>
                                <div
                                    style={{
                                        textAlign: "right",
                                        flexShrink: 0,
                                    }}
                                >
                                    <span
                                        className={`badge ${emp.vacationDaysRemaining < 5 ? "danger" : "success"}`}
                                    >
                                        {emp.vacationDaysRemaining} дн.
                                    </span>
                                </div>
                            </div>
                        ))
                    )}
                </div>
            )}
        </div>
    );
};
