# HROpsBot API

REST API для Telegram Mini App HROpsBot — автоматизация HR-процессов: онбординг, отпуска, справки, IT-заявки, оборудование.

> **Swagger UI** доступен по адресу `/swagger` (после запуска сервера).  
> Все ответы возвращаются в формате `application/json`.  
> Base URL: `https://<host>/api`

---

## Содержание

- [Bot](#bot)
- [TMA — Аутентификация](#tma--аутентификация)
- [TMA — Онбординг](#tma--онбординг)
- [TMA — Дашборд сотрудника](#tma--дашборд-сотрудника)
- [TMA — Справки](#tma--справки)
- [TMA — Оборудование](#tma--оборудование)
- [TMA — Отпуск](#tma--отпуск)
- [TMA — IT-заявки](#tma--it-заявки)
- [TMA — Регламенты](#tma--регламенты)
- [TMA — Записи к HR](#tma--записи-к-hr)
- [TMA — FAQ](#tma--faq)
- [TMA — Администрирование (HR Admin)](#tma--администрирование-hr-admin)
- [Справочник перечислений](#справочник-перечислений)

---

## Bot

### `GET /api/bot/health`

Проверка доступности API.

**Ответ `200 OK`**

```json
{
  "status": "ok",
  "time": "2024-06-04T10:00:00Z"
}
```

---

### `POST /api/bot/webhook`

Эндпоинт для получения Telegram Update. Вызывается Telegram-серверами автоматически — не вызывайте вручную.

**Тело запроса:** raw JSON Telegram `Update` объект.

**Ответ `200 OK`** — всегда, независимо от результата обработки.

---

## TMA — Аутентификация

### `POST /api/tma/auth`

Аутентификация / создание профиля сотрудника. Вызывается при запуске Mini App. Создаёт нового сотрудника или обновляет имя/username существующего.

**Тело запроса**

| Поле        | Тип      | Обязательное | Описание                            |
|-------------|----------|:------------:|-------------------------------------|
| `id`        | `long`   | ✓            | Telegram User ID                    |
| `firstName` | `string` | ✓            | Имя пользователя                    |
| `lastName`  | `string` |              | Фамилия пользователя                |
| `username`  | `string` |              | Telegram username (без `@`)         |

```json
{
  "id": 123456789,
  "firstName": "Айдар",
  "lastName": "Сейткали",
  "username": "aidar_s"
}
```

**Ответ `200 OK`**

```json
{
  "id": 42,
  "nameRu": "Айдар Сейткали",
  "nameKk": "Айдар Сейтқали",
  "department": "Разработка",
  "position": "Backend Developer",
  "isHrAdmin": false,
  "hiredAt": "2024-01-15T00:00:00Z"
}
```

---

## TMA — Онбординг

### `POST /api/tma/onboarding`

Заполнение анкеты при онбординге. Обновляет отдел и должность сотрудника и автоматически инициализирует чеклист онбординга.

**Тело запроса**

| Поле         | Тип      | Обязательное | Описание             |
|--------------|----------|:------------:|----------------------|
| `employeeId` | `int`    | ✓            | ID сотрудника        |
| `department` | `string` | ✓            | Название отдела      |
| `position`   | `string` | ✓            | Должность            |

**Ответ `200 OK`**

```json
{ "success": true }
```

---

### `GET /api/tma/onboarding-progress/{employeeId}`

Получить прогресс онбординга сотрудника.

**Path параметры:** `employeeId` — ID сотрудника.

**Ответ `200 OK`**

```json
{
  "id": 1,
  "employeeId": 42,
  "docsSubmitted": true,
  "accessGranted": true,
  "equipmentReceived": false,
  "materialsRead": false,
  "firstTasksDone": false,
  "buddyMet": false,
  "hr1on1Done": false,
  "progressPercent": 28,
  "startedAt": "2024-06-01T09:00:00Z"
}
```

---

### `POST /api/tma/onboarding-progress/{employeeId}/step`

Отметить шаг онбординга как выполненный или невыполненный.

**Path параметры:** `employeeId` — ID сотрудника.

**Тело запроса**

| Поле   | Тип      | Допустимые значения `step`                                                                          |
|--------|----------|------------------------------------------------------------------------------------------------------|
| `step` | `string` | `DocsSubmitted`, `AccessGranted`, `EquipmentReceived`, `MaterialsRead`, `FirstTasksDone`, `BuddyMet`, `Hr1on1Done` |
| `value` | `bool`  | `true` / `false`                                                                                    |

```json
{ "step": "EquipmentReceived", "value": true }
```

**Ответ `200 OK`**

```json
{ "progressPercent": 42 }
```

---

## TMA — Дашборд сотрудника

### `GET /api/tma/dashboard/{employeeId}`

Главный дашборд сотрудника. Возвращает баланс отпуска, активные задачи, запросы оборудования, заявки на отпуск и IT-заявки одним запросом.

**Path параметры:** `employeeId` — ID сотрудника.

**Ответ `200 OK`**

```json
{
  "vacation": {
    "total": 28,
    "used": 10,
    "remaining": 18
  },
  "tasks": [
    {
      "id": 1,
      "titleRu": "Пройти инструктаж",
      "titleKk": "Нұсқаулықтан өту",
      "status": "Active",
      "priority": 1,
      "isOverdue": false
    }
  ],
  "equipment": [
    { "id": 3, "ticketNumber": "EQ-0042", "type": "Laptop", "status": "InProgress" }
  ],
  "vacations": [
    { "id": 5, "startDate": "2024-07-01", "endDate": "2024-07-14", "status": "Pending", "daysCount": 14, "createdAt": "2024-06-01T12:00:00Z" }
  ],
  "itRequests": [
    { "id": 7, "type": "SystemAccess", "systemName": "Jira", "status": "Pending", "priority": 2, "createdAt": "2024-06-01T10:00:00Z" }
  ]
}
```

**Ответ `404 Not Found`** — сотрудник не найден.

---

## TMA — Справки

### `POST /api/tma/certificate`

Заказать справку. Срок готовности — 3 рабочих дня.

**Тело запроса**

| Поле             | Тип               | Обязательное | Описание                                        |
|------------------|-------------------|:------------:|-------------------------------------------------|
| `employeeId`     | `int`             | ✓            | ID сотрудника                                   |
| `type`           | `CertificateType` | ✓            | Тип справки (см. [перечисления](#certificatetype)) |
| `deliveryMethod` | `string`          |              | `"digital"` (по умолчанию) или `"paper"`        |

```json
{
  "employeeId": 42,
  "type": 0,
  "deliveryMethod": "digital"
}
```

**Ответ `200 OK`** — созданная заявка на справку.

---

## TMA — Оборудование

### `POST /api/tma/equipment`

Запросить оборудование.

**Тело запроса**

| Поле         | Тип             | Обязательное | Описание                                          |
|--------------|-----------------|:------------:|---------------------------------------------------|
| `employeeId` | `int`           | ✓            | ID сотрудника                                     |
| `type`       | `EquipmentType` | ✓            | Тип оборудования (см. [перечисления](#equipmenttype)) |

```json
{ "employeeId": 42, "type": 0 }
```

**Ответ `200 OK`** — созданная заявка на оборудование.

---

## TMA — Отпуск

### `POST /api/tma/vacation`

Подать заявку на отпуск.

**Тело запроса**

| Поле         | Тип        | Обязательное | Описание                              |
|--------------|------------|:------------:|---------------------------------------|
| `employeeId` | `int`      | ✓            | ID сотрудника                         |
| `startDate`  | `DateTime` | ✓            | Дата начала отпуска (ISO 8601)        |
| `endDate`    | `DateTime` | ✓            | Дата окончания отпуска (> `startDate`) |

```json
{
  "employeeId": 42,
  "startDate": "2024-07-01T00:00:00Z",
  "endDate": "2024-07-14T00:00:00Z"
}
```

**Ответ `200 OK`**

```json
{
  "id": 5,
  "startDate": "2024-07-01T00:00:00Z",
  "endDate": "2024-07-14T00:00:00Z",
  "daysCount": 14,
  "status": "Pending"
}
```

**Ответ `400 Bad Request`** — `endDate` <= `startDate`.

```json
{ "error": "Дата окончания должна быть позже даты начала" }
```

---

## TMA — IT-заявки

### `POST /api/tma/it-request`

Создать IT-заявку.

**Тело запроса**

| Поле          | Тип             | Обязательное | Описание                                             |
|---------------|-----------------|:------------:|------------------------------------------------------|
| `employeeId`  | `int`           | ✓            | ID сотрудника                                        |
| `type`        | `ItRequestType` | ✓            | Тип заявки (см. [перечисления](#itrequesttype))      |
| `systemName`  | `string`        | ✓            | Название системы (Jira, GitHub, 1C…)                 |
| `description` | `string`        | ✓            | Подробное описание запроса                           |
| `priority`    | `int`           |              | 1 — Высокий, **2 — Обычный** (по умолчанию), 3 — Низкий |

```json
{
  "employeeId": 42,
  "type": 1,
  "systemName": "Jira",
  "description": "Нужен доступ к проекту HROPS",
  "priority": 2
}
```

**Ответ `200 OK`**

```json
{
  "id": 7,
  "type": "SystemAccess",
  "systemName": "Jira",
  "status": "Pending",
  "createdAt": "2024-06-04T10:00:00Z"
}
```

---

### `GET /api/tma/it-requests/{employeeId}`

Получить все IT-заявки сотрудника.

**Path параметры:** `employeeId` — ID сотрудника.

**Ответ `200 OK`**

```json
[
  {
    "id": 7,
    "type": "SystemAccess",
    "systemName": "Jira",
    "description": "Нужен доступ к проекту HROPS",
    "status": "Pending",
    "priority": 2,
    "createdAt": "2024-06-04T10:00:00Z",
    "resolvedAt": null
  }
]
```

---

## TMA — Регламенты

### `GET /api/tma/regulations?q={query}`

Поиск по базе регламентов и корпоративных документов.

**Query параметры:** `q` — поисковый запрос (минимум 1 символ). Пустой запрос вернёт пустой массив.

**Ответ `200 OK`** — массив найденных документов.

---

## TMA — Записи к HR

### `GET /api/tma/appointments/slots`

Получить доступные временные слоты для записи к HR-менеджеру.

**Ответ `200 OK`** — список доступных временных слотов.

---

### `POST /api/tma/appointments`

Записаться на встречу с HR.

**Тело запроса**

| Поле         | Тип        | Обязательное | Описание                          |
|--------------|------------|:------------:|-----------------------------------|
| `employeeId` | `int`      | ✓            | ID сотрудника                     |
| `slot`       | `DateTime` | ✓            | Выбранный временной слот (ISO 8601) |

```json
{
  "employeeId": 42,
  "slot": "2024-06-10T14:00:00Z"
}
```

**Ответ `200 OK`**

```json
{
  "id": 3,
  "slotDateTime": "2024-06-10T14:00:00Z",
  "hrManagerNameRu": "Жанна Ахметова",
  "status": "Scheduled"
}
```

---

## TMA — FAQ

### `GET /api/tma/faq`

Список часто задаваемых вопросов.

**Ответ `200 OK`**

```json
[
  {
    "question": "Как получить справку с места работы?",
    "answer": "Зайдите в раздел 'Справка' → выберите тип → нажмите 'Заказать'. Справка будет готова в течение 1 рабочего дня."
  }
]
```

---

## TMA — Администрирование (HR Admin)

> Все эндпоинты этого раздела предназначены для пользователей с `isHrAdmin = true`.

---

### `GET /api/tma/admin/analytics`

Сводная HR-аналитика по заявкам, сотрудникам и онбордингу.

**Ответ `200 OK`** — агрегированный объект аналитики.

---

### `GET /api/tma/admin/employees`

Список всех сотрудников организации.

**Ответ `200 OK`**

```json
[
  {
    "id": 42,
    "nameRu": "Айдар Сейткали",
    "department": "Разработка",
    "position": "Backend Developer",
    "isHrAdmin": false,
    "hiredAt": "2024-01-15T00:00:00Z",
    "vacationDaysRemaining": 18
  }
]
```

---

### `GET /api/tma/admin/requests`

Все ожидающие заявки по всем категориям.

**Ответ `200 OK`**

```json
{
  "vacations": [
    {
      "id": 5, "startDate": "2024-07-01", "endDate": "2024-07-14",
      "daysCount": 14, "status": "Pending", "createdAt": "2024-06-01T12:00:00Z",
      "employee": { "id": 42, "nameRu": "Айдар Сейткали", "department": "Разработка" }
    }
  ],
  "certificates": [ { "id": 1, "type": "EmploymentConfirmation", "status": "Pending", "deliveryMethod": "digital", "createdAt": "...", "employee": { ... } } ],
  "itRequests":   [ { "id": 7, "type": "SystemAccess", "systemName": "Jira", "description": "...", "status": "Pending", "priority": 2, "createdAt": "...", "employee": { ... } } ],
  "equipment":    [ { "id": 3, "type": "Laptop", "descriptionRu": "...", "status": "Pending", "ticketNumber": "EQ-0042", "createdAt": "...", "employee": { ... } } ]
}
```

---

### `POST /api/tma/admin/vacation/{id}/approve`

Одобрить заявку на отпуск.

**Ответ `200 OK`** — `{ "success": true }` | **`404`** — заявка не найдена.

---

### `POST /api/tma/admin/vacation/{id}/reject`

Отклонить заявку на отпуск.

**Ответ `200 OK`** — `{ "success": true }` | **`404`** — заявка не найдена.

---

### `POST /api/tma/admin/certificate/{id}/approve`

Одобрить заявку на справку.

**Ответ `200 OK`** — `{ "success": true }` | **`404`** — заявка не найдена.

---

### `POST /api/tma/admin/certificate/{id}/reject`

Отклонить заявку на справку.

**Ответ `200 OK`** — `{ "success": true }` | **`404`** — заявка не найдена.

---

### `POST /api/tma/admin/equipment/{id}/approve`

Принять заявку на оборудование (перевести в статус `InProgress`).

**Ответ `200 OK`** — `{ "success": true }` | **`404`** — заявка не найдена.

---

### `POST /api/tma/admin/equipment/{id}/reject`

Отклонить заявку на оборудование.

**Ответ `200 OK`** — `{ "success": true }` | **`404`** — заявка не найдена.

---

### `POST /api/tma/admin/it-request/{id}/status`

Изменить статус IT-заявки.

**Тело запроса**

| Поле     | Тип               | Обязательное | Описание                     |
|----------|-------------------|:------------:|------------------------------|
| `status` | `ItRequestStatus` | ✓            | Новый статус заявки          |
| `note`   | `string`          |              | Опциональная заметка исполнителя |

```json
{ "status": 3, "note": "Доступ к Jira выдан, проверьте почту." }
```

**Ответ `200 OK`**

```json
{ "id": 7, "status": "Done" }
```

**Ответ `404 Not Found`** — заявка не найдена.

---

## Справочник перечислений

### `CertificateType`

| Значение | Название                  | Описание                   |
|:--------:|---------------------------|----------------------------|
| `0`      | `EmploymentConfirmation`  | Справка с места работы     |
| `1`      | `SalaryStatement`         | Справка о зарплате (2-НДФЛ) |
| `2`      | `IncomeTax`               | КПН / ИПН                  |
| `3`      | `WorkExperience`          | Стаж работы                |

### `RequestStatus`

| Значение      | Описание           |
|---------------|--------------------|
| `Pending`     | Ожидает обработки  |
| `InProgress`  | В обработке        |
| `Approved`    | Одобрена           |
| `Ready`       | Готова             |
| `Delivered`   | Выдана             |
| `Rejected`    | Отклонена          |
| `Cancelled`   | Отменена           |

### `EquipmentType`

| Значение | Название   |
|:--------:|------------|
| `0`      | `Laptop`   |
| `1`      | `Monitor`  |
| `2`      | `Keyboard` |
| `3`      | `Mouse`    |
| `4`      | `Headset`  |
| `5`      | `Phone`    |
| `6`      | `Chair`    |
| `7`      | `Desk`     |
| `8`      | `Other`    |

### `ItRequestType`

| Значение | Название       | Описание                           |
|:--------:|----------------|------------------------------------|
| `1`      | `SystemAccess` | Доступ к системе (Jira, GitHub, 1C) |
| `2`      | `FolderAccess` | Доступ к папке / диску             |
| `3`      | `GroupAccess`  | Добавление в группу / команду      |
| `4`      | `EmailSetup`   | Настройка корпоративной почты      |
| `5`      | `VpnAccess`    | VPN доступ                         |
| `6`      | `Other`        | Прочее                             |

### `ItRequestStatus`

| Значение | Название     | Описание          |
|:--------:|--------------|-------------------|
| `1`      | `Pending`    | Ожидает обработки |
| `2`      | `InProgress` | Выполняется       |
| `3`      | `Done`       | Выполнена         |
| `4`      | `Rejected`   | Отклонена         |
