# 🪪 Система Цифрового Пропуска (Digital Pass System)

## Обзор

Полная система для выдачи и проверки цифровых пропусков сотрудников. Охранник сканирует QR-код и видит информацию о сотруднике.

- **Сотрудник:** Видит красивый ID-карточку с QR-кодом, который автоматически обновляется каждые 4 минуты (перед истечением 5-минутного срока действия)
- **Охранник:** Открывает камеру, сканирует QR, видит результат проверки (✅ валиден или ❌ невалиден)

---

## Архитектура

### Backend (`PassController.cs`)

```
GET /api/pass/generate/{employeeId}
  └─> Возвращает: { token, expiresIn: 300, employee: { nameRu, department, position } }
      Token формат: base64url(employeeId~unixExpiry~hmac-sha256-base64url)
      TTL: 5 минут
      HMAC-SHA256 подписано с ключом из appsettings.json

GET /api/pass/verify?t={token}
  └─> Возвращает: { valid: true, employee: {...} } или { valid: false, error: "..." }
      Проверяет:
        ✓ HMAC подпись (защита от подделки)
        ✓ Срок действия (не истёк ли токен)
        ✓ Сотрудник существует и имеет department (активен)
        ✓ Сотрудник не в заявленном отпуске (опционально, сейчас отключено)
```

#### Токен Безопасность

- **Алгоритм:** HMAC-SHA256 с timing-safe comparison (`CryptographicOperations.FixedTimeEquals`)
- **Защита от подделки:** HMAC позволяет только серверу подписывать токены
- **TTL:** 5 минут, достаточно для проверки у охранника
- **Secret Key:** Конфигурируется в `appsettings.json` (`Pass:Secret`)

⚠️ **ВАЖНО для production:** Установить случайный 256-bit ключ в переменную окружения!

```json
{
  "Pass": {
    "Secret": "CHANGE-THIS-TO-A-RANDOM-256-BIT-SECRET-IN-PRODUCTION"
  }
}
```

---

### Frontend - Компоненты

#### 1. **DigitalPass.tsx** - ID-карточка сотрудника

📍 Расположение: `frontend/src/components/DigitalPass.tsx`

**Функционал:**
- Модальное окно с красивым ID-картой
- QR-код с автоматическим обновлением каждые 4 минуты
- Таймер обратного отсчёта с цветовой прогрессией:
  - 🟢 Зелёный (> 120 сек)
  - 🟡 Жёлтый (> 30 сек)
  - 🔴 Красный (< 30 сек)
- Конфетти анимация при генерации нового токена
- Кнопка "Закрыть"

**Props:**
```typescript
interface Props {
  telegramId: number;
  nameRu: string;
  department: string;
  position: string;
  onClose: () => void;
}
```

**Как использовать:**
```tsx
// В App.tsx (уже интегрировано)
const [showPass, setShowPass] = useState(false);

// Кнопка в хедере
<button onClick={() => setShowPass(true)}>🪪 Пропуск</button>

// Модаль
{showPass && (
  <DigitalPass {...employee} onClose={() => setShowPass(false)} />
)}
```

---

#### 2. **VerifyPage.tsx** - Экран проверки охранника

📍 Расположение: `frontend/src/components/VerifyPage.tsx`

**URL параметр:** `/?verify={token}`

**Функционал:**
- **Успешная проверка (✅):**
  - Зелёный градиент фон
  - Большой аватар сотрудника (120x120)
  - Информация: имя, отдел, должность, дата найма, ID
  - Конфетти анимация (60 частиц)
  - Кнопка "Сканировать снова"
  
- **Ошибка проверки (❌):**
  - Красный градиент фон
  - Пульсирующий красный круг (140x140)
  - Сообщение об ошибке
  - Кнопка "Попробовать снова"

**Animations:**
```css
@keyframes slide-up { /* подъём снизу */ }
@keyframes glow-pulse { /* пульсирование свечения */ }
@keyframes confetti-fall { /* падение конфетти */ }
@keyframes pulse-red { /* красный пульс для ошибки */ }
```

**Логика:**
```
1. Парсит URL: /?verify={token}
2. Отправляет GET /api/pass/verify?t={token}
3. Если valid=true:
   - Показывает успех с данными сотрудника
   - Запускает конфетти
4. Если valid=false:
   - Показывает ошибку
   - Отправляет ошибку на бэкенд (audit log)
```

---

#### 3. **ScanQR.tsx** - Сканер QR-кодов охранника

📍 Расположение: `frontend/src/components/ScanQR.tsx`

**URL параметр:** `/?scan=true`

