# HROps Bot 🤖

Корпоративный Telegram-бот для автоматизации внутренних HR-запросов сотрудников.

## Возможности

| Сценарий | Команда/Интент |
|----------|---------------|
| 📅 Статус отпуска | «Сколько дней отпуска осталось?» / «Демалысым қалды ма?» |
| 📄 Запрос справки | «Нужна справка с места работы» |
| 🔍 Поиск регламентов | «Политика командировок» / «Іссапар ережесі» |
| 💻 Запрос оборудования | «Нужен ноутбук» |
| ✅ Мои задачи | «Какие у меня задачи?» / «Менің тапсырмаларым» |
| 🗓️ Запись к HR | «Хочу записаться к HR» |
| ❓ FAQ | «Как оформить больничный?» |

## Стек технологий

- **Backend**: ASP.NET Core 8 + EF Core + PostgreSQL + Redis
- **NLU**: Google Gemini 1.5 Flash (бесплатно, 1500 req/day)
- **Bot**: Telegram Bot API (webhook)
- **Frontend (TMA)**: Vite + React 18 + TypeScript + Recharts
- **Real-time**: SignalR
- **Deploy**: Docker Compose + GitHub Actions

## Быстрый старт

### 1. Настройка окружения

```bash
cp .env.example .env
# Заполни .env: TELEGRAM_BOT_TOKEN и GEMINI_API_KEY
```

### 2. Получи ключи

- **Telegram Bot Token**: [@BotFather](https://t.me/BotFather) → `/newbot`
- **Gemini API Key**: [aistudio.google.com](https://aistudio.google.com/app/apikey) (бесплатно)

### 3. Запуск

```bash
docker-compose up -d
```

### 4. Регистрация webhook

```bash
curl -X POST "https://api.telegram.org/bot<TOKEN>/setWebhook" \
  -d "url=https://your-domain.com/api/bot/webhook"
```

## Разработка

```bash
# Backend
dotnet run --project src/HROpsBot.API

# Frontend (TMA)
cd frontend && npm run dev
```

## Структура проекта

```
hrops-bot/
├── src/
│   ├── HROpsBot.API/           # ASP.NET Core WebAPI + Telegram webhook
│   ├── HROpsBot.Core/          # NLU (Gemini), Dialog Manager, Handlers
│   ├── HROpsBot.Domain/        # Доменные модели
│   ├── HROpsBot.Infrastructure/ # EF Core, Redis, Telegram adapter
│   └── HROpsBot.MockAPI/       # Mock внутренних систем (HR, ITSM, Tasks)
├── frontend/                   # Telegram Mini App (React TS)
├── tests/                      # Unit + Integration тесты
├── docs/                       # Документация сценариев
└── docker-compose.yml
```

## Языки

Бот отвечает одновременно на **русском** и **казахском (кириллица)** языках.

## Лицензия

MIT