import React from "react";

type Role = "employee" | "hr";

interface RoleSelectProps {
    employeeName: string;
    isHrAdmin: boolean;
    onSelect: (role: Role) => void;
}

export const RoleSelect: React.FC<RoleSelectProps> = ({ employeeName, isHrAdmin, onSelect }) => {
    return (
        <div className="role-select-screen">
            {/* Decorative background blobs */}
            <div className="role-select-blob blob-1" />
            <div className="role-select-blob blob-2" />
            <div className="role-select-blob blob-3" />

            <div className="role-select-content animate-fade-in">
                {/* Logo / Greeting */}
                <div className="role-select-header">
                    <div className="role-select-logo">🏢</div>
                    <h1 className="role-select-title">HROps</h1>
                    <p className="role-select-greeting">
                        Добро пожаловать,{" "}
                        <span className="role-select-name">{employeeName.split(" ")[0]}</span>
                    </p>
                    <p className="role-select-subtitle">Выберите, как вы входите в систему</p>
                </div>

                {/* Role Cards */}
                <div className="role-select-cards">
                    {/* Employee Card */}
                    <button
                        id="role-select-employee"
                        className="role-card employee-card"
                        onClick={() => onSelect("employee")}
                    >
                        <div className="role-card-icon-wrap employee-icon-wrap">
                            <span className="role-card-icon">👤</span>
                        </div>
                        <div className="role-card-body">
                            <div className="role-card-title">Сотрудник</div>
                            <div className="role-card-desc">
                                Мои заявки, отпуска, документы и онбординг
                            </div>
                        </div>
                        <div className="role-card-arrow">→</div>
                    </button>

                    {/* HR Card */}
                    <button
                        id="role-select-hr"
                        className={`role-card hr-card ${!isHrAdmin ? "role-card-locked" : ""}`}
                        onClick={() => onSelect("hr")}
                    >
                        <div className="role-card-icon-wrap hr-icon-wrap">
                            <span className="role-card-icon">{isHrAdmin ? "🏢" : "🔒"}</span>
                        </div>
                        <div className="role-card-body">
                            <div className="role-card-title">
                                HR-панель
                                {!isHrAdmin && (
                                    <span className="role-card-badge-lock">Только HR</span>
                                )}
                            </div>
                            <div className="role-card-desc">
                                {isHrAdmin
                                    ? "Управление сотрудниками, заявки, аналитика"
                                    : "Доступно только HR-администраторам"}
                            </div>
                        </div>
                        <div className="role-card-arrow">→</div>
                    </button>
                </div>

                <p className="role-select-footer">HROps · Telegram Mini App</p>
            </div>
        </div>
    );
};
