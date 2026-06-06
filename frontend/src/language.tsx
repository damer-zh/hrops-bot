import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from "react";

export type Language = "kk" | "ru";

type LanguageContextValue = {
    language: Language;
    setLanguage: (language: Language) => void;
    t: (key: keyof typeof translations.ru) => string;
};

const STORAGE_KEY = "hrops-language";

const translations = {
    ru: {
        back: "Назад",
        viewAvatar: "Посмотреть аватарку",
        setAvatar: "Поставить аватарку",
        positionMissing: "Должность не указана",
        departmentMissing: "Отдел не указан",
        set: "Поставить",
        view: "Посмотреть",
        remove: "Убрать",
        employeeId: "ID сотрудника",
        hiredDate: "Дата найма",
        role: "Роль",
        hrAdmin: "HR-админ",
        employee: "Сотрудник",
        openPass: "Открыть пропуск",
        notSpecified: "Не указано",
        loadingError: "Ошибка загрузки",
        loadingProfile: "Загружаем профиль...",
        openInTelegram: "Пожалуйста, откройте приложение внутри Telegram.",
        avatarSetError: "Не удалось поставить аватарку",
        hrPanel: "HR-Панель",
        switchRoleTitle: "Сменить роль",
        switchRole: "Роль",
        pass: "Пропуск",
        openProfile: "Открыть профиль",
        reason: "Причина",
        closeAvatarPreview: "Закрыть просмотр аватарки",
        employeeAvatar: "Аватарка сотрудника",
        welcome: "Добро пожаловать",
        chooseRole: "Выберите, как вы входите в систему",
        employeeRole: "Сотрудник",
        employeeRoleDesc: "Мои заявки, отпуска, документы и онбординг",
        guardRole: "Охранник",
        guardRoleDesc: "Сканирование QR-пропусков сотрудников",
        hrPanelRole: "HR-панель",
        hrOnly: "Только HR",
        hrPanelDesc: "Управление сотрудниками, заявки, аналитика",
        hrRestricted: "Доступно только HR-администраторам",
    },
    kk: {
        back: "Артқа",
        viewAvatar: "Аватарды көру",
        setAvatar: "Аватар қою",
        positionMissing: "Лауазым көрсетілмеген",
        departmentMissing: "Бөлім көрсетілмеген",
        set: "Қою",
        view: "Көру",
        remove: "Өшіру",
        employeeId: "Қызметкер ID",
        hiredDate: "Жұмысқа қабылданған күн",
        role: "Рөл",
        hrAdmin: "HR-әкімші",
        employee: "Қызметкер",
        openPass: "Рұқсатнаманы ашу",
        notSpecified: "Көрсетілмеген",
        loadingError: "Жүктеу қатесі",
        loadingProfile: "Профиль жүктелуде...",
        openInTelegram: "Қолданбаны Telegram ішінде ашыңыз.",
        avatarSetError: "Аватарды қою мүмкін болмады",
        hrPanel: "HR-панель",
        switchRoleTitle: "Рөлді ауыстыру",
        switchRole: "Рөл",
        pass: "Рұқсатнама",
        openProfile: "Профильді ашу",
        reason: "Себебі",
        closeAvatarPreview: "Аватар көрінісін жабу",
        employeeAvatar: "Қызметкер аватары",
        welcome: "Қош келдіңіз",
        chooseRole: "Жүйеге қалай кіретініңізді таңдаңыз",
        employeeRole: "Қызметкер",
        employeeRoleDesc: "Менің өтінімдерім, демалыстар, құжаттар және онбординг",
        guardRole: "Күзетші",
        guardRoleDesc: "Қызметкерлердің QR-рұқсатнамаларын сканерлеу",
        hrPanelRole: "HR-панель",
        hrOnly: "Тек HR",
        hrPanelDesc: "Қызметкерлерді, өтінімдерді және аналитиканы басқару",
        hrRestricted: "Тек HR-әкімшілерге қолжетімді",
    },
} as const;

const LanguageContext = createContext<LanguageContextValue | null>(null);

const getInitialLanguage = (): Language => {
    const stored = localStorage.getItem(STORAGE_KEY);
    return stored === "kk" || stored === "ru" ? stored : "ru";
};

export const LanguageProvider = ({ children }: { children: ReactNode }) => {
    const [language, setLanguageState] = useState<Language>(getInitialLanguage);

    const setLanguage = (nextLanguage: Language) => {
        localStorage.setItem(STORAGE_KEY, nextLanguage);
        setLanguageState(nextLanguage);
    };

    useEffect(() => {
        document.documentElement.lang = language === "kk" ? "kk-KZ" : "ru-RU";
    }, [language]);

    const value = useMemo(
        () => ({
            language,
            setLanguage,
            t: (key: keyof typeof translations.ru) => translations[language][key],
        }),
        [language],
    );

    return <LanguageContext.Provider value={value}>{children}</LanguageContext.Provider>;
};

export const useLanguage = () => {
    const context = useContext(LanguageContext);
    if (!context) {
        throw new Error("useLanguage must be used within LanguageProvider");
    }
    return context;
};

export const LanguageToggle = ({ variant = "light" }: { variant?: "light" | "dark" }) => {
    const { language, setLanguage } = useLanguage();

    return (
        <div className={`language-toggle language-toggle-${variant}`} aria-label="Language">
            <button
                type="button"
                className={language === "kk" ? "active" : ""}
                onClick={() => setLanguage("kk")}
                aria-pressed={language === "kk"}
            >
                KZ
            </button>
            <button
                type="button"
                className={language === "ru" ? "active" : ""}
                onClick={() => setLanguage("ru")}
                aria-pressed={language === "ru"}
            >
                RUS
            </button>
        </div>
    );
};
