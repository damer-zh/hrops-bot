import React, { useEffect, useState } from "react";
import api from "../api";

interface OnboardingChecklistProps {
    employeeId: number;
    employeeName: string;
}

interface Progress {
    fireSafetyDone: boolean;
    generalSafetyDone: boolean;
    cyberSafetyDone: boolean;
    passReceived: boolean;
    faceIdDone: boolean;
    workplaceSetupRequested: boolean;
    progressPercent: number;
    startedAt: string;
}

const STEPS = [
    {
        key: "fire_safety",
        field: "fireSafetyDone",
        icon: "🔥",
        title: "Пожарная безопасность",
        desc: "Пройти инструктаж по пожарной безопасности",
        fullDesc: "Вам необходимо пройти обязательный инструктаж по пожарной безопасности. Ознакомьтесь с планами эвакуации, расположением огнетушителей и порядком действий при ЧС. Инструктаж проводится в кабинете 101 или онлайн на внутреннем портале."
    },
    {
        key: "general_safety",
        field: "generalSafetyDone",
        icon: "🦺",
        title: "Общая безопасность",
        desc: "Пройти инструктаж по общей безопасности",
        fullDesc: "Ознакомьтесь с правилами охраны труда и техники безопасности на рабочем месте. Это включает в себя правила работы за компьютером, эргономику и общие правила поведения в офисе."
    },
    {
        key: "cyber_safety",
        field: "cyberSafetyDone",
        icon: "💻",
        title: "Кибербезопасность",
        desc: "Пройти инструктаж по кибербезопасности",
        fullDesc: "Изучите политику информационной безопасности компании. Узнайте, как защитить свои учетные записи, как распознавать фишинг и какие данные запрещено передавать третьим лицам. Пройдите тест на портале ИБ."
    },
    {
        key: "pass",
        field: "passReceived",
        icon: "🪪",
        title: "Получить пропуск",
        desc: "Оформить и получить физический пропуск",
        fullDesc: "Подойдите на ресепшн или к сотрудникам службы безопасности (кабинет 102), чтобы сфотографироваться и получить физический пропуск для доступа в офис в нерабочее время (по необходимости)."
    },
    {
        key: "face_id",
        field: "faceIdDone",
        icon: "😎",
        title: "Сделать FaceId",
        desc: "Зарегистрировать лицо для входа",
        fullDesc: "Служба безопасности должна отсканировать ваше лицо для добавления в систему биометрического контроля доступа на турникетах. Подойдите на пост охраны на первом этаже."
    },
    {
        key: "workplace",
        field: "workplaceSetupRequested",
        icon: "🖥️",
        title: "Рабочее место",
        desc: "Запустить заявку на настройку рабочего места",
        fullDesc: "Оформите заявку в IT-отдел для подготовки вашего рабочего места: получение ноутбука, монитора, создание учетных записей (почта, мессенджеры, Jira, внутренние системы)."
    },
];