**Функционал:**
- Камера включена (режим `environment` - задняя камера на мобильной)
- Кадрирование 500мс для поиска QR
- Регулятор яркости (50-150%)
- Направляющий кадр с пульсирующей границей
- Fallback ввод токена вручную через `window.prompt()`
- После обнаружения QR: перенаправляет на `/?verify={token}`

**Текущее состояние:**
```
✅ Видеопоток работает
✅ Кадрирование реализовано
✅ Fallback ввод работает
⏳ QR-детектор ждёт jsQR библиотеку
```

**Интеграция jsQR (когда npm install заработает):**

```bash
npm install jsqr
```

Затем в `ScanQR.tsx` заменить `captureFrame()`:

```typescript
import jsQR from "jsqr";

private async captureFrame(): Promise<void> {
  const canvas = this.videoRef.current;
  const context = canvas.getContext("2d");
  
  if (context && this.videoRef.current) {
    context.drawImage(this.videoRef.current, 0, 0, canvas.width, canvas.height);
    const imageData = context.getImageData(0, 0, canvas.width, canvas.height);
    
    const code = jsQR(imageData.data, imageData.width, imageData.height);
    if (code) {
      console.log("QR found:", code.data);
      this.props.onTokenFound(code.data);
    }
  }
}
```

---

#### 4. **App.tsx** - Маршрутизация

📍 Расположение: `frontend/src/App.tsx`

**Добавленный роутинг:**

```tsx
// 1. Guard verify page - показать результат проверки
const verifyToken = new URLSearchParams(window.location.search).get("verify");
if (verifyToken) return <VerifyPage token={verifyToken} />;

// 2. Guard scanner - открыть камеру
const shouldScan = new URLSearchParams(window.location.search).get("scan") === "true";
if (shouldScan) {
  return (
    <ScanQR
      onTokenFound={(token) => {
        window.location.href = `/?verify=${encodeURIComponent(token)}`;
      }}
      onClose={() => {
        window.location.href = "/";
      }}
    />
  );
}

// Обычный интерфейс сотрудника
// ...
```

**Кнопка пропуска в хедере:**
```tsx
<button
  onClick={() => setShowPass(true)}
  style={{ /* ... */ }}
>
  🪪
</button>

{showPass && (
  <DigitalPass {...employee} onClose={() => setShowPass(false)} />
)}
```

---

## Сценарии Использования

### 📱 Сотрудник Получает Пропуск

```
1. Сотрудник открывает HROps Bot
2. Нажимает кнопку "🪪 Пропуск" в хедере
3. Видит модальное окно с ID-карточкой и QR-кодом
4. QR автоматически обновляется каждые 4 минуты
5. Показывает QR охраннику
```

### 👮 Охранник Проверяет Пропуск

#### Способ 1: Сканирование камерой (когда jsQR интегрирована)
```
1. Охранник открывает приложение
2. Переходит на URL: /?scan=true
3. Включается камера
4. Охранник наводит на QR-код сотрудника
5. QR автоматически сканируется
6. Перенаправляется на /?verify={token}
7. Видит результат: ✅ валиден или ❌ невалиден с информацией сотрудника
```

#### Способ 2: Ручной ввод (fallback, работает сейчас)
```
1. Охранник открывает приложение
2. Переходит на URL: /?scan=true
3. Вводит токен вручную через диалоговое окно
4. Система проверяет токен
5. Перенаправляет на результат проверки
```

---

## API Эндпоинты

### `GET /api/pass/generate/{employeeId}`

**Параметры:**
- `employeeId` (path): ID сотрудника в системе

**Ответ 200 OK:**
```json
{
  "token": "base64url_encoded_token_with_hmac",
  "expiresIn": 300,
  "employee": {
    "nameRu": "Иван Петров",
    "department": "IT",
    "position": "Senior Developer"
  }
}
```

**Ошибки:**
- `400 Bad Request`: Сотрудник не найден или не активен
- `500 Internal Server Error`: Ошибка сервера

---

### `GET /api/pass/verify?t={token}`

**Параметры:**
- `t` (query): Токен для проверки (URL-safe base64)

**Ответ 200 OK (успешная проверка):**
```json
{
  "valid": true,
  "employee": {
    "id": 123,
    "nameRu": "Иван Петров",
    "department": "IT",
    "position": "Senior Developer",
    "hireDate": "2023-01-15T00:00:00Z"
  }
}
```

**Ответ 200 OK (невалидный токен):**
```json
{
  "valid": false,
  "error": "Token expired"
}
```

**Возможные ошибки:**
- `invalid_signature` - HMAC не совпадает (подделка)
- `token_expired` - Истёк срок действия (> 5 минут)
- `employee_not_found` - Сотрудник удалён из системы
- `employee_inactive` - У сотрудника нет отдела (не активен)