export const OnboardingChecklist: React.FC<OnboardingChecklistProps> = ({
    employeeId,
    employeeName,
}) => {
    const [progress, setProgress] = useState<Progress | null>(null);
    const [updating, setUpdating] = useState<string | null>(null);
    const [activeModal, setActiveModal] = useState<any>(null);

    useEffect(() => {
        api.get(`/tma/onboarding-progress/${employeeId}`)
            .then((res) => setProgress(res.data))
            .catch(console.error);
    }, [employeeId]);

    const toggleStep = async (stepKey: string, currentValue: boolean) => {
        if (updating) return;
        setUpdating(stepKey);
        try {
            const res = await api.post(
                `/tma/onboarding-progress/${employeeId}/step`,
                {
                    step: stepKey,
                    value: !currentValue,
                },
            );
            setProgress((prev) =>
                prev
                    ? {
                          ...prev,
                          [STEPS.find((s) => s.key === stepKey)!.field]:
                              !currentValue,
                          progressPercent: res.data.progressPercent,
                      }
                    : prev,
            );
        } catch (e) {
            console.error(e);
        } finally {
            setUpdating(null);
            setActiveModal(null);
        }
    };

    if (!progress)
        return (
            <div className="flex-center" style={{ height: "100px" }}>
                <div className="spinner" />
            </div>
        );

    const done = STEPS.filter(
        (s) => progress[s.field as keyof Progress] as boolean,
    ).length;

    return (
        <div className="glass-card no-hover animate-fade-in">
            <div className="section-header">
                <div>
                    <div className="section-title">🚀 Онбординг</div>
                    <div
                        className="text-subtitle"
                        style={{ fontSize: "0.8rem", marginTop: "2px" }}
                    >
                        {done} из {STEPS.length} шагов выполнено
                    </div>
                </div>
                <span
                    className={`badge ${progress.progressPercent === 100 ? "success" : progress.progressPercent > 50 ? "info" : "warning"}`}
                >
                    {progress.progressPercent}%
                </span>
            </div>

            <div className="progress-bar-bg" style={{ marginBottom: "20px" }}>
                <div
                    className="progress-bar-fill"
                    style={{ width: `${progress.progressPercent}%` }}
                />
            </div>

            {progress.progressPercent === 100 ? (
                <div className="empty-state">
                    <div className="empty-state-icon">🎉</div>
                    <div
                        className="empty-state-text"
                        style={{ color: "var(--green-700)", fontWeight: 600 }}
                    >
                        Онбординг завершён! Добро пожаловать в команду,{" "}
                        {employeeName}!
                    </div>
                </div>
            ) : (
                <div>
                    {STEPS.map((step) => {
                        const isDone = progress[
                            step.field as keyof Progress
                        ] as boolean;
                        return (
                            <div
                                key={step.key}
                                className="checklist-item"
                                onClick={() => setActiveModal({ step, isDone })}
                                style={{
                                    opacity: updating === step.key ? 0.6 : 1,
                                    cursor: "pointer",
                                }}
                            >
                                <div
                                    className={`checklist-check ${isDone ? "done" : ""}`}
                                >
                                    {isDone ? "✓" : ""}
                                </div>
                                <div className="checklist-text">
                                    <div
                                        className="checklist-title"
                                        style={{
                                            textDecoration: isDone
                                                ? "line-through"
                                                : "none",
                                            color: isDone
                                                ? "var(--text-muted)"
                                                : undefined,
                                        }}
                                    >
                                        {step.icon} {step.title}
                                    </div>
                                    <div className="checklist-desc">
                                        {step.desc}
                                    </div>
                                </div>
                            </div>
                        );
                    })}
                </div>
            )}

            {/* Modal */}
            {activeModal && (
                <div style={{
                    position: "fixed", top: 0, left: 0, right: 0, bottom: 0,
                    background: "rgba(0,0,0,0.5)", zIndex: 100,
                    display: "flex", alignItems: "center", justifyContent: "center",
                    padding: "20px"
                }}>
                    <div className="glass-card animate-fade-in" style={{ maxWidth: "400px", width: "100%", background: "#fff", color: "#111827" }}>
                        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: "16px" }}>
                            <div style={{ fontSize: "1.2rem", fontWeight: 700 }}>
                                {activeModal.step.icon} {activeModal.step.title}
                            </div>
                            <button onClick={() => setActiveModal(null)} style={{ background: "none", border: "none", fontSize: "1.5rem", cursor: "pointer", color: "#9ca3af" }}>&times;</button>
                        </div>
                        <div style={{ fontSize: "0.95rem", lineHeight: 1.5, color: "#4b5563", marginBottom: "24px" }}>
                            {activeModal.step.fullDesc}
                        </div>
                        <button
                            onClick={() => toggleStep(activeModal.step.key, activeModal.isDone)}
                            disabled={updating === activeModal.step.key}
                            style={{
                                width: "100%",
                                padding: "12px",
                                borderRadius: "8px",
                                border: "none",
                                background: activeModal.isDone ? "#ef4444" : "#2563eb",
                                color: "#fff",
                                fontWeight: 600,
                                cursor: "pointer"
                            }}
                        >
                            {updating === activeModal.step.key ? "Сохранение..." : activeModal.isDone ? "Снять отметку" : "Отметить как выполнено"}
                        </button>
                    </div>
                </div>
            )}
        </div>
    );
};