---

## Конфигурация

### Backend (`appsettings.json`)

```json
{
  "Pass": {
    "Secret": "your-random-256-bit-secret-here"
  }
}
```

**Production рекомендация:**
```bash
# Генерируем 256-bit ключ (32 байта = 256 бит)
# Linux/Mac:
openssl rand -base64 32

# PowerShell:
[Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
```

Установить в переменную окружения:
```bash
export HROPS_PASS_SECRET="your-generated-secret"
```

### Frontend

Нет специальной конфигурации. Используется `VITE_API_URL` для API базового URL.

---

## Безопасность

### Защита от Атак

| Угроза | Защита |
|--------|--------|
| **Подделка токена** | HMAC-SHA256 подпись; сервер валидирует перед принятием |
| **Replay attack** | TTL 5 минут; нельзя переиспользовать старый токен |
| **Brute-force** | HMAC требует знания серверного ключа; нет перебора |
| **Token capture** | Token в URL - нужна HTTPS в production |
| **False verification** | Timing-safe сравнение HMAC (защита от timing attack) |

### Рекомендации

1. **HTTPS Обязательна** в production (Token в URL)
2. **Secret Key** должен быть 256-bit и случайным
3. **Secret Key** должен быть в переменной окружения, не в коде
4. **Database** должна иметь индекс на Employee.Id для быстрого поиска
5. **Audit Logging** рекомендуется добавить для логирования всех проверок

---

## Тестирование

### Unit Tests

```bash
cd src/HROpsBot.API
dotnet test
```

Должно пройти 36+ тестов.

### Manual Testing

#### 1️⃣ Тест генерации пропуска
```bash
# сотрудник вызывает
GET http://localhost:5000/api/pass/generate/1
# ответ: { token: "...", expiresIn: 300, employee: {...} }
```

#### 2️⃣ Тест проверки пропуска
```bash
# охранник проверяет
GET http://localhost:5000/api/pass/verify?t=<token_from_step_1>
# ответ: { valid: true, employee: {...} }
```

#### 3️⃣ Тест истёкшего токена
```bash
# подождите > 5 минут
GET http://localhost:5000/api/pass/verify?t=<old_token>
# ответ: { valid: false, error: "Token expired" }
```

#### 4️⃣ Тест frontend UI
```bash
# Employee
http://localhost:5173/?telegramId=123&isAdmin=false
# Нажать кнопку 🪪 → Видит QR

# Guard Scanner
http://localhost:5173/?scan=true
# Камера открывается, можно вводить токен вручную

# Guard Verify
http://localhost:5173/?verify=<token_from_employee>
# Видит результат проверки
```

---

## Развёртывание

### Docker

```bash
# Build backend
docker build -f src/HROpsBot.API/Dockerfile -t hrops-api .

# Build frontend
docker build -f frontend/Dockerfile -t hrops-frontend .

# Run with docker-compose
docker-compose up
```

### Environment Variables

**Backend:**
```bash
HROPS_PASS_SECRET=your-256-bit-secret
HROPS_DB_CONNECTION=postgresql://user:pass@localhost/hrops
```

**Frontend:**
```bash
VITE_API_URL=https://api.example.com
```

---

## Следующие Шаги

### ✅ Сейчас Готово
- [x] Backend PassController с HMAC токенами
- [x] Frontend DigitalPass компонент с QR
- [x] Frontend VerifyPage с результатами
- [x] Frontend ScanQR с камерой
- [x] App.tsx маршрутизация
- [x] Backend компилируется без ошибок
- [x] Frontend компилируется без ошибок

### ⏳ Когда npm install Заработает
1. Добавить jsQR библиотеку
2. Интегрировать jsQR в ScanQR.tsx
3. Протестировать сканирование камерой

### 🔄 Опциональные Улучшения
1. Добавить PassAuditLog таблицу для логирования всех проверок
2. Добавить "rescan timeout" состояние на VerifyPage (5 сек) для предотвращения спама
3. Добавить тёмный/светлый режим
4. Добавить offline режим (кэширование последних пропусков)
5. Добавить multi-language QR (содержит lang prefix)
6. Добавить Websocket для live notification охраннику

---

## Контакты & Вопросы

- **PassController.cs:** `src/HROpsBot.API/Controllers/PassController.cs`
- **DigitalPass.tsx:** `frontend/src/components/DigitalPass.tsx`
- **VerifyPage.tsx:** `frontend/src/components/VerifyPage.tsx`
- **ScanQR.tsx:** `frontend/src/components/ScanQR.tsx`
- **App.tsx:** `frontend/src/App.tsx`

